using System.Globalization;
using System.Text;

namespace GerenciadorAulas.Core;

public static class StudyWeek
{
    public static readonly (DayOfWeek Day, string Name)[] Options =
    [
        (DayOfWeek.Monday, "Segunda-feira"), (DayOfWeek.Tuesday, "Terça-feira"),
        (DayOfWeek.Wednesday, "Quarta-feira"), (DayOfWeek.Thursday, "Quinta-feira"),
        (DayOfWeek.Friday, "Sexta-feira"), (DayOfWeek.Saturday, "Sábado"), (DayOfWeek.Sunday, "Domingo")
    ];
    public static DayOfWeek[] Normalize(IEnumerable<DayOfWeek> days)
    {
        var selected = days.ToHashSet();
        if (selected.Count == 0 || selected.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Selecione pelo menos um dia de estudo válido.");
        return Options.Where(option => selected.Contains(option.Day)).Select(option => option.Day).ToArray();
    }
}

public sealed record SubjectImport(string CatalogItemId, int LessonCount, int LessonsPerDay, DayOfWeek[] StudyDays, int? EstimatedMinutes = null);
public sealed record SubjectImportResult(int Added, int Skipped);

public sealed class SubjectCatalogService(JournalStore store)
{
    public static SubjectCatalog Initial() => new()
    {
        Id = SubjectCatalog.RecordId,
        Items = [new("portugues", "Português"), new("matematica", "Matemática"), new("historia", "História"),
            new("geografia", "Geografia"), new("ciencias", "Ciências"), new("artes", "Artes"), new("ingles", "Inglês"),
            new("educacao-fisica", "Educação Física"), new("ensino-religioso", "Ensino Religioso")]
    };
    public static string NameKey(string name) => string.Concat(name.Trim().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .Select(c => char.IsWhiteSpace(c) ? ' ' : char.ToUpperInvariant(c)))
        .Split(' ', StringSplitOptions.RemoveEmptyEntries).Aggregate("", (result, part) => result.Length == 0 ? part : result + " " + part);

    // Reading the virtual initial catalog never writes into an existing journal.
    public async Task<SubjectCatalog> GetAsync() => (await store.AllAsync<SubjectCatalog>()).SingleOrDefault() ?? Initial();
    public Task SaveAsync(SubjectCatalog catalog) => store.MutateAsync((db, tx) =>
    {
        if (catalog.Id != SubjectCatalog.RecordId || catalog.Items.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name)) ||
            catalog.Items.Select(item => item.Id).Distinct().Count() != catalog.Items.Count ||
            catalog.Items.Select(item => NameKey(item.Name)).Distinct().Count() != catalog.Items.Count)
            throw new ArgumentException("Informe nomes únicos e válidos para o catálogo.");
        JournalStore.Put(db, tx, catalog with { Items = catalog.Items.Select(item => item with { Name = item.Name.Trim() }).ToList() });
    });
    public async Task<SubjectImportResult> ImportAsync(string yearId, IReadOnlyList<SubjectImport> selections)
    {
        var added = 0; var skipped = 0;
        await store.MutateAsync((db, tx) =>
        {
            if (JournalStore.Find(db, tx, yearId) is not SchoolYear) throw new ArgumentException("Selecione um ano letivo válido.");
            if (selections.Count == 0) throw new ArgumentException("Selecione pelo menos uma matéria.");
            var catalog = JournalStore.Read<SubjectCatalog>(db, tx).SingleOrDefault() ?? Initial();
            var existing = JournalStore.Read<Subject>(db, tx).Where(s => s.YearId == yearId).ToList();
            foreach (var selection in selections)
            {
                var item = catalog.Items.SingleOrDefault(item => item.Id == selection.CatalogItemId) ?? throw new ArgumentException("Matéria não encontrada no catálogo. Reabra a seleção.");
                var days = StudyWeek.Normalize(selection.StudyDays);
                if (selection.LessonCount is < 1 or > 5000 || selection.LessonsPerDay is < 1 or > 50 || selection.EstimatedMinutes is < 0)
                    throw new ArgumentException("Informe de 1 a 5000 lições e de 1 a 50 lições por dia para cada matéria.");
                if (existing.Any(s => s.CatalogItemId == item.Id || NameKey(s.Name) == NameKey(item.Name))) { skipped++; continue; }
                var subject = new Subject { YearId = yearId, CatalogItemId = item.Id, Name = item.Name, LessonCount = selection.LessonCount,
                    LessonsPerDay = selection.LessonsPerDay, StudyDays = days, EstimatedLessonMinutes = selection.EstimatedMinutes };
                JournalStore.Put(db, tx, subject);
                for (var n = 1; n <= subject.LessonCount; n++) JournalStore.Put(db, tx, new Lesson { SubjectId = subject.Id, Number = n });
                existing.Add(subject); added++;
            }
        });
        return new(added, skipped);
    }
}
