using System.Security.Cryptography;

namespace GerenciadorAulas.Core;
public sealed class EvidenceService(JournalStore store)
{
    public const long MaximumFileSize = 50 * 1024 * 1024;
    public async Task<Evidence> ImportAsync(string recordId, string name, Stream input, bool sensitive)
    {
        Directory.CreateDirectory(store.AttachmentsPath);
        var temporary = Path.Combine(store.AttachmentsPath, Guid.NewGuid() + ".tmp");
        try
        {
            long size = 0;
            await using (var output = File.Create(temporary))
            {
                var buffer = new byte[81920]; int read;
                while ((read = await input.ReadAsync(buffer)) > 0)
                {
                    size += read;
                    if (size > MaximumFileSize) throw new InvalidDataException("O limite por arquivo é 50 MB.");
                    await output.WriteAsync(buffer.AsMemory(0, read));
                }
            }
            string hash;
            await using (var file = File.OpenRead(temporary)) hash = Convert.ToHexString(await SHA256.HashDataAsync(file)).ToLowerInvariant();
            var evidence = new Evidence { RecordId = recordId, FileName = Path.GetFileName(name), Hash = hash, Size = size, Sensitive = sensitive };
            await store.MutateAsync((db, tx) =>
            {
                if (JournalStore.Find(db, tx, recordId) is not (StudySession or Activity or Occurrence or Difficulty)) throw new ArgumentException("Vincule a evidência a um estudo, atividade, ocorrência ou dificuldade.");
                var target = Path.Combine(store.AttachmentsPath, hash);
                if (!File.Exists(target)) File.Move(temporary, target);
                JournalStore.Put(db, tx, evidence);
            });
            return evidence;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public async Task<string> VerifiedPathAsync(Evidence evidence)
    {
        if (evidence.Hash.Length != 64 || !evidence.Hash.All(Uri.IsHexDigit)) throw new InvalidDataException("Referência inválida.");
        var path = Path.Combine(store.AttachmentsPath, evidence.Hash);
        await using var input = File.OpenRead(path);
        if (!Convert.ToHexString(await SHA256.HashDataAsync(input)).Equals(evidence.Hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Arquivo alterado ou danificado.");
        return path;
    }
}
