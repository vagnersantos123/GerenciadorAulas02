using GerenciadorAulas.Core;
using Xunit;

namespace GerenciadorAulas.Tests;

public sealed class PlanningSelectionTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-002-" + Guid.NewGuid());
    private JournalStore store = null!;
    private EducationService service = null!;
    private SchoolYear year = null!;

    public async Task InitializeAsync()
    {
        store = new(root); service = new(store);
        var student = new Student { Name = "Aluno descartável 002" };
        await service.SaveAsync(student);
        year = new SchoolYear { StudentId = student.Id, Name = "Teste", Start = new(2026, 6, 29), End = new(2026, 8, 14),
            StudyDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            Breaks = [new(new(2026, 7, 1), new(2026, 7, 31), "Julho"), new(new(2026, 8, 3), new(2026, 8, 3), "Feriado")] };
        await service.SaveAsync(year);
    }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root, true); return Task.CompletedTask; }

    private async Task<Subject> SubjectAsync(string name, int count = 4, int perDay = 1)
    {
        var subject = new Subject { YearId = year.Id, Name = name, LessonCount = count, LessonsPerDay = perDay,
            StudyDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday] };
        await service.SaveAsync(subject); return subject;
    }

    [Fact]
    public async Task PlanUsesBoundsIntersectionJulyHolidayAndSubjectRate()
    {
        var subject = await SubjectAsync("Matemática", 5, 2);
        await service.GeneratePlanAsync(subject.Id);
        var plan = await store.AllAsync<PlannedLesson>();
        Assert.Equal(new DateOnly[] { new(2026, 6, 29), new(2026, 6, 29), new(2026, 8, 5), new(2026, 8, 5), new(2026, 8, 7) }, plan.Select(p => p.OriginalDate));
        Assert.All(plan, p => Assert.InRange(p.OriginalDate, year.Start, year.End));
        var other = await SubjectAsync("História", 2);
        other.StudyDays = [DayOfWeek.Tuesday]; await service.SaveAsync(other);
        await service.GeneratePlanAsync(other.Id);
        var ids = (await store.AllAsync<Lesson>()).Where(l => l.SubjectId == other.Id).Select(l => l.Id).ToHashSet();
        Assert.Equal(new DateOnly[] { new(2026, 6, 30), new(2026, 8, 4) }, (await store.AllAsync<PlannedLesson>()).Where(p => ids.Contains(p.LessonId)).Select(p => p.OriginalDate));
    }

    [Fact]
    public async Task RepeatedAndConcurrentGenerationNeverDuplicateOrReplaceOriginalDates()
    {
        var subject = await SubjectAsync("Matemática");
        async Task<bool> Generate() { try { await service.GeneratePlanAsync(subject.Id); return true; } catch (InvalidOperationException) { return false; } }
        var results = await Task.WhenAll(Generate(), Generate());
        Assert.Single(results, r => r);
        var before = await store.AllAsync<PlannedLesson>();
        var revision = await store.RevisionAsync();
        year.Start = new(2026, 8, 10); await service.SaveAsync(year);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GeneratePlanAsync(subject.Id));
        Assert.Equal(before.Select(p => (p.Id, p.OriginalDate)), (await store.AllAsync<PlannedLesson>()).Select(p => (p.Id, p.OriginalDate)));
        Assert.Equal(revision + 1, await store.RevisionAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsufficientOrEmptyCalendarLeavesNoPartialPlan(bool empty)
    {
        year.End = year.Start; if (empty) year.Breaks.Add(new(year.Start, year.End, "Sem aula"));
        await service.SaveAsync(year);
        var subject = await SubjectAsync("Sem capacidade", 3);
        var revision = await store.RevisionAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.GeneratePlanAsync(subject.Id));
        Assert.Empty(await store.AllAsync<PlannedLesson>());
        Assert.Equal(revision, await store.RevisionAsync());
    }

    [Fact]
    public async Task RevisedDateAndRealStudiesNeverChangeOriginalPlan()
    {
        var subject = await SubjectAsync("Matemática"); await service.GeneratePlanAsync(subject.Id);
        var plan = (await store.AllAsync<PlannedLesson>())[0]; var original = plan.OriginalDate;
        plan.RevisedDate = original.AddDays(8); await service.SaveAsync(plan);
        await service.SaveAsync(new StudySession { SubjectId = subject.Id, LessonIds = [plan.LessonId], Date = original.AddDays(-1), Minutes = 50 });
        var reopened = new JournalStore(root);
        var saved = (await reopened.AllAsync<PlannedLesson>())[0];
        Assert.Equal(original, saved.OriginalDate); Assert.Equal(plan.RevisedDate, saved.RevisedDate);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(saved with { OriginalDate = original.AddDays(1) }));
    }

    [Fact]
    public void CalendarIncludesLastRepresentableDayWithoutOverflow()
    {
        var maxYear = year with { Start = DateOnly.MaxValue, End = DateOnly.MaxValue, StudyDays = [DateOnly.MaxValue.DayOfWeek], Breaks = [] };
        var subject = new Subject { StudyDays = [DateOnly.MaxValue.DayOfWeek] };
        Assert.Equal(new[] { DateOnly.MaxValue }, Calendar.StudyDates(maxYear, subject));
    }

    [Fact]
    public void PlanningDistinguishesAheadOverdueCompletedAndRevisedDates()
    {
        var lesson = new Lesson { SubjectId = "math", Number = 1 };
        var plan = new PlannedLesson { LessonId = lesson.Id, OriginalDate = new(2026, 8, 10) };
        var study = new StudySession { SubjectId = "math", LessonIds = [lesson.Id], Date = new(2026, 8, 7) };
        Assert.Equal("Pendente", LessonPlanning.Describe(lesson, plan, [], new(2026, 8, 10)));
        Assert.Equal("Atrasada · Pendente", LessonPlanning.Describe(lesson, plan, [], new(2026, 8, 11)));
        Assert.Equal("Em andamento · estudo adiantado", LessonPlanning.Describe(lesson, plan, [study], new(2026, 8, 8)));
        Assert.Equal("Atrasada · Em andamento", LessonPlanning.Describe(lesson, plan, [study], new(2026, 8, 11)));
        lesson.Completed = true;
        Assert.Equal("Concluída · estudo adiantado", LessonPlanning.Describe(lesson, plan, [study], new(2026, 8, 11)));
        lesson.Completed = false; plan.RevisedDate = new(2026, 8, 6);
        Assert.Equal("Em andamento · estudo iniciado após o previsto", LessonPlanning.Describe(lesson, plan, [study], new(2026, 8, 6)));
        study.Date = new(2026, 8, 6);
        Assert.Equal("Em andamento · estudo na data prevista", LessonPlanning.Describe(lesson, plan, [study], new(2026, 8, 6)));
        Assert.Equal(new DateOnly(2026, 8, 10), plan.OriginalDate);
    }

    [Fact]
    public async Task SelectionSearchStatusAndFiltersPreserveChosenLessons()
    {
        var subject = await SubjectAsync("Matemática");
        var lessons = await store.AllAsync<Lesson>(); lessons[1].Title = "Frações"; lessons[2].Completed = true;
        await service.SaveAsync(lessons[1]); await service.SaveAsync(lessons[2]);
        await service.SaveAsync(new StudySession { SubjectId = subject.Id, LessonIds = [lessons[1].Id], Minutes = 50 });
        var selection = new LessonSelection(lessons, await store.AllAsync<StudySession>(), subject.Id, [lessons[0].Id]);
        Assert.Equal(new[] { LessonProgress.Pending, LessonProgress.InProgress, LessonProgress.Completed, LessonProgress.Pending }, selection.Options.Select(o => o.Progress));
        Assert.Equal(lessons[1].Id, Assert.Single(selection.Find(" fracoes ")).Id);
        Assert.Equal(lessons[2].Id, Assert.Single(selection.Find("3")).Id);
        selection.Select(lessons[1].Id, true); selection.Select(lessons[1].Id, true); selection.Find("ausente");
        Assert.Equal(2, selection.Selected.Count); Assert.Equal(2, selection.Find(null, true).Count);
        selection.Select(lessons[0].Id, false); Assert.Equal(lessons[1].Id, Assert.Single(selection.SelectedIds));
        Assert.Equal("Lição 2 · Frações", selection.Selected[0].Caption);
        Assert.Empty(selection.Find("5000"));
    }

    [Fact]
    public async Task SubjectSwitchClearsSelectionAndRejectsForeignLessons()
    {
        var subject = await SubjectAsync("Matemática", 1); var other = await SubjectAsync("História", 1);
        var lessons = await store.AllAsync<Lesson>(); var first = lessons.Single(l => l.SubjectId == subject.Id);
        var selection = new LessonSelection(lessons, [], subject.Id, [first.Id]);
        selection.ChangeSubject(subject.Id); Assert.Single(selection.Selected);
        selection.ChangeSubject(other.Id); Assert.Empty(selection.Selected);
        Assert.Single(selection.Options); Assert.Throws<ArgumentException>(() => selection.Select(first.Id, true));
    }

    [Fact]
    public async Task VisualSelectionSupportsMultipleDaysEditingAndExplicitCompletionOnly()
    {
        var subject = await SubjectAsync("Matemática", 3);
        var lessons = await store.AllAsync<Lesson>();
        var selection = new LessonSelection(lessons, [], subject.Id, []);
        selection.Select(lessons[0].Id, true); selection.Select(lessons[1].Id, true);
        var session = new StudySession { SubjectId = subject.Id, LessonIds = selection.SelectedIds, Date = new(2026, 8, 5), Minutes = 50, SessionFinished = true };
        await service.SaveAsync(session);
        await service.SaveAsync(new StudySession { SubjectId = subject.Id, LessonIds = selection.SelectedIds, Date = new(2026, 8, 7), Minutes = 20 });
        session.Minutes = 55; await service.SaveAsync(session);
        var reopened = new JournalStore(root);
        Assert.Equal(2, (await reopened.AllAsync<StudySession>()).Count);
        Assert.Equal(55, (await reopened.AllAsync<StudySession>()).Single(s => s.Id == session.Id).Minutes);
        Assert.All(await reopened.AllAsync<Lesson>(), l => Assert.False(l.Completed));
        lessons[0].Completed = true; await service.SaveAsync(lessons[0]);
        var again = new LessonSelection(await reopened.AllAsync<Lesson>(), await reopened.AllAsync<StudySession>(), subject.Id, session.LessonIds);
        Assert.Equal(LessonProgress.Completed, again.Selected[0].Progress); Assert.Equal(2, again.Selected.Count);
    }

    [Fact]
    public void LargeCatalogCanFindAndKeepSelectionOutsideCurrentSearch()
    {
        var lessons = Enumerable.Range(1, 5000).Select(n => new Lesson { SubjectId = "math", Number = n }).ToArray();
        var selection = new LessonSelection(lessons, [], "math", []);
        selection.Select(lessons[4999].Id, true);
        Assert.Equal(5000, Assert.Single(selection.Find("5000")).Number);
        selection.Find("1"); Assert.Equal(lessons[4999].Id, Assert.Single(selection.SelectedIds));
    }
}
