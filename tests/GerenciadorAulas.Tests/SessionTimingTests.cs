using System.Text.Json.Nodes;
using GerenciadorAulas.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GerenciadorAulas.Tests;

public sealed class SessionTimingTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-003-" + Guid.NewGuid());
    private JournalStore store = null!;
    private EducationService service = null!;
    private Subject subject = null!;
    private List<Lesson> lessons = [];
    public async Task InitializeAsync()
    {
        store = new(root); service = new(store);
        var student = new Student { Name = "Aluno fictício V003" }; await service.SaveAsync(student);
        var year = new SchoolYear { StudentId = student.Id, Name = "2026", Start = new(2026, 11, 16), End = new(2026, 12, 18) }; await service.SaveAsync(year);
        subject = new Subject { YearId = year.Id, Name = "Matemática", LessonCount = 3, EstimatedLessonMinutes = 20 }; await service.SaveAsync(subject);
        lessons = await store.AllAsync<Lesson>();
    }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root, true); return Task.CompletedTask; }
    private StudySession Session() => new() { SubjectId = subject.Id, LessonIds = [lessons[0].Id], Date = new(2026, 11, 16), Minutes = 50 };

    [Fact]
    public void NewStartUsesDeviceLocalClockWithoutMutatingRecord()
    {
        var session = Session(); var now = new DateTime(2026, 11, 16, 14, 23, 48, DateTimeKind.Local);
        var timing = new SessionTiming(session, true, now);
        Assert.Equal(new TimeOnly(14, 23), timing.Start); Assert.Null(session.StartTime);
        Assert.Equal(new TimeOnly(15, 13), timing.End);
    }

    [Theory]
    [InlineData(30)] [InlineData(45)] [InlineData(50)] [InlineData(60)] [InlineData(90)] [InlineData(17)] [InlineData(0)] [InlineData(3000)]
    public void ArbitraryAndQuickDurationsCalculateEnd(int minutes)
    {
        var timing = new SessionTiming(Session() with { StartTime = new(23, 40) }, false, DateTime.Now);
        timing.ChangeMinutes(minutes);
        var expected = new DateTime(2026, 11, 16, 23, 40, 0).AddMinutes(minutes);
        Assert.Equal(DateOnly.FromDateTime(expected), timing.EndDate); Assert.Equal(TimeOnly.FromDateTime(expected), timing.End);
    }

    [Fact]
    public void EndTimeInfersMidnightAndExplicitDatesSupportMultipleDays()
    {
        var timing = new SessionTiming(Session() with { StartTime = new(23, 40), Minutes = 0, Date = new(2026, 12, 31) }, false, DateTime.Now);
        timing.ChangeEndTime(new(0, 20)); Assert.Equal(new DateOnly(2027, 1, 1), timing.EndDate); Assert.Equal(40, timing.Minutes);
        timing.ChangeEnd(new(2027, 1, 2), new(0, 10)); Assert.Equal(1470, timing.Minutes);
        timing.ChangeStart(new(2028, 2, 28), new(23, 40)); Assert.Equal(new DateOnly(2028, 3, 1), timing.EndDate);
    }

    [Fact]
    public void EditingNeverReplacesExistingOrMissingStartWithCurrentTime()
    {
        var existing = Session() with { StartTime = new(8, 15) };
        var draft = new SessionTiming(existing, false, new(2026, 11, 20, 18, 0, 0)); draft.Apply(existing);
        Assert.Equal(new TimeOnly(8, 15), existing.StartTime); Assert.Equal(50, existing.Minutes); Assert.Equal(new TimeOnly(9, 5), existing.EndTime);
        var unknown = Session(); new SessionTiming(unknown, false, DateTime.Now).Apply(unknown);
        Assert.Null(unknown.StartTime); Assert.Null(unknown.EndTime); Assert.Null(unknown.EndDate); Assert.Equal(50, unknown.Minutes);
    }

    [Fact]
    public void InvalidTimeChangesLeaveDraftIntact()
    {
        var timing = new SessionTiming(Session() with { StartTime = new(10, 0) }, false, DateTime.Now);
        Assert.Throws<ArgumentException>(() => timing.ChangeMinutes(-1));
        Assert.Throws<ArgumentException>(() => timing.ChangeEnd(new(2026, 11, 16), new(9, 0)));
        Assert.Throws<ArgumentException>(() => timing.ChangeStart(DateOnly.MaxValue, new(23, 59)));
        Assert.Equal(50, timing.Minutes); Assert.Equal(new TimeOnly(10, 50), timing.End);
        Assert.Throws<ArgumentException>(() => SessionTiming.CalculateEnd(DateOnly.MaxValue, new(23, 59), 1));
    }

    [Fact]
    public async Task InconsistentEndOrNegativeEstimateNeverWritesPartialSession()
    {
        var session = Session() with { StartTime = new(10, 0), EndDate = new(2026, 11, 16), EndTime = new(10, 30) };
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(session));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(Session() with { EstimatedMinutes = -1 }));
        Assert.Empty(await store.AllAsync<StudySession>());
    }

    [Fact]
    public async Task PlanningSuggestionSumsWholeSessionAndPreservesOriginalSnapshot()
    {
        await service.GeneratePlanAsync(subject.Id);
        var plans = await store.AllAsync<PlannedLesson>();
        plans[0].RevisedEstimatedMinutes = 30; await service.SaveAsync(plans[0]);
        var ids = lessons.Take(2).Select(l => l.Id).ToArray();
        var suggestion = SessionTimeComparison.Suggest(await store.AllAsync<PlannedLesson>(), ids);
        Assert.Equal(40, suggestion.OriginalMinutes); Assert.Equal(50, suggestion.SuggestedMinutes);
        var session = Session() with { LessonIds = ids, EstimatedMinutes = suggestion.SuggestedMinutes };
        await service.SaveAsync(session); Assert.Equal(40, session.PlannedMinutes);
        plans[0].RevisedEstimatedMinutes = 90; await service.SaveAsync(plans[0]);
        session.EstimatedMinutes = 60; await service.SaveAsync(session);
        var saved = Assert.Single(await new JournalStore(root).AllAsync<StudySession>());
        Assert.Equal(40, saved.PlannedMinutes); Assert.Equal(60, saved.EstimatedMinutes);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(saved with { PlannedMinutes = 41 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(plans[0] with { EstimatedMinutes = 99 }));
    }

    [Fact]
    public void PartialOrMissingPlanDoesNotInventTotalEstimate()
    {
        var plans = new[] { new PlannedLesson { LessonId = "one", EstimatedMinutes = 20 }, new PlannedLesson { LessonId = "two" } };
        Assert.Null(SessionTimeComparison.Suggest(plans, ["one", "two"]).SuggestedMinutes);
        Assert.Null(SessionTimeComparison.Suggest(plans, ["one", "missing"]).OriginalMinutes);
        Assert.Null(SessionTimeComparison.Suggest(plans, []).SuggestedMinutes);
    }

    [Theory]
    [InlineData(30, -20, "a menos")] [InlineData(50, 0, "mesmo tempo")] [InlineData(90, 40, "a mais")]
    public void ComparisonReportsNeutralSignedDifference(int actual, int difference, string label)
    {
        var session = Session() with { Minutes = actual, EstimatedMinutes = 50 };
        Assert.Equal((long)difference, SessionTimeComparison.Difference(session)); Assert.Contains(label, SessionTimeComparison.Describe(session));
        Assert.Null(SessionTimeComparison.Difference(session with { EstimatedMinutes = null }));
    }

    [Fact]
    public void TotalsArePerSubjectAndStartPeriodWithoutLessonAllocationOrMissingEstimateBias()
    {
        var sessions = new[] { Session() with { Minutes = 50, EstimatedMinutes = 40, LessonIds = lessons.Select(l => l.Id).ToArray() },
            Session() with { Minutes = 20 }, Session() with { SubjectId = "other", Minutes = 30, EstimatedMinutes = 45 }, Session() with { Date = new(2026, 11, 17), Minutes = 999 } };
        var total = SessionTimeComparison.Totals(sessions, new(2026, 11, 16), new(2026, 11, 16)).Single(s => s.SubjectId == subject.Id);
        Assert.Equal(70, total.ActualMinutes); Assert.Equal(1, total.SessionsWithEstimate); Assert.Equal(40, total.EstimatedMinutes); Assert.Equal(10, total.DifferenceMinutes);
        var big = SessionTimeComparison.Totals([Session() with { Minutes = int.MaxValue }, Session() with { Minutes = int.MaxValue }], DateOnly.MinValue, DateOnly.MaxValue);
        Assert.Equal(2L * int.MaxValue, Assert.Single(big).ActualMinutes);
    }

    [Fact]
    public async Task LegacySchemaOneJsonReadsAndEditsWithoutGuessingTimesOrChangingSchema()
    {
        var legacy = Session(); await service.SaveAsync(legacy);
        using (var db = new SqliteConnection($"Data Source={store.DatabasePath};Pooling=False"))
        {
            db.Open(); using var read = db.CreateCommand(); read.CommandText = "SELECT Data FROM Records WHERE Id=$id"; read.Parameters.AddWithValue("$id", legacy.Id);
            var json = JsonNode.Parse((string)read.ExecuteScalar()!)!.AsObject();
            foreach (var field in new[] { "EndDate", "EndTime", "EstimatedMinutes", "PlannedMinutes" }) json.Remove(field);
            using var update = db.CreateCommand(); update.CommandText = "UPDATE Records SET Data=$data WHERE Id=$id"; update.Parameters.AddWithValue("$data", json.ToJsonString()); update.Parameters.AddWithValue("$id", legacy.Id); update.ExecuteNonQuery();
        }
        var before = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(store.DatabasePath));
        var reopened = new JournalStore(root); var old = Assert.Single(await reopened.AllAsync<StudySession>());
        Assert.Equal(before, System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(store.DatabasePath)));
        Assert.Null(old.StartTime); Assert.Null(old.EndTime); Assert.Null(old.EstimatedMinutes); Assert.Equal(50, old.Minutes);
        new SessionTiming(old, false, DateTime.Now).Apply(old); await service.SaveAsync(old);
        using var check = new SqliteConnection($"Data Source={store.DatabasePath};Pooling=False"); check.Open(); using var command = check.CreateCommand(); command.CommandText = "PRAGMA user_version";
        Assert.Equal(1L, command.ExecuteScalar()); Assert.Equal(legacy.Id, Assert.Single(await reopened.AllAsync<StudySession>()).Id);
    }

    [Fact]
    public async Task TimingAndEstimateSurviveBackupAndRestoreWithNoSchemaMigration()
    {
        var session = Session() with { StartTime = new(23, 40), EstimatedMinutes = 45 };
        var timing = new SessionTiming(session, false, DateTime.Now); timing.Apply(session); await service.SaveAsync(session);
        var backup = new BackupService(store); const string password = "Senha ficticia V003!";
        var package = await backup.CreateAsync(password, "Teste V003");
        await backup.RestoreAsync(package.Bytes, password, true, true);
        var restored = Assert.Single(await store.AllAsync<StudySession>());
        Assert.Equal(new DateOnly(2026, 11, 17), restored.EndDate); Assert.Equal(new TimeOnly(0, 30), restored.EndTime); Assert.Equal(45, restored.EstimatedMinutes); Assert.Equal(50, restored.Minutes);
        Assert.Equal(1, package.Manifest.Schema);
    }
}
