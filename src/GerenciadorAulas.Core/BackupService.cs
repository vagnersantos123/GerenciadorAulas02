using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace GerenciadorAulas.Core;
public sealed record BackupFile(string Path, string Sha256, long Size);
public sealed record BackupManifest(int Schema, DateTimeOffset CreatedUtc, string Device, long Revision, List<BackupFile> Files);
public sealed record BackupPackage(byte[] Bytes, BackupManifest Manifest);
public sealed class BackupService(JournalStore store)
{
    private const int Limit = 256 * 1024 * 1024;
    private const int Iterations = 600_000;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("GABK0001");
    public async Task<BackupPackage> CreateAsync(string password, string device)
    {
        ValidatePassword(password);
        await store.InitializeAsync();
        await store.Gate.WaitAsync();
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        try
        {
            using var db = store.Open();
            using (var destination = new SqliteConnection($"Data Source={temp};Pooling=False")) { destination.Open(); db.BackupDatabase(destination); }
            if(new FileInfo(temp).Length>Limit)throw new InvalidDataException("O banco excede o limite de 256 MB desta versão.");
            var files = new Dictionary<string, byte[]> { ["journal.db"] = await File.ReadAllBytesAsync(temp) };
            long total = files["journal.db"].LongLength;
            foreach (var evidence in JournalStore.Read<Evidence>(db).DistinctBy(e => e.Hash))
            {
                if (evidence.Hash.Length != 64 || !evidence.Hash.All(Uri.IsHexDigit)) throw new InvalidDataException("Referência inválida.");
                var path = Path.Combine(store.AttachmentsPath, evidence.Hash);
                total += new FileInfo(path).Length;
                if (total > Limit) throw new InvalidDataException("Este backup excede o limite de 256 MB da primeira versão.");
                var bytes = await File.ReadAllBytesAsync(path);
                if (Hash(bytes) != evidence.Hash || bytes.LongLength != evidence.Size) throw new InvalidDataException("Evidência alterada ou ausente.");
                files.Add("attachments/" + evidence.Hash, bytes);
            }
            var manifest = new BackupManifest(JournalStore.SchemaVersion, DateTimeOffset.UtcNow, device, JournalStore.Revision(db), files.Select(f => new BackupFile(f.Key, Hash(f.Value), f.Value.LongLength)).ToList());
            using var zipBytes = new MemoryStream();
            using (var zip = new ZipArchive(zipBytes, ZipArchiveMode.Create, true))
            {
                foreach (var file in files) { using var output = zip.CreateEntry(file.Key, CompressionLevel.Optimal).Open(); output.Write(file.Value); }
                using var json = zip.CreateEntry("manifest.json").Open(); JsonSerializer.Serialize(json, manifest);
            }
            return new BackupPackage(await Task.Run(()=>Encrypt(zipBytes.ToArray(), password)), manifest);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); store.Gate.Release(); }
    }
    public async Task<BackupManifest> InspectAsync(byte[] encrypted, string password)
    {
        var staging = await ExtractAsync(encrypted, password);
        try { return staging.Manifest; } finally { Directory.Delete(staging.Path, true); }
    }
    public async Task<string> RestoreAsync(byte[] encrypted, string password, bool confirmed, bool acceptNewerLocal = false)
    {
        if (!confirmed) throw new InvalidOperationException("Confirme explicitamente a substituição dos dados locais.");
        var staging = await ExtractAsync(encrypted, password);
        try
        {
            var revision = await store.RevisionAsync();
            var newer = await store.ReadAsync(db =>
            {
                using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT Value FROM Metadata WHERE Key='last-change-utc'";
                return DateTimeOffset.TryParse(cmd.ExecuteScalar()?.ToString(),out var changed) && changed > staging.Manifest.CreatedUtc;
            });
            if (newer && !acceptNewerLocal) throw new InvalidOperationException("Há alterações locais posteriores ao backup. Confirmação adicional necessária.");
            var safety = await CreateAsync(password, "Segurança antes da restauração");
            var safetyPath = Path.Combine(Path.GetDirectoryName(store.DirectoryPath)!, $"seguranca-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.gabackup");
            await File.WriteAllBytesAsync(safetyPath, safety.Bytes);
            await store.Gate.WaitAsync();
            try
            {
                using (var db = store.Open()) if (JournalStore.Revision(db) != revision) throw new InvalidOperationException("Os dados mudaram durante a restauração. Tente novamente.");
                var rollback = store.DirectoryPath + ".rollback-" + Guid.NewGuid().ToString("N");
                Directory.Move(store.DirectoryPath, rollback);
                try { Directory.Move(staging.Path, store.DirectoryPath); }
                catch { Directory.Move(rollback, store.DirectoryPath); throw; }
                // The encrypted safety backup is retained; no plaintext duplicate is retained.
                Directory.Delete(rollback, true);
            }
            finally { store.Gate.Release(); }
            return safetyPath;
        }
        finally { if (Directory.Exists(staging.Path)) Directory.Delete(staging.Path, true); }
    }
    private async Task<(string Path, BackupManifest Manifest)> ExtractAsync(byte[] encrypted, string password)
    {
        ValidatePassword(password);
        var plain = await Task.Run(()=>Decrypt(encrypted, password));
        var directory = store.DirectoryPath + ".restore-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            using var stream = new MemoryStream(plain); using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            if (zip.Entries.Count > 10000 || zip.Entries.Sum(e => e.Length) > Limit) throw new InvalidDataException("Backup excede os limites.");
            if (zip.Entries.Select(e => e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zip.Entries.Count) throw new InvalidDataException("Entradas duplicadas.");
            var json = zip.GetEntry("manifest.json") ?? throw new InvalidDataException("Manifesto ausente.");
            if (json.Length > 2 * 1024 * 1024) throw new InvalidDataException("Manifesto excessivo.");
            using var manifestStream = json.Open(); var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream) ?? throw new InvalidDataException("Manifesto inválido.");
            if (manifest.Schema != JournalStore.SchemaVersion || manifest.Files.Count != zip.Entries.Count - 1 || !manifest.Files.Any(f => f.Path == "journal.db")) throw new InvalidDataException("Esquema ou conteúdo incompatível.");
            if (manifest.Files.Select(f => f.Path).Distinct().Count() != manifest.Files.Count) throw new InvalidDataException("Manifesto duplicado.");
            foreach (var file in manifest.Files)
            {
                if (file.Path != "journal.db" && !(file.Path.StartsWith("attachments/", StringComparison.Ordinal) && file.Path.Length == 76 && file.Path[12..].All(Uri.IsHexDigit))) throw new InvalidDataException("Caminho não permitido.");
                var entry = zip.GetEntry(file.Path) ?? throw new InvalidDataException("Arquivo ausente.");
                if (entry.Length != file.Size || file.Size < 0) throw new InvalidDataException("Tamanho inválido.");
                using var input = entry.Open(); using var content = new MemoryStream();var buffer=new byte[81920];int read;
                while((read=await input.ReadAsync(buffer))>0){if(content.Length+read>file.Size||content.Length+read>Limit)throw new InvalidDataException("Conteúdo descompactado excessivo.");await content.WriteAsync(buffer.AsMemory(0,read));}
                var bytes = content.ToArray(); if (Hash(bytes) != file.Sha256) throw new InvalidDataException("Integridade inválida.");
                var target = Path.Combine(directory, file.Path); Directory.CreateDirectory(Path.GetDirectoryName(target)!); await File.WriteAllBytesAsync(target, bytes);
            }
            using var db = new SqliteConnection($"Data Source={Path.Combine(directory, "journal.db")};Pooling=False;Foreign Keys=True"); db.Open();
            using var check = db.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
            if ((string?)check.ExecuteScalar() != "ok") throw new InvalidDataException("Banco danificado.");
            check.CommandText = "PRAGMA user_version"; if (Convert.ToInt32(check.ExecuteScalar()) != manifest.Schema) throw new InvalidDataException("Versão do banco inválida.");
            check.CommandText = "PRAGMA foreign_key_check"; using (var reader = check.ExecuteReader()) if (reader.Read()) throw new InvalidDataException("Vínculos inconsistentes.");
            foreach (var e in JournalStore.Read<Evidence>(db)) if (!manifest.Files.Any(f => f.Path == "attachments/" + e.Hash && f.Sha256 == e.Hash && f.Size == e.Size)) throw new InvalidDataException("Evidência ausente do manifesto.");
            Directory.CreateDirectory(Path.Combine(directory, "attachments"));
            return (directory, manifest);
        }
        catch { Directory.Delete(directory, true); throw; }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    private static void ValidatePassword(string password) { if (password.Length < 12) throw new ArgumentException("Use uma senha de recuperação com pelo menos 12 caracteres."); }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static byte[] Encrypt(byte[] plain, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var cipher = new byte[plain.Length];
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        try { using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plain, cipher, tag, Magic); return Magic.Concat(salt).Concat(nonce).Concat(tag).Concat(cipher).ToArray(); }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }
    private static byte[] Decrypt(byte[] encrypted, string password)
    {
        if (encrypted.Length < 52 || encrypted.Length > Limit + 4 * 1024 * 1024 || !encrypted.AsSpan(0, 8).SequenceEqual(Magic)) throw new InvalidDataException("Formato de backup inválido.");
        var key = Rfc2898DeriveBytes.Pbkdf2(password, encrypted.AsSpan(8, 16), Iterations, HashAlgorithmName.SHA256, 32); var plain = new byte[encrypted.Length - 52];
        try { using var aes = new AesGcm(key, 16); aes.Decrypt(encrypted.AsSpan(24, 12), encrypted.AsSpan(52), encrypted.AsSpan(36, 16), plain, Magic); return plain; }
        catch (CryptographicException ex) { throw new InvalidDataException("Senha incorreta ou backup danificado.", ex); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
