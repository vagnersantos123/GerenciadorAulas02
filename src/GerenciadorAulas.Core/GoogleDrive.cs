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
public sealed class GoogleAuthorization(HttpClient http, ITokenVault vault)
{
    public const string Scope = "https://www.googleapis.com/auth/drive.file";
    public static Uri AuthorizationUri(string clientId, string redirect, OAuthChallenge challenge) => new("https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", new Dictionary<string,string>
    {
        ["client_id"] = clientId, ["redirect_uri"] = redirect, ["response_type"] = "code", ["scope"] = Scope,
        ["code_challenge"] = challenge.Challenge, ["code_challenge_method"] = "S256", ["state"] = challenge.State, ["access_type"] = "offline", ["prompt"] = "consent"
    }.Select(k => Uri.EscapeDataString(k.Key) + "=" + Uri.EscapeDataString(k.Value))));
    public async Task ExchangeAsync(string code, string returnedState, OAuthChallenge challenge, string clientId, string redirect, string? desktopSecret = null)
    {
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(returnedState), Encoding.UTF8.GetBytes(challenge.State))) throw new InvalidOperationException("Resposta OAuth inválida: state divergente.");
        var fields = new Dictionary<string,string> { ["client_id"] = clientId, ["code"] = code, ["redirect_uri"] = redirect, ["code_verifier"] = challenge.Verifier, ["grant_type"] = "authorization_code" };
        if (!string.IsNullOrEmpty(desktopSecret)) fields["client_secret"] = desktopSecret;
        await TokenAsync(fields,false);
        await vault.SetAsync("google-client-id", clientId);
        if (!string.IsNullOrEmpty(desktopSecret)) await vault.SetAsync("google-desktop-secret", desktopSecret);
    }
    private async Task<OAuthTokens> TokenAsync(Dictionary<string,string> fields,bool preserveRefresh=true)
    {
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(fields));
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Não foi possível autorizar a conta Google ({(int)response.StatusCode}).");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var root = json.RootElement;
        var previous = preserveRefresh ? await vault.GetAsync("google-tokens") : null; var old = previous is null ? null : JsonSerializer.Deserialize<OAuthTokens>(previous);
        var tokens = new OAuthTokens(root.GetProperty("access_token").GetString()!, root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : old?.RefreshToken, DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()));
        await vault.SetAsync("google-tokens", JsonSerializer.Serialize(tokens)); return tokens;
    }
    public async Task<string> AccessTokenAsync()
    {
        var value = await vault.GetAsync("google-tokens") ?? throw new InvalidOperationException("Conecte sua conta Google primeiro.");
        var tokens = JsonSerializer.Deserialize<OAuthTokens>(value)!;
        if (tokens.ExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(1)) return tokens.AccessToken;
        if (tokens.RefreshToken is null) throw new InvalidOperationException("Conecte novamente a conta Google.");
        var fields = new Dictionary<string,string> { ["client_id"] = await vault.GetAsync("google-client-id") ?? throw new InvalidOperationException("Cliente OAuth não configurado."), ["refresh_token"] = tokens.RefreshToken, ["grant_type"] = "refresh_token" };
        var secret = await vault.GetAsync("google-desktop-secret"); if (secret is not null) fields["client_secret"] = secret;
        return (await TokenAsync(fields)).AccessToken;
    }
    public async Task DisconnectAsync()
    {
        var value = await vault.GetAsync("google-tokens");
        if (value is not null)
        {
            var t = JsonSerializer.Deserialize<OAuthTokens>(value)!;
            using var response = await http.PostAsync("https://oauth2.googleapis.com/revoke", new FormUrlEncodedContent(new Dictionary<string,string> { ["token"] = t.RefreshToken ?? t.AccessToken }));
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest) throw new HttpRequestException("Não foi possível revogar a autorização. Tente novamente com internet.");
        }
        foreach (var key in new[] { "google-tokens", "google-client-id", "google-desktop-secret", "drive-folder", "drive-last-revision", "drive-last-success" }) await vault.RemoveAsync(key);
    }
}
public sealed record RemoteBackup(string Id, string Name, long Size, DateTimeOffset CreatedUtc, string Device, int Schema);
public interface IBackupDrive
{
    Task<RemoteBackup> UploadAsync(BackupPackage package, CancellationToken cancellationToken = default);
    Task<List<RemoteBackup>> ListAsync(CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken cancellationToken = default);
}
public sealed class GoogleDriveClient(HttpClient http, GoogleAuthorization authorization, ITokenVault vault) : IBackupDrive
{
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await authorization.AccessTokenAsync());
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) { var status = response.StatusCode; response.Dispose(); throw new HttpRequestException($"Google Drive indisponível ({(int)status}). O backup continua pendente."); }
        return response;
    }
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
        using var multipart = new MultipartContent("related");
        multipart.Add(JsonContent.Create(new { name, parents = new[] { folder }, appProperties = new { device = package.Manifest.Device, schema = package.Manifest.Schema.ToString(), revision = package.Manifest.Revision.ToString() } }));
        var binary = new ByteArrayContent(package.Bytes); binary.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); multipart.Add(binary);
        using var response = await SendAsync(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name,size,createdTime,md5Checksum,appProperties", multipart, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); var root = json.RootElement;
        if (root.GetProperty("size").GetString() != package.Bytes.LongLength.ToString() || !root.GetProperty("md5Checksum").GetString()!.Equals(Convert.ToHexString(MD5.HashData(package.Bytes)), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("O Drive não confirmou a integridade do upload.");
        return Parse(root);
    }
    public async Task<List<RemoteBackup>> ListAsync(CancellationToken cancellationToken = default)
    {
        var folder = await BackupFolderAsync(cancellationToken); string? next = null; var list = new List<RemoteBackup>();
        do
        {
            var query = Uri.EscapeDataString($"'{Escape(folder)}' in parents and trashed=false and mimeType!='application/vnd.google-apps.folder'");
            var uri = "https://www.googleapis.com/drive/v3/files?q=" + query + "&fields=nextPageToken,files(id,name,size,createdTime,appProperties)&orderBy=createdTime%20desc&pageSize=100" + (next is null ? "" : "&pageToken=" + Uri.EscapeDataString(next));
            using var response = await SendAsync(HttpMethod.Get, uri, null, cancellationToken); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            list.AddRange(json.RootElement.GetProperty("files").EnumerateArray().Where(f => f.GetProperty("name").GetString()!.EndsWith(".gabackup", StringComparison.Ordinal)).Select(Parse));
            next = json.RootElement.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null;
        } while (next is not null);
        return list;
    }
    private static RemoteBackup Parse(JsonElement root)
    {
        var has = root.TryGetProperty("appProperties", out var props);
        return new(root.GetProperty("id").GetString()!, root.GetProperty("name").GetString()!, long.Parse(root.GetProperty("size").GetString()!), root.GetProperty("createdTime").GetDateTimeOffset(), has && props.TryGetProperty("device", out var d) ? d.GetString()! : "Desconhecido", has && props.TryGetProperty("schema", out var s) && int.TryParse(s.GetString(), out var schema) ? schema : 0);
    }
    public async Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken cancellationToken = default)
    {
        if (backup.Size > 260L * 1024 * 1024) throw new InvalidDataException("Backup excede o limite.");
        using var response = await SendAsync(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files/" + Uri.EscapeDataString(backup.Id) + "?alt=media", null, cancellationToken);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken); using var output = new MemoryStream(); var buffer = new byte[81920]; int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0) { if (output.Length + read > 260L * 1024 * 1024) throw new InvalidDataException("Backup excessivo."); await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken); }
        if (output.Length != backup.Size) throw new InvalidDataException("Download incompleto."); return output.ToArray();
    }
}
public sealed class BackupCoordinator(JournalStore store, BackupService backups, IBackupDrive drive, ITokenVault vault)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public string Status { get; private set; } = "Backup automático desativado.";
    private DateTimeOffset due;
    public bool Enabled { get; private set; }
    public void Enable() { Enabled = true; due = DateTimeOffset.UtcNow.AddSeconds(30); Status = "Backup pendente."; }
    public void Disable() { Enabled = false; Status = "Backup automático desativado."; }
    public void NotifyChange() { if (Enabled) { due = DateTimeOffset.UtcNow.AddSeconds(30); Status = "Alterações pendentes; aguardando 30 segundos."; } }
    public async Task TickAsync(string? password, string device, CancellationToken ct = default)
    {
        if (!Enabled || DateTimeOffset.UtcNow < due) return;
        var revision = await store.RevisionAsync();
        if (await vault.GetAsync("drive-last-revision") == revision.ToString()) return;
        if (string.IsNullOrEmpty(password)) { Status = "Backup pendente: informe a senha de recuperação nesta sessão."; return; }
        try { await RunAsync(password, device, ct); }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException) { Status = "Backup pendente; falha de conexão ou configuração. Tente novamente."; due = DateTimeOffset.UtcNow.AddMinutes(2); }
    }
    public async Task<RemoteBackup> RunAsync(string password, string device, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            Status = "Criando e enviando backup…";
            var package = await backups.CreateAsync(password, device); var result = await drive.UploadAsync(package, ct);
            await vault.SetAsync("drive-last-revision", package.Manifest.Revision.ToString()); await vault.SetAsync("drive-last-success", DateTimeOffset.UtcNow.ToString("O"));
            Status = "Último backup verificado: " + DateTimeOffset.Now.ToString("g"); return result;
        }
        catch { Status = "Backup pendente; envio não concluído."; throw; }
        finally { gate.Release(); }
    }
}
