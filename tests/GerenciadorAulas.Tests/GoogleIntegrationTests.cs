using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GerenciadorAulas.Core;
using Xunit;
namespace GerenciadorAulas.Tests;

public sealed class GoogleIntegrationTests : IAsyncLifetime
{
    private const string Password = "Recuperacao ficticia V005!";
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-005-" + Guid.NewGuid());
    private JournalStore store = null!;
    private BackupService backups = null!;
    public async Task InitializeAsync() { store = new(root); backups = new(store); await new EducationService(store).SaveAsync(new Student { Name = "Aluno fictício V005" }); }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root, true); return Task.CompletedTask; }
    [Fact] public async Task WelcomeChoicePersistsAndConnectedUsersAreNotPrompted()
    {
        var vault = new Vault(); var welcome = new GoogleOnboarding(vault); Assert.True(await welcome.ShouldOfferAsync());
        await welcome.CompleteAsync(); Assert.False(await new GoogleOnboarding(vault).ShouldOfferAsync());
        var connected = new Vault(); await SeedAsync(connected); Assert.False(await new GoogleOnboarding(connected).ShouldOfferAsync());
    }
    [Fact] public async Task DesktopExchangeUsesPkceAndRefreshRetainsRefreshTokenWithoutInteractiveRequest()
    {
        var vault = new Vault(); var calls = 0; var challenge = OAuthChallenge.Create();
        using var http = new HttpClient(new Handler(async request =>
        {
            calls++; Assert.Equal("https://oauth2.googleapis.com/token", request.RequestUri!.ToString()); var body = await request.Content!.ReadAsStringAsync();
            if (calls == 1) { Assert.Contains("code_verifier=" + challenge.Verifier, body); Assert.Contains("authorization_code", body); return Json(new { access_token = "synthetic-first", refresh_token = "synthetic-refresh", expires_in = 1, scope = GoogleAuthorization.Scope }); }
            Assert.Contains("synthetic-refresh", body); return Json(new { access_token = "synthetic-renewed", expires_in = 3600 });
        }));
        var auth = new GoogleAuthorization(http, vault); await auth.ExchangeAsync("synthetic-code", challenge.State, challenge, "synthetic-desktop-id", "http://127.0.0.1:12345/");
        Assert.Equal("synthetic-renewed", await auth.AccessTokenAsync()); Assert.Equal("synthetic-renewed", await auth.AccessTokenAsync()); Assert.Equal(2, calls);
        Assert.Equal("synthetic-refresh", JsonSerializer.Deserialize<OAuthTokens>((await vault.GetAsync("google-tokens"))!)!.RefreshToken);
    }
    [Fact] public async Task DeniedDriveScopeDoesNotReplaceExistingCredentials()
    {
        var vault = new Vault(); await SeedAsync(vault); var before = await vault.GetAsync("google-tokens");
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Json(new { access_token = "synthetic-denied", expires_in = 3600, scope = "email" }))));
        var auth = new GoogleAuthorization(http, vault); var challenge = OAuthChallenge.Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.ExchangeAsync("code", challenge.State, challenge, "client", "redirect")); Assert.Equal(before, await vault.GetAsync("google-tokens"));
    }
    [Fact] public async Task NativeRefreshIsSilentPinnedToAccountAndReconnectClearsOldDriveState()
    {
        var vault = new Vault(); var native = new Native(); using var http = new HttpClient(new Handler(_ => throw new Xunit.Sdk.XunitException("Native flow must not exchange server credentials")));
        var auth = new GoogleAuthorization(http, vault, native); await vault.SetAsync("drive-folder", "old-folder"); await vault.SetAsync("drive-auto-enabled", "true");
        await auth.ConnectNativeAsync(); Assert.Null(await vault.GetAsync("drive-folder")); Assert.Null(await vault.GetAsync("drive-auto-enabled")); Assert.True(native.Interactive);
        await auth.RememberAccountAsync(new("Conta fictícia", "ficticio@example.invalid", "fake")); await auth.InvalidateAsync();
        Assert.Equal("synthetic-native", await auth.AccessTokenAsync()); Assert.False(native.Interactive); Assert.Equal("ficticio@example.invalid", native.Account); Assert.Equal(2, native.Calls);
    }
    [Fact] public async Task NativeRenewalRequiringConsentStaysPendingWithoutOpeningUi()
    {
        var vault = new Vault(); var native = new Native(); using var http = new HttpClient(); var auth = new GoogleAuthorization(http, vault, native);
        await auth.ConnectNativeAsync(); await auth.RememberAccountAsync(new("Teste", "ficticio@example.invalid", null)); await auth.InvalidateAsync(); native.FailSilent = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.AccessTokenAsync()); Assert.False(native.Interactive);
    }
    [Fact] public async Task OfflineDisconnectClearsLocalCredentialsAndReportsUnconfirmedRemoteRevocation()
    {
        var vault = new Vault(); await SeedAsync(vault); await vault.SetAsync("drive-folder", "old"); await vault.SetAsync("google-desktop-secret", "synthetic-secret");
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("Offline simulado"))); var auth = new GoogleAuthorization(http, vault);
        Assert.False(await auth.DisconnectAsync()); Assert.False(await auth.HasSessionAsync()); Assert.Null(await vault.GetAsync("drive-folder")); Assert.Null(await vault.GetAsync("google-desktop-secret"));
    }
    [Fact] public async Task AccountUsesDriveFileAndDoesNotUploadEducationalData()
    {
        var vault = new Vault(); await SeedAsync(vault); using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Contains("/drive/v3/about", request.RequestUri!.AbsolutePath); Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            return Task.FromResult(Json(new { user = new { displayName = "Conta fictícia", emailAddress = "ficticio@example.invalid", permissionId = "fake" } }));
        }));
        var auth = new GoogleAuthorization(http, vault); var drive = new GoogleDriveClient(http, auth, vault);
        Assert.Equal("ficticio@example.invalid", (await drive.GetAccountAsync()).Email); Assert.Equal("Conta fictícia", (await auth.AccountAsync())!.DisplayName);
    }
    [Fact] public async Task UnauthorizedInvalidatesCachedTokenWithoutRetryingAnUpload()
    {
        var vault = new Vault(); await SeedAsync(vault); var calls = 0; using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)); }));
        var auth = new GoogleAuthorization(http, vault); var drive = new GoogleDriveClient(http, auth, vault);
        await Assert.ThrowsAsync<HttpRequestException>(() => drive.GetAccountAsync()); Assert.Equal(1, calls);
        Assert.Equal(DateTimeOffset.MinValue, JsonSerializer.Deserialize<OAuthTokens>((await vault.GetAsync("google-tokens"))!)!.ExpiresUtc);
    }
    [Theory] [InlineData(true)] [InlineData(false)] public async Task IncompleteOrCorruptDownloadIsRejected(bool incomplete)
    {
        var vault = new Vault(); await SeedAsync(vault); var bytes = new byte[] { 1, 2, 3 };
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) })));
        var drive = new GoogleDriveClient(http, new(http, vault), vault);
        var remote = new RemoteBackup("fake", "fake.gabackup", incomplete ? 5 : 3, DateTimeOffset.UtcNow, "Fictício", 1, incomplete ? null : new string('0', 64));
        await Assert.ThrowsAsync<InvalidDataException>(() => drive.DownloadAsync(remote));
    }
    [Fact] public async Task VerifiedRemoteBackupRestoresOnSecondDisposableDeviceWithEncryptedSafetyCopy()
    {
        var package = await backups.CreateAsync(Password, "Dispositivo A fictício"); var vault = new Vault(); await SeedAsync(vault);
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(package.Bytes) })));
        var drive = new GoogleDriveClient(http, new(http, vault), vault);
        var remote = new RemoteBackup("fake", "fake.gabackup", package.Bytes.Length, package.Manifest.CreatedUtc, "Dispositivo A fictício", 1, Convert.ToHexString(SHA256.HashData(package.Bytes)));
        var downloaded = await drive.DownloadAsync(remote); var destination = new JournalStore(Path.Combine(root, "device-b")); var restore = new BackupService(destination);
        await new EducationService(destination).SaveAsync(new Student { Name = "Antes da restauração" });
        var manifest = await restore.InspectAsync(downloaded, Password); Assert.Equal(1, manifest.Schema);
        await Assert.ThrowsAsync<InvalidDataException>(() => restore.InspectAsync(downloaded, "Senha incorreta ficticia")); Assert.Equal("Antes da restauração", Assert.Single(await destination.AllAsync<Student>()).Name);
        await Assert.ThrowsAsync<InvalidOperationException>(() => restore.RestoreAsync(downloaded, Password, false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => restore.RestoreAsync(downloaded, Password, true));
        var safety = await restore.RestoreAsync(downloaded, Password, true, true);
        try { Assert.True(File.Exists(safety)); Assert.Equal("Aluno fictício V005", Assert.Single(await destination.AllAsync<Student>()).Name); var safeManifest = await restore.InspectAsync(await File.ReadAllBytesAsync(safety), Password); Assert.Contains("Segurança", safeManifest.Device); }
        finally { File.Delete(safety); }
    }
    [Fact] public async Task AutomaticBackupIsOptInDebouncedThrottledAndPersisted()
    {
        var vault = new Vault(); var drive = new FakeDrive(); var clock = new Clock(); var coordinator = new BackupCoordinator(store, backups, drive, vault, clock);
        await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(0, drive.Uploads);
        await coordinator.ConfigureAsync(true, 120); clock.Advance(29); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(0, drive.Uploads);
        clock.Advance(2); await coordinator.TickAsync(null, "Fictício"); Assert.Equal(0, drive.Uploads);
        await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(1, drive.Uploads); Assert.NotNull(await vault.GetAsync("drive-last-backup"));
        await new EducationService(store).SaveAsync(new Student { Name = "Mudança fictícia" }); coordinator.NotifyChange(); clock.Advance(31); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(1, drive.Uploads);
        clock.Advance(90); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(2, drive.Uploads);
        clock.Advance(500); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(2, drive.Uploads);
        var reopened = new BackupCoordinator(store, backups, drive, vault, clock); await reopened.InitializeAsync(); Assert.True(reopened.Enabled); Assert.Equal(120, reopened.MinimumIntervalSeconds);
    }
    [Fact] public async Task OfflineAutoBackupRetriesLaterWithoutMarkingSuccessOrChangingLocalData()
    {
        var vault = new Vault(); var drive = new FakeDrive { Offline = true }; var clock = new Clock(); var coordinator = new BackupCoordinator(store, backups, drive, vault, clock);
        var before = SHA256.HashData(File.ReadAllBytes(store.DatabasePath)); await coordinator.ConfigureAsync(true); clock.Advance(31);
        await coordinator.TickAsync(Password, "Fictício"); Assert.Contains("pendente", coordinator.Status); Assert.Null(await vault.GetAsync("drive-last-success")); Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(store.DatabasePath)));
        drive.Offline = false; clock.Advance(30); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(1, drive.Uploads);
        clock.Advance(100); await coordinator.TickAsync(Password, "Fictício"); Assert.NotNull(await vault.GetAsync("drive-last-success")); Assert.Equal(2, drive.Uploads);
    }
    [Fact] public async Task LargeUploadUsesVerifiedChunksAndRecoversAcknowledgedProgress()
    {
        var bytes = new byte[6 * 1024 * 1024 + 3]; RandomNumberGenerator.Fill(bytes); var vault = new Vault(); await SeedAsync(vault); await vault.SetAsync("drive-folder", "fake-folder");
        var sent = new MemoryStream(); var puts = 0; var failedOnce = false;
        using var http = new HttpClient(new Handler(async request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                Assert.Contains("uploadType=resumable", request.RequestUri!.Query); Assert.Equal(bytes.Length.ToString(), request.Headers.GetValues("X-Upload-Content-Length").Single());
                var begin = new HttpResponseMessage(HttpStatusCode.OK); begin.Headers.Location = new("https://www.googleapis.com/upload/drive/v3/files?upload_id=synthetic"); return begin;
            }
            puts++; var range = request.Content!.Headers.ContentRange!;
            if (range.From is null) { Assert.Equal(0, request.Content.Headers.ContentLength); return Progress(); }
            Assert.Equal(sent.Length, range.From); var chunk = await request.Content.ReadAsByteArrayAsync(); Assert.True(chunk.Length <= 1024 * 1024); sent.Write(chunk);
            if (!failedOnce) { failedOnce = true; throw new HttpRequestException("Resposta perdida simulada"); }
            if (sent.Length < bytes.Length) return Progress();
            return Json(new { id = "fake", name = "large.gabackup", size = bytes.Length.ToString(), createdTime = DateTimeOffset.UtcNow, md5Checksum = Convert.ToHexString(MD5.HashData(bytes)), appProperties = new { schema = "1", device = "Fictício", sha256 = Convert.ToHexString(SHA256.HashData(bytes)) } });
            HttpResponseMessage Progress() { var response = new HttpResponseMessage((HttpStatusCode)308); response.Headers.TryAddWithoutValidation("Range", "bytes=0-" + (sent.Length - 1)); return response; }
        }));
        var drive = new GoogleDriveClient(http, new(http, vault), vault); var result = await drive.UploadAsync(new(bytes, new(1, DateTimeOffset.UtcNow, "Fictício", 1, [])));
        Assert.Equal(bytes.Length, result.Size); Assert.Equal(bytes, sent.ToArray()); Assert.Equal(8, puts);
    }
    [Fact] public async Task ResumableSessionRejectsAnUntrustedDestinationBeforeSendingBackup()
    {
        var vault = new Vault(); await SeedAsync(vault); await vault.SetAsync("drive-folder", "fake"); var calls = 0;
        using var http = new HttpClient(new Handler(_ => { calls++; var response = new HttpResponseMessage(HttpStatusCode.OK); response.Headers.Location = new("https://example.invalid/upload"); return Task.FromResult(response); }));
        var drive = new GoogleDriveClient(http, new(http, vault), vault);
        await Assert.ThrowsAsync<InvalidDataException>(() => drive.UploadAsync(new(new byte[6 * 1024 * 1024], new(1, DateTimeOffset.UtcNow, "Fictício", 1, [])))); Assert.Equal(1, calls);
    }

    private static Task SeedAsync(Vault vault) => vault.SetAsync("google-tokens", JsonSerializer.Serialize(new OAuthTokens("synthetic-access", "synthetic-refresh", DateTimeOffset.UtcNow.AddHours(1))));
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handler(request); }
    private sealed class Vault : ITokenVault
    {
        private readonly Dictionary<string,string> values = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key)); public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; } public Task RemoveAsync(string key) { values.Remove(key); return Task.CompletedTask; }
    }
    private sealed class Native : INativeGoogleAuthorization
    {
        public bool Interactive; public string? Account; public int Calls; public bool FailSilent;
        public Task<OAuthTokens> AuthorizeAsync(bool interactive, string? account) { Calls++; Interactive = interactive; Account = account; if (!interactive && FailSilent) throw new InvalidOperationException("Consentimento necessário"); return Task.FromResult(new OAuthTokens("synthetic-native", null, DateTimeOffset.UtcNow.AddHours(1))); }
        public Task RevokeAsync(string? account) => Task.CompletedTask;
    }
    private sealed class Clock : TimeProvider { private DateTimeOffset now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => now; public void Advance(int seconds) => now = now.AddSeconds(seconds); }
    private sealed class FakeDrive : IBackupDrive
    {
        public int Uploads; public bool Offline;
        public Task<RemoteBackup> UploadAsync(BackupPackage package, CancellationToken ct = default) { Uploads++; if (Offline) throw new HttpRequestException("Offline simulado"); return Task.FromResult(new RemoteBackup("fake", "fake.gabackup", package.Bytes.Length, package.Manifest.CreatedUtc, package.Manifest.Device, package.Manifest.Schema)); }
        public Task<List<RemoteBackup>> ListAsync(CancellationToken ct = default) => Task.FromResult(new List<RemoteBackup>()); public Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
