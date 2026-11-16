using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GerenciadorAulas.Core;
public interface ITokenVault
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}
public sealed record OAuthTokens(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresUtc);
public sealed record OAuthChallenge(string Verifier, string State, string Challenge)
{
    public static OAuthChallenge Create()
    {
        var verifier = Base64(RandomNumberGenerator.GetBytes(32));
        return new(verifier, Base64(RandomNumberGenerator.GetBytes(32)), Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }
    private static string Base64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
public interface INativeGoogleAuthorization
{
    Task<OAuthTokens> AuthorizeAsync(bool interactive, string? accountName);
    Task RevokeAsync(string? accountName);
}
public sealed record GoogleAccount(string DisplayName, string Email, string? PermissionId);
public sealed class GoogleOnboarding(ITokenVault vault)
{
    public async Task<bool> ShouldOfferAsync() => await vault.GetAsync("google-welcome-complete") != "true" && await vault.GetAsync("google-tokens") is null;
    public Task CompleteAsync() => vault.SetAsync("google-welcome-complete", "true");
}
public sealed class GoogleAuthorization(HttpClient http, ITokenVault vault, INativeGoogleAuthorization? native = null)
{
    public const string Scope = "https://www.googleapis.com/auth/drive.file";
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private static readonly string[] DriveStateKeys = ["drive-folder", "drive-last-revision", "drive-last-success", "drive-last-backup", "drive-auto-enabled", "google-account"];
    public static Uri AuthorizationUri(string clientId, string redirect, OAuthChallenge challenge) => new("https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", new Dictionary<string,string>
    {
        ["client_id"] = clientId, ["redirect_uri"] = redirect, ["response_type"] = "code", ["scope"] = Scope,
        ["code_challenge"] = challenge.Challenge, ["code_challenge_method"] = "S256", ["state"] = challenge.State, ["access_type"] = "offline", ["prompt"] = "consent"
    }.Select(k => Uri.EscapeDataString(k.Key) + "=" + Uri.EscapeDataString(k.Value))));
    public async Task<bool> HasSessionAsync() => await vault.GetAsync("google-tokens") is not null;
    public async Task<GoogleAccount?> AccountAsync() => await vault.GetAsync("google-account") is string json ? JsonSerializer.Deserialize<GoogleAccount>(json) : null;
    public Task RememberAccountAsync(GoogleAccount account) => vault.SetAsync("google-account", JsonSerializer.Serialize(account));
    private async Task ClearDriveStateAsync() { foreach (var key in DriveStateKeys) await vault.RemoveAsync(key); }
    public async Task ConnectNativeAsync()
    {
        if (native is null) throw new NotSupportedException("Autorização nativa indisponível nesta plataforma.");
        var tokens = await native.AuthorizeAsync(true, null);
        if (string.IsNullOrWhiteSpace(tokens.AccessToken)) throw new InvalidOperationException("Google não retornou autorização.");
        await ClearDriveStateAsync();
        await vault.SetAsync("google-tokens", JsonSerializer.Serialize(tokens)); await vault.SetAsync("google-auth-mode", "android");
        await vault.RemoveAsync("google-client-id"); await vault.RemoveAsync("google-desktop-secret");
    }
    public async Task ExchangeAsync(string code, string returnedState, OAuthChallenge challenge, string clientId, string redirect, string? desktopSecret = null)
    {
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(returnedState), Encoding.UTF8.GetBytes(challenge.State))) throw new InvalidOperationException("Resposta OAuth inválida: state divergente.");
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Código ou cliente OAuth ausente.");
        var fields = new Dictionary<string,string> { ["client_id"] = clientId, ["code"] = code, ["redirect_uri"] = redirect, ["code_verifier"] = challenge.Verifier, ["grant_type"] = "authorization_code" };
        if (!string.IsNullOrEmpty(desktopSecret)) fields["client_secret"] = desktopSecret;
        await TokenAsync(fields, false); await ClearDriveStateAsync();
        await vault.SetAsync("google-client-id", clientId); await vault.RemoveAsync("google-auth-mode"); await vault.RemoveAsync("google-desktop-secret");
        if (!string.IsNullOrEmpty(desktopSecret)) await vault.SetAsync("google-desktop-secret", desktopSecret);
    }
    private async Task<OAuthTokens> TokenAsync(Dictionary<string,string> fields, bool preserveRefresh = true)
    {
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(fields));
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Não foi possível autorizar a conta Google ({(int)response.StatusCode}). Verifique a conexão ou reconecte a conta.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var root = json.RootElement;
        if (root.TryGetProperty("scope", out var scope) && !(scope.GetString() ?? "").Split(' ').Contains(Scope)) throw new InvalidOperationException("Permissão drive.file não concedida.");
        var access = root.GetProperty("access_token").GetString(); var seconds = root.GetProperty("expires_in").GetInt32();
        if (string.IsNullOrWhiteSpace(access) || seconds <= 0) throw new InvalidDataException("Resposta OAuth incompleta.");
        var previous = preserveRefresh ? await vault.GetAsync("google-tokens") : null; var old = previous is null ? null : JsonSerializer.Deserialize<OAuthTokens>(previous);
        var tokens = new OAuthTokens(access, root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : old?.RefreshToken, DateTimeOffset.UtcNow.AddSeconds(seconds));
        await vault.SetAsync("google-tokens", JsonSerializer.Serialize(tokens)); return tokens;
    }
    public async Task InvalidateAsync()
    {
        if (await vault.GetAsync("google-tokens") is string json)
            await vault.SetAsync("google-tokens", JsonSerializer.Serialize(JsonSerializer.Deserialize<OAuthTokens>(json)! with { ExpiresUtc = DateTimeOffset.MinValue }));
    }
    public async Task<string> AccessTokenAsync()
    {
        await refreshGate.WaitAsync();
        try
        {
            var value = await vault.GetAsync("google-tokens") ?? throw new InvalidOperationException("Conecte sua conta Google primeiro.");
            var tokens = JsonSerializer.Deserialize<OAuthTokens>(value)!;
            if (tokens.ExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(1)) return tokens.AccessToken;
            if (await vault.GetAsync("google-auth-mode") == "android")
            {
                if (native is null) throw new InvalidOperationException("Reconecte a conta neste dispositivo.");
                var account = await AccountAsync();
                if (account is null) throw new InvalidOperationException("Reconecte a conta para confirmar sua identidade.");
                var renewed = await native.AuthorizeAsync(false, account.Email);
                await vault.SetAsync("google-tokens", JsonSerializer.Serialize(renewed)); return renewed.AccessToken;
            }
            if (tokens.RefreshToken is null) throw new InvalidOperationException("Conecte novamente a conta Google.");
            var fields = new Dictionary<string,string> { ["client_id"] = await vault.GetAsync("google-client-id") ?? throw new InvalidOperationException("Cliente OAuth não configurado."), ["refresh_token"] = tokens.RefreshToken, ["grant_type"] = "refresh_token" };
            var secret = await vault.GetAsync("google-desktop-secret"); if (secret is not null) fields["client_secret"] = secret;
            return (await TokenAsync(fields)).AccessToken;
        }
        finally { refreshGate.Release(); }
    }
    public async Task<bool> DisconnectAsync()
    {
        var revoked = true;
        try
        {
            if (await vault.GetAsync("google-auth-mode") == "android" && native is not null) await native.RevokeAsync((await AccountAsync())?.Email);
            else if (await vault.GetAsync("google-tokens") is string value)
            {
                var token = JsonSerializer.Deserialize<OAuthTokens>(value)!;
                using var response = await http.PostAsync("https://oauth2.googleapis.com/revoke", new FormUrlEncodedContent(new Dictionary<string,string> { ["token"] = token.RefreshToken ?? token.AccessToken }));
                revoked = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidOperationException) { revoked = false; }
        finally
        {
            await ClearDriveStateAsync();
            foreach (var key in new[] { "google-tokens", "google-client-id", "google-desktop-secret", "google-auth-mode" }) await vault.RemoveAsync(key);
        }
        return revoked;
    }
}

public sealed record RemoteBackup(string Id, string Name, long Size, DateTimeOffset CreatedUtc, string Device, int Schema, string? Sha256 = null, string? Md5 = null);
public interface IBackupDrive
{
    Task<RemoteBackup> UploadAsync(BackupPackage package, CancellationToken cancellationToken = default);
    Task<List<RemoteBackup>> ListAsync(CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken cancellationToken = default);
}
public sealed class GoogleDriveClient(HttpClient http, GoogleAuthorization authorization, ITokenVault vault) : IBackupDrive
{
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken ct, bool allowResume = false, Dictionary<string,string>? headers = null)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        if (headers is not null) foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await authorization.AccessTokenAsync());
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode && !(allowResume && (int)response.StatusCode == 308)) { var status = response.StatusCode; response.Dispose(); if (status == HttpStatusCode.Unauthorized) await authorization.InvalidateAsync(); if (status == HttpStatusCode.NotFound) await vault.RemoveAsync("drive-folder"); throw new HttpRequestException($"Google Drive indisponível ({(int)status}). O backup continua pendente.", null, status); }
        return response;
    }
    public async Task<GoogleAccount> GetAccountAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/drive/v3/about?fields=user(displayName,emailAddress,permissionId)", null, ct);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); var user = json.RootElement.GetProperty("user");
        var email = user.GetProperty("emailAddress").GetString();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidDataException("O Google não identificou a conta autorizada.");
        var account = new GoogleAccount(user.TryGetProperty("displayName", out var name) ? name.GetString() ?? email : email, email, user.TryGetProperty("permissionId", out var id) ? id.GetString() : null);
        await authorization.RememberAccountAsync(account); return account;
    }
    public async Task PrepareFoldersAsync(CancellationToken ct = default) => _ = await BackupFolderAsync(ct);
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
    private async Task<string> FolderAsync(string name, string parent, CancellationToken ct)
    {
        var query = $"trashed=false and mimeType='application/vnd.google-apps.folder' and name='{Escape(name)}' and '{Escape(parent)}' in parents";
        using var response = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(query) + "&fields=files(id)&pageSize=100", null, ct);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); var items = json.RootElement.GetProperty("files");
        if (items.GetArrayLength() > 0) return items[0].GetProperty("id").GetString()!;
        using var create = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/drive/v3/files?fields=id", JsonContent.Create(new { name, mimeType = "application/vnd.google-apps.folder", parents = new[] { parent } }), ct);
        using var result = JsonDocument.Parse(await create.Content.ReadAsStringAsync(ct)); return result.RootElement.GetProperty("id").GetString()!;
    }
    private readonly SemaphoreSlim folderGate = new(1, 1);
    private async Task<string> BackupFolderAsync(CancellationToken ct)
    {
        await folderGate.WaitAsync(ct);
        try
        {
            var saved = await vault.GetAsync("drive-folder"); if (saved is not null) return saved;
            var app = await FolderAsync("GerenciadorAulas", "root", ct); var backups = await FolderAsync("Backups", app, ct);
            await vault.SetAsync("drive-folder", backups); return backups;
        }
        finally { folderGate.Release(); }
    }
    public async Task<RemoteBackup> UploadAsync(BackupPackage package, CancellationToken cancellationToken = default)
    {
        var folder = await BackupFolderAsync(cancellationToken);
        var name = $"GerenciadorAulas-{package.Manifest.CreatedUtc:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.gabackup";
        if (package.Bytes.Length > 5 * 1024 * 1024) return await UploadResumableAsync(package, folder, name, cancellationToken);
        using var multipart = new MultipartContent("related");
        multipart.Add(JsonContent.Create(new { name, parents = new[] { folder }, appProperties = new { device = package.Manifest.Device, schema = package.Manifest.Schema.ToString(), revision = package.Manifest.Revision.ToString(), sha256 = Convert.ToHexString(SHA256.HashData(package.Bytes)) } }));
        var binary = new ByteArrayContent(package.Bytes); binary.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); multipart.Add(binary);
        using var response = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name,size,createdTime,md5Checksum,appProperties", multipart, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); var root = json.RootElement;
        if (root.GetProperty("size").GetString() != package.Bytes.LongLength.ToString() || !root.GetProperty("md5Checksum").GetString()!.Equals(Convert.ToHexString(MD5.HashData(package.Bytes)), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("O Drive não confirmou a integridade do upload.");
        return Parse(root);
    }
    private static RemoteBackup VerifyUpload(JsonElement root, BackupPackage package)
    {
        if (root.GetProperty("size").GetString() != package.Bytes.LongLength.ToString() || !root.GetProperty("md5Checksum").GetString()!.Equals(Convert.ToHexString(MD5.HashData(package.Bytes)), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("O Drive não confirmou a integridade do upload.");
        return Parse(root);
    }
    private async Task<RemoteBackup> UploadResumableAsync(BackupPackage package, string folder, string name, CancellationToken ct)
    {
        var metadata = JsonContent.Create(new { name, parents = new[] { folder }, appProperties = new { device = package.Manifest.Device, schema = package.Manifest.Schema.ToString(), revision = package.Manifest.Revision.ToString(), sha256 = Convert.ToHexString(SHA256.HashData(package.Bytes)) } });
        using var begin = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable&fields=id,name,size,createdTime,md5Checksum,appProperties", metadata, ct, headers: new() { ["X-Upload-Content-Type"] = "application/octet-stream", ["X-Upload-Content-Length"] = package.Bytes.Length.ToString() });
        var session = begin.Headers.Location;
        if (session is null || !session.IsAbsoluteUri || session.Scheme != "https" || session.Host != "www.googleapis.com" || !session.IsDefaultPort || !string.IsNullOrEmpty(session.UserInfo) || !session.AbsolutePath.StartsWith("/upload/drive/v3/files", StringComparison.Ordinal)) throw new InvalidDataException("Sessão de upload Google inválida.");
        var offset = 0; var probes = 0;
        while (offset < package.Bytes.Length)
        {
            var count = Math.Min(1024 * 1024, package.Bytes.Length - offset);
            using var chunk = new ByteArrayContent(package.Bytes, offset, count); chunk.Headers.ContentType = new("application/octet-stream"); chunk.Headers.ContentRange = new(offset, offset + count - 1, package.Bytes.Length);
            HttpResponseMessage response;
            try { response = await SendAsync(HttpMethod.Put, session.AbsoluteUri, chunk, ct, allowResume: true); }
            catch (HttpRequestException ex) when (!ct.IsCancellationRequested && probes++ == 0 && (ex.StatusCode is null || (int)ex.StatusCode >= 500))
            {
                using var query = new ByteArrayContent([]); query.Headers.ContentRange = new ContentRangeHeaderValue(package.Bytes.Length);
                response = await SendAsync(HttpMethod.Put, session.AbsoluteUri, query, ct, allowResume: true);
            }
            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); return VerifyUpload(json.RootElement, package);
                }
                var range = response.Headers.TryGetValues("Range", out var values) ? values.SingleOrDefault() : null;
                if (range is null || !range.StartsWith("bytes=0-", StringComparison.Ordinal) || !int.TryParse(range[8..], out var last) || last < offset - 1 || last >= offset + count || last >= package.Bytes.Length - 1)
                    throw new InvalidDataException("O Drive não confirmou o progresso do upload.");
                var next = last + 1; if (next == offset && probes > 1) throw new IOException("Upload sem progresso. Tente novamente.");
                if (next == offset) probes++; offset = next;
            }
        }
        throw new InvalidDataException("Upload incompleto.");
    }

    public async Task<List<RemoteBackup>> ListAsync(CancellationToken cancellationToken = default)
    {
        var folder = await BackupFolderAsync(cancellationToken); string? next = null; var list = new List<RemoteBackup>();
        do
        {
            var query = Uri.EscapeDataString($"'{Escape(folder)}' in parents and trashed=false and mimeType!='application/vnd.google-apps.folder'");
            var uri = "https://www.googleapis.com/drive/v3/files?q=" + query + "&fields=nextPageToken,files(id,name,size,createdTime,md5Checksum,appProperties)&orderBy=createdTime%20desc&pageSize=100" + (next is null ? "" : "&pageToken=" + Uri.EscapeDataString(next));
            using var response = await SendAsync(HttpMethod.Get, uri, null, cancellationToken); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            list.AddRange(json.RootElement.GetProperty("files").EnumerateArray().Where(f => f.GetProperty("name").GetString()!.EndsWith(".gabackup", StringComparison.Ordinal)).Select(Parse));
            next = json.RootElement.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null;
        } while (next is not null);
        return list;
    }
    private static RemoteBackup Parse(JsonElement root)
    {
        var has = root.TryGetProperty("appProperties", out var props);
        return new(root.GetProperty("id").GetString()!, root.GetProperty("name").GetString()!, long.Parse(root.GetProperty("size").GetString()!), root.GetProperty("createdTime").GetDateTimeOffset(), has && props.TryGetProperty("device", out var d) ? d.GetString()! : "Desconhecido", has && props.TryGetProperty("schema", out var s) && int.TryParse(s.GetString(), out var schema) ? schema : 0, has && props.TryGetProperty("sha256", out var hash) ? hash.GetString() : null, root.TryGetProperty("md5Checksum", out var md5) ? md5.GetString() : null);
    }
    public async Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken cancellationToken = default)
    {
        if (backup.Size <= 0 || backup.Size > 260L * 1024 * 1024) throw new InvalidDataException("Backup excede o limite.");
        using var response = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files/" + Uri.EscapeDataString(backup.Id) + "?alt=media", null, cancellationToken);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken); using var output = new MemoryStream(); var buffer = new byte[81920]; int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0) { if (output.Length + read > 260L * 1024 * 1024) throw new InvalidDataException("Backup excessivo."); await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken); }
        if (output.Length != backup.Size) throw new InvalidDataException("Download incompleto."); var bytes = output.ToArray(); if (backup.Sha256 is string sha && !sha.Equals(Convert.ToHexString(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase) || backup.Md5 is string md5 && !md5.Equals(Convert.ToHexString(MD5.HashData(bytes)), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Integridade do download inválida."); return bytes;
    }
}
public sealed class BackupCoordinator(JournalStore store, BackupService backups, IBackupDrive drive, ITokenVault vault, TimeProvider? clock = null)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset Now => (clock ?? TimeProvider.System).GetUtcNow();
    public string Status { get; private set; } = "Backup automático desativado.";
    private DateTimeOffset due;
    private DateTimeOffset firstPending;
    private DateTimeOffset lastAttempt;
    public int MinimumIntervalSeconds { get; private set; } = 120;
    public bool Enabled { get; private set; }
    public async Task InitializeAsync()
    {
        if (int.TryParse(await vault.GetAsync("drive-auto-interval"), out var interval) && interval is >= 30 and <= 3600) MinimumIntervalSeconds = interval;
        if (await vault.GetAsync("drive-auto-enabled") == "true") Enable();
    }
    public async Task ConfigureAsync(bool enabled, int intervalSeconds = 120)
    {
        if (intervalSeconds is < 30 or > 3600) throw new ArgumentException("Intervalo automático deve estar entre 30 segundos e 60 minutos.");
        MinimumIntervalSeconds = intervalSeconds;
        await vault.SetAsync("drive-auto-interval", intervalSeconds.ToString()); await vault.SetAsync("drive-auto-enabled", enabled ? "true" : "false");
        if (enabled) Enable(); else Disable();
    }
    public void Enable() { Enabled = true; firstPending = Now; due = Now.AddSeconds(30); Status = "Backup pendente."; }
    public void Disable() { Enabled = false; Status = "Backup automático desativado."; }
    public void NotifyChange()
    {
        if (!Enabled) return;
        if (firstPending == default) firstPending = Now;
        var quiet = Now.AddSeconds(30); var maximum = firstPending.AddMinutes(5);
        due = quiet < maximum ? quiet : maximum; Status = "Alterações pendentes; aguardando intervalo de envio.";
    }
    public async Task TickAsync(string? password, string device, CancellationToken ct = default)
    {
        if (!Enabled || Now < due || Now < lastAttempt.AddSeconds(MinimumIntervalSeconds)) return;
        var revision = await store.RevisionAsync();
        if (await vault.GetAsync("drive-last-revision") == revision.ToString()) return;
        if (string.IsNullOrEmpty(password)) { Status = "Backup pendente: informe a senha de recuperação nesta sessão."; return; }
        lastAttempt = Now;
        try { await RunAsync(password, device, ct); }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException) { Status = "Backup pendente; falha de conexão ou configuração. Tente novamente."; due = Now.AddMinutes(2); }
    }
    public async Task<RemoteBackup> RunAsync(string password, string device, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            Status = "Criando e enviando backup…";
            var package = await backups.CreateAsync(password, device); var result = await drive.UploadAsync(package, ct);
            await vault.SetAsync("drive-last-revision", package.Manifest.Revision.ToString()); await vault.SetAsync("drive-last-success", Now.ToString("O"));
            await vault.SetAsync("drive-last-backup", JsonSerializer.Serialize(result)); firstPending = default; lastAttempt = Now;
            Status = "Último backup verificado: " + Now.ToLocalTime().ToString("g"); return result;
        }
        catch { Status = "Backup pendente; envio não concluído."; throw; }
        finally { gate.Release(); }
    }
}
