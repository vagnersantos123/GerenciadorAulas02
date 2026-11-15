using Microsoft.Data.Sqlite;

namespace GerenciadorAulas.Core;

public sealed class EducationService(JournalStore store)
{
    private static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
    private static T Parent<T>(SqliteConnection db, SqliteTransaction tx, string id) where T : Entry => JournalStore.Find(db, tx, id) as T ?? throw new ArgumentException("Vínculo inexistente ou incompatível.");
    private static void Lessons(SqliteConnection db, SqliteTransaction tx, string subjectId, string[] ids)
    {
        Require(ids.Distinct().Count() == ids.Length, "Lições repetidas na seleção.");
        foreach (var id in ids) Require(Parent<Lesson>(db, tx, id).SubjectId == subjectId, "A lição pertence a outra matéria.");
    }
    public Task SaveAsync(Entry entry) => store.MutateAsync((db, tx) =>
    {
        switch (entry)
        {
            case Student s: Require(!string.IsNullOrWhiteSpace(s.Name), "Informe o nome do aluno."); break;
            case SchoolYear y:
                Parent<Student>(db, tx, y.StudentId); Require(!string.IsNullOrWhiteSpace(y.Name) && y.End >= y.Start, "Ano letivo ou datas inválidos.");
                Require(y.StudyDays.Length > 0 && y.StudyDays.All(d => Enum.IsDefined(d)), "Selecione dias de estudo válidos.");
                Require(y.Breaks.All(b => b.End >= b.Start), "Pausa com datas inválidas."); break;
            case Subject s:
                Parent<SchoolYear>(db, tx, s.YearId);
                Require(!string.IsNullOrWhiteSpace(s.Name) && s.LessonCount is > 0 and <= 5000 && s.LessonsPerDay is > 0 and <= 50, "Matéria, quantidade ou ritmo inválidos.");
                Require(s.StudyDays.Length > 0 && s.StudyDays.All(d => Enum.IsDefined(d)), "Selecione dias válidos.");
                Require(s.EstimatedLessonMinutes is null or >= 0, "Estimativa inválida.");
                var old = JournalStore.Find(db, tx, s.Id) as Subject;
                Require(old is null || s.LessonCount >= old.LessonCount, "Não reduza a quantidade: há histórico de lições.");
                JournalStore.Put(db, tx, s);
                var numbers = JournalStore.Read<Lesson>(db, tx).Where(l => l.SubjectId == s.Id).Select(l => l.Number).ToHashSet();
                for (var n = 1; n <= s.LessonCount; n++) if (!numbers.Contains(n)) JournalStore.Put(db, tx, new Lesson { SubjectId = s.Id, Number = n });
                return;
            case Lesson l:
                var subject = Parent<Subject>(db, tx, l.SubjectId);
                Require(l.Number > 0 && l.Number <= subject.LessonCount, "Número de lição inválido."); break;
            case PlannedLesson p:
                Parent<Lesson>(db, tx, p.LessonId);
                if (JournalStore.Find(db, tx, p.Id) is PlannedLesson original) Require(original.OriginalDate == p.OriginalDate, "A data original do planejamento é imutável.");
                Require(p.EstimatedMinutes is null or >= 0 && p.RevisedEstimatedMinutes is null or >= 0, "Estimativa inválida.");
                if (JournalStore.Find(db, tx, p.Id) is PlannedLesson priorPlan && priorPlan.EstimatedMinutes is not null)
                    Require(priorPlan.EstimatedMinutes == p.EstimatedMinutes, "O tempo originalmente planejado é imutável. Utilize a estimativa ajustada.");
                break;
            case StudySession s:
                Parent<Subject>(db, tx, s.SubjectId); Lessons(db, tx, s.SubjectId, s.LessonIds);
                Require(s.Minutes >= 0 && s.Exercises >= 0 && s.Correct >= 0 && s.Correct <= s.Exercises, "Duração ou acertos inválidos.");
                Require(s.LessonIds.Length > 0 || s.DifficultyId is not null, "Selecione lições ou uma dificuldade para reforço.");
                if (s.DifficultyId is not null) Require(Parent<Difficulty>(db, tx, s.DifficultyId).SubjectId == s.SubjectId, "Dificuldade de outra matéria.");
                SessionTiming.Validate(s);
                if (JournalStore.Find(db, tx, s.Id) is StudySession priorSession)
                    Require(priorSession.PlannedMinutes == s.PlannedMinutes, "O tempo original do planejamento registrado na sessão é imutável.");
                else
                {
                    var suggestion = SessionTimeComparison.Suggest(JournalStore.Read<PlannedLesson>(db, tx), s.LessonIds);
                    s.PlannedMinutes = suggestion.OriginalMinutes;
                }
                break;
            case Difficulty d:
                Parent<Subject>(db, tx, d.SubjectId); Lessons(db, tx, d.SubjectId, d.LessonIds);
                Require(!string.IsNullOrWhiteSpace(d.Theme) && Enum.IsDefined(d.Status), "Informe o tema e uma situação válida."); break;
            case Activity a: Parent<Subject>(db, tx, a.SubjectId); Require(!string.IsNullOrWhiteSpace(a.Title), "Informe o título."); break;
            case Occurrence o:
                Parent<SchoolYear>(db, tx, o.YearId); Require(o.End >= o.Start && !string.IsNullOrWhiteSpace(o.Title), "Ocorrência inválida.");
                foreach (var id in o.SubjectIds) Require(Parent<Subject>(db, tx, id).YearId == o.YearId, "Matéria de outro ano letivo."); break;
            case Evidence: throw new ArgumentException("Utilize o importador de evidências.");
            default: throw new ArgumentException("Tipo de registro inválido.");
        }
        JournalStore.Put(db, tx, entry);
        if (entry is StudySession session) JournalStore.Link(db, tx, entry.Id, session.LessonIds.Concat(session.DifficultyId is null ? [] : new[] { session.DifficultyId }));
        if (entry is Difficulty difficulty) JournalStore.Link(db, tx, entry.Id, difficulty.LessonIds);
        if (entry is Occurrence occurrence) JournalStore.Link(db, tx, entry.Id, occurrence.SubjectIds);
    });
    public Task GeneratePlanAsync(string subjectId) => store.MutateAsync((db, tx) =>
    {
        var subject = Parent<Subject>(db, tx, subjectId); var year = Parent<SchoolYear>(db, tx, subject.YearId);
        var already = JournalStore.Read<PlannedLesson>(db, tx).Select(p => p.LessonId).ToHashSet();
        var lessons = JournalStore.Read<Lesson>(db, tx).Where(l => l.SubjectId == subjectId && !already.Contains(l.Id)).OrderBy(l => l.Number).ToArray();
        if (already.Overlaps(JournalStore.Read<Lesson>(db, tx).Where(l => l.SubjectId == subjectId).Select(l => l.Id))) throw new InvalidOperationException("Já existe planejamento para esta matéria. Ajuste as datas explicitamente; o original será preservado.");
        var dates = Calendar.StudyDates(year, subject).SelectMany(d => Enumerable.Repeat(d, subject.LessonsPerDay)).Take(lessons.Length).ToArray();
        Require(dates.Length >= lessons.Length, $"Calendário comporta {dates.Length} lições; são necessárias {lessons.Length}. Ajuste o ritmo ou calendário.");
        for (var i = 0; i < lessons.Length; i++) JournalStore.Put(db, tx, new PlannedLesson { LessonId = lessons[i].Id, OriginalDate = dates[i], EstimatedMinutes = subject.EstimatedLessonMinutes });
    });
}
public static class Calendar
{
    public static IEnumerable<DateOnly> StudyDates(SchoolYear year, Subject subject)
    {
        if (year.Start > year.End) yield break;
        for (var date = year.Start; ; date = date.AddDays(1))
        {
            if (year.StudyDays.Contains(date.DayOfWeek) && subject.StudyDays.Contains(date.DayOfWeek) && !year.Breaks.Any(b => date >= b.Start && date <= b.End)) yield return date;
            if (date == year.End) yield break;
        }
    }
}
