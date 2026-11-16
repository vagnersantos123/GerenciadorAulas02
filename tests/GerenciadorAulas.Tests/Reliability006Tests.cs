using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using GerenciadorAulas.Core;
using Xunit;

namespace GerenciadorAulas.Tests;

public sealed class Reliability006Tests : IAsyncLifetime
{
    private const string Password = "Recuperacao ficticia V006!";
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-006-" + Guid.NewGuid());
    private JournalStore source = null!;
    private JournalStore target = null!;
    private BackupService backups = null!;
    public async Task InitializeAsync()
    {
        source = new(Path.Combine(root, "source")); target = new(Path.Combine(root, "target"));
        var service = new EducationService(source);
        var student = new Student { Name = "Aluno fictício V006" }; await service.SaveAsync(student);
        var year = new SchoolYear { StudentId = student.Id, Name = "2026", Start = new(2026, 2, 2), End = new(2026, 12, 15) }; await service.SaveAsync(year);
        var subject = new Subject { YearId = year.Id, Name = "Ciências fictícias", LessonCount = 3, EstimatedLessonMinutes = 45 }; await service.SaveAsync(subject);
        await service.GeneratePlanAsync(subject.Id);
        var session = new StudySession { SubjectId = subject.Id, LessonIds = [(await source.AllAsync<Lesson>())[0].Id], Date = new(2026, 2, 2), Minutes = 50, EstimatedMinutes = 45 };
        await service.SaveAsync(session);
        await new EvidenceService(source).ImportAsync(session.Id, "ficticio.txt", new MemoryStream([1, 3, 5, 7]), false);
        await new EducationService(target).SaveAsync(new Student { Name = "Preservar destino fictício" });
        backups = new(source);
    }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root, true); return Task.CompletedTask; }
    private async Task AssertSameAsync<T>(JournalStore reopened) where T : Entry =>
        Assert.Equal((await source.AllAsync<T>()).Select(x => JsonSerializer.Serialize(x)), (await reopened.AllAsync<T>()).Select(x => JsonSerializer.Serialize(x)));
    private string Fingerprint() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target.DatabasePath)));

    [Fact] public async Task CrossDeviceRestorePreservesRecordsPlansTimingAndAttachmentAfterReopen()
    {
        var package = await backups.CreateAsync(Password, "Origem fictícia");
        var safety = await new BackupService(target).RestoreAsync(package.Bytes, Password, true, true);
        var reopened = new JournalStore(target.DirectoryPath);
        await AssertSameAsync<Student>(reopened); await AssertSameAsync<SchoolYear>(reopened);
        await AssertSameAsync<Subject>(reopened); await AssertSameAsync<Lesson>(reopened);
        await AssertSameAsync<PlannedLesson>(reopened); await AssertSameAsync<StudySession>(reopened); await AssertSameAsync<Evidence>(reopened);
        var evidence = Assert.Single(await reopened.AllAsync<Evidence>());
        Assert.Equal(new byte[] { 1, 3, 5, 7 }, await File.ReadAllBytesAsync(await new EvidenceService(reopened).VerifiedPathAsync(evidence)));
        var safetyTarget = new JournalStore(Path.Combine(root, "safety-check"));
        await new BackupService(safetyTarget).RestoreAsync(await File.ReadAllBytesAsync(safety), Password, true, true);
        Assert.Equal("Preservar destino fictício", Assert.Single(await safetyTarget.AllAsync<Student>()).Name);
    }

    [Theory] [InlineData("password")] [InlineData("truncated")] [InlineData("ciphertext")] [InlineData("header")]
    public async Task InvalidRestoreLeavesExistingDatabaseByteIdentical(string fault)
    {
        var package = await backups.CreateAsync(Password, "Origem fictícia"); var bytes = package.Bytes.ToArray();
        var password = Password;
        if (fault == "password") password = "Senha incorreta ficticia!";
        if (fault == "truncated") bytes = bytes[..^17];
        if (fault == "ciphertext") bytes[^1] ^= 1;
        if (fault == "header") bytes[0] ^= 1;
        var before = Fingerprint();
        await Assert.ThrowsAsync<InvalidDataException>(() => new BackupService(target).RestoreAsync(bytes, password, true, true));
        Assert.Equal(before, Fingerprint());
        Assert.Equal("Preservar destino fictício", Assert.Single(await target.AllAsync<Student>()).Name);
        Assert.Empty(Directory.GetDirectories(root, "target.restore-*"));
    }

    [Fact] public async Task MissingOrChangedAttachmentPreventsBackupWithoutDamagingRecords()
    {
        var evidence = Assert.Single(await source.AllAsync<Evidence>());
        var path = await new EvidenceService(source).VerifiedPathAsync(evidence);
        await File.WriteAllBytesAsync(path, [9, 9, 9, 9]);
        await Assert.ThrowsAsync<InvalidDataException>(() => backups.CreateAsync(Password, "Fictício"));
        Assert.Single(await source.AllAsync<StudySession>());
    }

    [Fact] public async Task OfflineWithoutGoogleStillSavesAndReopensStudySessions()
    {
        var vault = new Vault(); var auth = new GoogleAuthorization(new HttpClient(new NoNetwork()), vault);
        Assert.False(await auth.HasSessionAsync());
        var reopened = new JournalStore(source.DirectoryPath); var service = new EducationService(reopened);
        var original = Assert.Single(await reopened.AllAsync<StudySession>());
        await service.SaveAsync(original with { Id = Guid.NewGuid().ToString("N"), Minutes = 30 });
        Assert.Equal(2, (await new JournalStore(source.DirectoryPath).AllAsync<StudySession>()).Count);
        Assert.False(Assert.Single(await reopened.AllAsync<Lesson>(), l => l.Id == original.LessonIds[0]).Completed);
    }

    [Fact] public async Task TransportTimeoutInAutomaticBackupRemainsPendingWithoutEscapingTimer()
    {
        var vault = new Vault(); var clock = new Clock();
        var drive = new InterruptedDrive(); var coordinator = new BackupCoordinator(source, backups, drive, vault, clock);
        await coordinator.ConfigureAsync(true, 30); clock.Advance();
        await coordinator.TickAsync(Password, "Fictício");
        Assert.Contains("pendente", coordinator.Status);
        Assert.Null(await vault.GetAsync("drive-last-success")); Assert.Null(await vault.GetAsync("drive-last-backup"));
        Assert.Single(await source.AllAsync<StudySession>());
        clock.Advance(31); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(1, drive.Calls);
        clock.Advance(90); await coordinator.TickAsync(Password, "Fictício"); Assert.Equal(2, drive.Calls);
    }

    [Fact] public async Task ExplicitCancellationNeverMarksInterruptedUploadAsSuccessful()
    {
        var vault = new Vault(); var coordinator = new BackupCoordinator(source, backups, new InterruptedDrive(), vault);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.RunAsync(Password, "Fictício", cancellation.Token));
        Assert.Null(await vault.GetAsync("drive-last-success")); Assert.Null(await vault.GetAsync("drive-last-revision"));
        Assert.Single(await source.AllAsync<StudySession>());
    }

    [Theory] [InlineData("future-schema")] [InlineData("hash")] [InlineData("missing-attachment")]
    public async Task AuthenticatedButIncompatibleArchiveCannotReplaceExistingData(string fault)
    {
        var package = await backups.CreateAsync(Password, "Fictício");
        var edited = EditArchive(package.Bytes, fault); var before = Fingerprint();
        await Assert.ThrowsAsync<InvalidDataException>(() => new BackupService(target).RestoreAsync(edited, Password, true, true));
        Assert.Equal(before, Fingerprint());
        Assert.Empty(Directory.GetDirectories(root, "target.restore-*"));
    }

    // Produce an authenticated fixture with invalid contents, to exercise validation beyond AES-GCM.
    private static byte[] EditArchive(byte[] encrypted, string fault)
    {
        var key = Rfc2898DeriveBytes.Pbkdf2(Password, encrypted.AsSpan(8, 16), 600_000, HashAlgorithmName.SHA256, 32);
        var plain = new byte[encrypted.Length - 52];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(encrypted.AsSpan(24, 12), encrypted.AsSpan(52), encrypted.AsSpan(36, 16), plain, encrypted.AsSpan(0, 8));
        using var input = new MemoryStream(plain); using var sourceZip = new ZipArchive(input, ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var entry in sourceZip.Entries)
            {
                if (fault == "missing-attachment" && entry.FullName.StartsWith("attachments/", StringComparison.Ordinal)) continue;
                using var from = entry.Open(); using var to = zip.CreateEntry(entry.FullName).Open();
                if (entry.FullName != "manifest.json") { from.CopyTo(to); continue; }
                var manifest = JsonNode.Parse(from)!;
                if (fault == "future-schema") manifest["Schema"] = JournalStore.SchemaVersion + 1;
                if (fault == "hash") manifest["Files"]![0]!["Sha256"] = new string('0', 64);
                to.Write(Encoding.UTF8.GetBytes(manifest.ToJsonString()));
            }
        }
        var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16];
        var newKey = Rfc2898DeriveBytes.Pbkdf2(Password, salt, 600_000, HashAlgorithmName.SHA256, 32);
        var changed = output.ToArray(); var cipher = new byte[changed.Length];
        using var encoder = new AesGcm(newKey, 16);
        encoder.Encrypt(nonce, changed, cipher, tag, encrypted.AsSpan(0, 8));
        CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(newKey); CryptographicOperations.ZeroMemory(plain);
        return encrypted[..8].Concat(salt).Concat(nonce).Concat(tag).Concat(cipher).ToArray();
    }
    private sealed class Clock : TimeProvider { private DateTimeOffset now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => now; public void Advance(int seconds = 60) => now = now.AddSeconds(seconds); }
    private sealed class InterruptedDrive : IBackupDrive
    {
        public int Calls;
        public Task<RemoteBackup> UploadAsync(BackupPackage package, CancellationToken ct = default) { Calls++; throw new TaskCanceledException("Timeout fictício de transporte"); }
        public Task<List<RemoteBackup>> ListAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<byte[]> DownloadAsync(RemoteBackup backup, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class NoNetwork : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => throw new InvalidOperationException("Este teste não permite rede."); }
    private sealed class Vault : ITokenVault
    {
        private readonly Dictionary<string, string> values = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string key) { values.Remove(key); return Task.CompletedTask; }
    }
}
