using System.Globalization;

namespace GerenciadorAulas.Core;

public enum LessonProgress { Pending, InProgress, Completed }

public sealed record LessonOption(string Id, int Number, string Title, LessonProgress Progress)
{
    public string Caption => string.IsNullOrWhiteSpace(Title) ? $"Lição {Number}" : $"Lição {Number} · {Title}";
    public string Status => Progress switch
    {
        LessonProgress.Completed => "Concluída",
        LessonProgress.InProgress => "Em andamento",
        _ => "Pendente"
    };
}

// Selection stays independent of the record until the form is saved.
public sealed class LessonSelection
{
    private readonly List<Lesson> lessons;
    private readonly HashSet<string> studied;
    private readonly HashSet<string> selected = [];
    public string SubjectId { get; private set; }
    public IReadOnlyList<LessonOption> Options { get; private set; } = [];
    public string[] SelectedIds => Options.Where(o => selected.Contains(o.Id)).Select(o => o.Id).ToArray();
    public IReadOnlyList<LessonOption> Selected => Options.Where(o => selected.Contains(o.Id)).ToArray();

    public LessonSelection(IEnumerable<Lesson> lessons, IEnumerable<StudySession> sessions, string subjectId, IEnumerable<string> selectedIds)
    {
        this.lessons = lessons.ToList();
        studied = sessions.SelectMany(s => s.LessonIds).ToHashSet();
        SubjectId = subjectId;
        LoadOptions();
        var valid = Options.Select(o => o.Id).ToHashSet();
        selected.UnionWith(selectedIds.Where(valid.Contains));
    }

    private void LoadOptions() => Options = lessons.Where(l => l.SubjectId == SubjectId).OrderBy(l => l.Number)
        .Select(l => new LessonOption(l.Id, l.Number, l.Title,
            l.Completed ? LessonProgress.Completed : studied.Contains(l.Id) ? LessonProgress.InProgress : LessonProgress.Pending)).ToArray();

    public void ChangeSubject(string subjectId)
    {
        if (subjectId == SubjectId) return;
        SubjectId = subjectId;
        selected.Clear();
        LoadOptions();
    }

    public void Select(string id, bool value)
    {
        if (!Options.Any(o => o.Id == id)) throw new ArgumentException("A lição pertence a outra matéria.");
        if (value) selected.Add(id); else selected.Remove(id);
    }

    public IReadOnlyList<LessonOption> Find(string? query, bool selectedOnly = false)
    {
        var text = query?.Trim() ?? "";
        return Options.Where(o => (!selectedOnly || selected.Contains(o.Id)) &&
            (text.Length == 0 || (int.TryParse(text, out var number) && o.Number == number) ||
             CultureInfo.CurrentCulture.CompareInfo.IndexOf(o.Title, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)).ToArray();
    }
}

public static class LessonPlanning
{
    // The model has no completion date: timing describes the first recorded study,
    // never an inferred completion date. An unfinished past-due lesson stays overdue.
    public static string Describe(Lesson lesson, PlannedLesson plan, IEnumerable<StudySession> sessions, DateOnly today)
    {
        var dates = sessions.Where(s => s.SubjectId == lesson.SubjectId && s.LessonIds.Contains(lesson.Id)).Select(s => s.Date).ToArray();
        var progress = lesson.Completed ? "Concluída" : dates.Length > 0 ? "Em andamento" : "Pendente";
        var expected = plan.RevisedDate ?? plan.OriginalDate;
        if (!lesson.Completed && expected < today) return $"Atrasada · {progress}";
        if (dates.Length == 0) return progress;
        var first = dates.Min();
        return first < expected ? $"{progress} · estudo adiantado" :
            first > expected ? $"{progress} · estudo iniciado após o previsto" : $"{progress} · estudo na data prevista";
    }
}
