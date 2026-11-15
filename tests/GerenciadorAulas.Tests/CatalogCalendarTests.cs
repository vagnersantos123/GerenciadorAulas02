using System.Security.Cryptography;
using System.Text.Json;
using GerenciadorAulas.Core;
using Microsoft.Data.Sqlite;
using Xunit;
namespace GerenciadorAulas.Tests;

public sealed class CatalogCalendarTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-004-" + Guid.NewGuid());
    private JournalStore store = null!;
    private EducationService education = null!;
    private SubjectCatalogService catalog = null!;
    private SchoolYear year = null!;
    public async Task InitializeAsync()
    {
        store = new(root); education = new(store); catalog = new(store);
        var student = new Student { Name = "Aluno fictício V004" }; await education.SaveAsync(student);
        year = new SchoolYear { StudentId = student.Id, Name = "2026", Start = new(2026, 11, 16), End = new(2026, 12, 31) }; await education.SaveAsync(year);
    }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root, true); return Task.CompletedTask; }
    private SubjectImport Pick(string id = "portugues", int count = 3) => new(id, count, 1, [DayOfWeek.Monday, DayOfWeek.Wednesday]);
    [Fact] public async Task InitialCatalogHasNineNamesAndReadingDoesNotWriteOrImport()
    {
        var revision = await store.RevisionAsync(); var hash = SHA256.HashData(File.ReadAllBytes(store.DatabasePath));
        Assert.Equal(new[] { "Português", "Matemática", "História", "Geografia", "Ciências", "Artes", "Inglês", "Educação Física", "Ensino Religioso" }, (await catalog.GetAsync()).Items.Select(item => item.Name));
        Assert.Empty(await store.AllAsync<Subject>()); Assert.Empty(await store.AllAsync<SubjectCatalog>());
        Assert.Equal(revision, await store.RevisionAsync()); Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(store.DatabasePath)));
    }
    [Fact] public async Task CatalogRenameOrderRemovalCustomAndEmptyPersistAcrossReopen()
    {
        var draft = await catalog.GetAsync(); draft.Items.Reverse(); draft.Items.RemoveAt(1); draft.Items[0] = draft.Items[0] with { Name = "Religiões" }; draft.Items.Add(new("custom", "Robótica")); await catalog.SaveAsync(draft);
        var reopened = new SubjectCatalogService(new(root)); Assert.Equal(draft.Items, (await reopened.GetAsync()).Items);
        draft.Items.Clear(); await catalog.SaveAsync(draft); Assert.Empty((await reopened.GetAsync()).Items);
    }
    [Theory] [InlineData(" português  ")] [InlineData("PORTUGUES")] [InlineData("Portugue\u0302s")]
    public async Task EquivalentCatalogNamesRejected(string name)
    {
        var draft = await catalog.GetAsync(); draft.Items.Add(new("duplicate", name));
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.SaveAsync(draft)); Assert.Empty(await store.AllAsync<SubjectCatalog>());
    }
    [Fact] public async Task SelectedSubjectsHaveIndependentCountsAndFrequencyWithoutPlanning()
    {
        var result = await catalog.ImportAsync(year.Id, [Pick(), new("matematica", 7, 2, [DayOfWeek.Friday])]);
        Assert.Equal(new SubjectImportResult(2, 0), result);
        var subjects = await store.AllAsync<Subject>(); var lessons = await store.AllAsync<Lesson>();
        Assert.Equal(3, subjects[0].LessonCount); Assert.Equal(7, subjects[1].LessonCount); Assert.Equal(2, subjects[1].LessonsPerDay);
        Assert.Equal(new[] { DayOfWeek.Friday }, subjects[1].StudyDays); Assert.Equal(10, lessons.Count); Assert.Empty(await store.AllAsync<PlannedLesson>());
    }
    [Fact] public async Task RepeatedConcurrentAndRenamedImportsNeverDuplicateOrOverwrite()
    {
        await Task.WhenAll(catalog.ImportAsync(year.Id, [Pick()]), catalog.ImportAsync(year.Id, [Pick()]));
        var subject = Assert.Single(await store.AllAsync<Subject>());
        var draft = await catalog.GetAsync(); draft.Items[0] = draft.Items[0] with { Name = "Língua Portuguesa" }; await catalog.SaveAsync(draft);
        var result = await catalog.ImportAsync(year.Id, [Pick(count: 15)]);
        Assert.Equal(new SubjectImportResult(0, 1), result); Assert.Equal(JsonSerializer.Serialize(subject), JsonSerializer.Serialize(Assert.Single(await store.AllAsync<Subject>()))); Assert.Equal(3, (await store.AllAsync<Lesson>()).Count);
    }
    [Fact] public async Task LegacyNamedSubjectSkippedWithoutModificationAndOtherYearsRemainIndependent()
    {
        var legacy = new Subject { YearId = year.Id, Name = "  PORTUGUES ", LessonCount = 2 }; await education.SaveAsync(legacy);
        Assert.Equal(new SubjectImportResult(0, 1), await catalog.ImportAsync(year.Id, [Pick()])); Assert.Equal(JsonSerializer.Serialize(legacy), JsonSerializer.Serialize(Assert.Single(await store.AllAsync<Subject>())));
        var second = year with { Id = Guid.NewGuid().ToString("N"), Name = "Outro ano" }; await education.SaveAsync(second);
        Assert.Equal(new SubjectImportResult(1, 0), await catalog.ImportAsync(second.Id, [Pick()])); Assert.Equal(2, (await store.AllAsync<Subject>()).Count);
    }
    [Fact] public async Task InvalidBatchRollsBackSubjectsLessonsAndRevision()
    {
        var revision = await store.RevisionAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.ImportAsync(year.Id, [Pick(), Pick("matematica", 0)]));
        Assert.Empty(await store.AllAsync<Subject>()); Assert.Empty(await store.AllAsync<Lesson>()); Assert.Equal(revision, await store.RevisionAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.ImportAsync("missing", [Pick()]));
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.ImportAsync(year.Id, []));
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.ImportAsync(year.Id, [Pick("missing")]));
    }
    [Fact] public async Task CustomCatalogAndDirectSubjectsWorkAndCatalogEditsNeverRewriteStudents()
    {
        var draft = await catalog.GetAsync(); draft.Items.Add(new("custom", "Astronomia")); await catalog.SaveAsync(draft);
        await catalog.ImportAsync(year.Id, [Pick("custom", 2)]); var original = Assert.Single(await store.AllAsync<Subject>());
        await education.SaveAsync(new Subject { YearId = year.Id, Name = "Piano", LessonCount = 4 });
        draft.Items.Clear(); await catalog.SaveAsync(draft); Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize((await store.AllAsync<Subject>()).Single(s => s.Id == original.Id))); Assert.Equal(6, (await store.AllAsync<Lesson>()).Count);
    }
    [Fact] public void EveryNonemptyWeekCombinationRoundTripsAndInvalidSelectionsAreRejected()
    {
        for (var mask = 1; mask < 128; mask++)
        {
            var days = Enum.GetValues<DayOfWeek>().Where(day => (mask & (1 << (int)day)) != 0).ToArray();
            var normalized = StudyWeek.Normalize(days); Assert.Equal(days.Order(), normalized.Order()); Assert.Equal(normalized, StudyWeek.Normalize(normalized.Concat(normalized)));
            var loaded = JsonSerializer.Deserialize<SchoolYear>(JsonSerializer.Serialize(new SchoolYear { StudyDays = days }))!;
            Assert.Equal(days, loaded.StudyDays);
        }
        Assert.Throws<ArgumentException>(() => StudyWeek.Normalize([])); Assert.Throws<ArgumentException>(() => StudyWeek.Normalize([(DayOfWeek)7]));
        Assert.Equal(5, new SchoolYear().StudyDays.Length); Assert.DoesNotContain(DayOfWeek.Saturday, new SchoolYear().StudyDays); Assert.DoesNotContain(DayOfWeek.Sunday, new SchoolYear().StudyDays);
    }
    [Fact] public async Task CalendarSelectionFeedsGeneratorAndLaterChangesPreserveExistingPlans()
    {
        year.StudyDays = [DayOfWeek.Saturday, DayOfWeek.Sunday]; await education.SaveAsync(year);
        await catalog.ImportAsync(year.Id, [new("artes", 2, 1, [DayOfWeek.Saturday])]); var subject = Assert.Single(await store.AllAsync<Subject>());
        await education.GeneratePlanAsync(subject.Id); var before = await store.AllAsync<PlannedLesson>(); Assert.All(before, plan => Assert.Equal(DayOfWeek.Saturday, plan.OriginalDate.DayOfWeek));
        year.StudyDays = [DayOfWeek.Tuesday]; await education.SaveAsync(year); Assert.Equal(before, await store.AllAsync<PlannedLesson>());
        Assert.Equal(new[] { DayOfWeek.Tuesday }, Assert.Single(await new JournalStore(root).AllAsync<SchoolYear>()).StudyDays);
        await Assert.ThrowsAsync<ArgumentException>(() => education.SaveAsync(year with { StudyDays = [] }));
        await Assert.ThrowsAsync<ArgumentException>(() => education.SaveAsync(subject with { StudyDays = [(DayOfWeek)8] }));
    }
    [Fact] public async Task LegacyNumericCalendarAndSessionReadWithoutChangesAndBackupIncludesCatalog()
    {
        using (var db = new SqliteConnection($"Data Source={store.DatabasePath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand(); command.CommandText = "UPDATE Records SET Data=json_set(Data,'$.StudyDays',json('[0,2,6]')) WHERE Id=$id"; command.Parameters.AddWithValue("$id", year.Id); command.ExecuteNonQuery();
        }
        var legacyHash = SHA256.HashData(File.ReadAllBytes(store.DatabasePath));
        Assert.Equal(new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Saturday }, Assert.Single(await store.AllAsync<SchoolYear>()).StudyDays);
        await catalog.GetAsync(); Assert.Equal(legacyHash, SHA256.HashData(File.ReadAllBytes(store.DatabasePath)));
        var draft = await catalog.GetAsync(); draft.Items.Reverse(); await catalog.SaveAsync(draft); await catalog.ImportAsync(year.Id, [Pick()]);
        var subject = Assert.Single(await store.AllAsync<Subject>()); var lesson = (await store.AllAsync<Lesson>())[0];
        var session = new StudySession { SubjectId = subject.Id, LessonIds = [lesson.Id], Minutes = 50 }; await education.SaveAsync(session);
        var backup = await new BackupService(store).CreateAsync("Senha fictícia V004!", "Teste");
        var restoredRoot = Path.Combine(root, "restore"); var restoredStore = new JournalStore(restoredRoot); var safety = await new BackupService(restoredStore).RestoreAsync(backup.Bytes, "Senha fictícia V004!", true, true);
        try { Assert.Equal(draft.Items, (await new SubjectCatalogService(restoredStore).GetAsync()).Items); Assert.Equal(JsonSerializer.Serialize(subject), JsonSerializer.Serialize(Assert.Single(await restoredStore.AllAsync<Subject>()))); Assert.Equal(JsonSerializer.Serialize(session), JsonSerializer.Serialize(Assert.Single(await restoredStore.AllAsync<StudySession>()))); }
        finally { if (File.Exists(safety)) File.Delete(safety); }
    }
}
