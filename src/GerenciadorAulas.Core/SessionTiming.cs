namespace GerenciadorAulas.Core;

// Local wall-clock values, at minute precision for new input. No timezone or UTC
// conversion is inferred for historical records that never stored one.
public sealed class SessionTiming
{
    public DateOnly Date { get; private set; }
    public TimeOnly? Start { get; private set; }
    public int Minutes { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public TimeOnly? End { get; private set; }

    public SessionTiming(StudySession session, bool isNew, DateTime localNow)
    {
        Date = session.Date;
        Start = session.StartTime ?? (isNew ? new TimeOnly(localNow.Hour, localNow.Minute) : null);
        Minutes = session.Minutes;
        var end = CalculateEnd(Date, Start, Minutes);
        EndDate = session.EndDate ?? end?.Date;
        End = session.EndTime ?? end?.Time;
    }

    public static (DateOnly Date, TimeOnly Time)? CalculateEnd(DateOnly date, TimeOnly? start, int minutes)
    {
        if (minutes < 0) throw new ArgumentException("A duração não pode ser negativa.");
        if (start is null) return null;
        try
        {
            var end = date.ToDateTime(start.Value).AddMinutes(minutes);
            return (DateOnly.FromDateTime(end), TimeOnly.FromDateTime(end));
        }
        catch (ArgumentOutOfRangeException) { throw new ArgumentException("O término ultrapassa o intervalo de datas permitido."); }
    }

    public void ChangeStart(DateOnly date, TimeOnly? start)
    {
        var end = CalculateEnd(date, start, Minutes);
        Date = date; Start = start; EndDate = end?.Date; End = end?.Time;
    }

    public void ChangeMinutes(int minutes)
    {
        var end = CalculateEnd(Date, Start, minutes);
        Minutes = minutes; EndDate = end?.Date; End = end?.Time;
    }

    public void ChangeEnd(DateOnly date, TimeOnly time)
    {
        if (Start is null) throw new ArgumentException("Informe o início antes de definir o término.");
        var duration = date.ToDateTime(time) - Date.ToDateTime(Start.Value);
        if (duration < TimeSpan.Zero) throw new ArgumentException("O término não pode ser anterior ao início. Confira também a data de término.");
        if (duration.Ticks % TimeSpan.TicksPerMinute != 0 || duration.TotalMinutes > int.MaxValue)
            throw new ArgumentException("Informe horários com duração em minutos inteiros.");
        Minutes = (int)duration.TotalMinutes; EndDate = date; End = time;
    }

    public void ChangeEndTime(TimeOnly time)
    {
        var date = EndDate ?? Date;
        // A time earlier than the start on the same day means the following day.
        if (Start is not null && date == Date && time < Start.Value)
        {
            if (date == DateOnly.MaxValue) throw new ArgumentException("Não há uma data seguinte disponível.");
            date = date.AddDays(1);
        }
        ChangeEnd(date, time);
    }

    public void Apply(StudySession session)
    {
        session.Date = Date; session.StartTime = Start; session.Minutes = Minutes;
        session.EndDate = EndDate; session.EndTime = End;
    }

    public static void Validate(StudySession session)
    {
        var end = CalculateEnd(session.Date, session.StartTime, session.Minutes);
        if ((session.EndDate is null) != (session.EndTime is null)) throw new ArgumentException("Informe a data e o horário de término juntos.");
        if (session.EndTime is not null && (end is null || end.Value.Date != session.EndDate || end.Value.Time != session.EndTime))
            throw new ArgumentException("Início, término e duração não correspondem.");
        if (session.EstimatedMinutes < 0 || session.PlannedMinutes < 0) throw new ArgumentException("A estimativa não pode ser negativa.");
    }

    public static string Describe(StudySession session)
    {
        if (session.StartTime is null) return "Horário não registrado";
        var end = session.EndDate is not null && session.EndTime is not null
            ? (session.EndDate.Value, session.EndTime.Value) : CalculateEnd(session.Date, session.StartTime, session.Minutes)!.Value;
        return $"{session.Date:dd/MM/yyyy} {session.StartTime:HH:mm} → {end.Item1:dd/MM/yyyy} {end.Item2:HH:mm}";
    }
}

public sealed record PlanTimeSuggestion(int? OriginalMinutes, int? SuggestedMinutes);

public static class SessionTimeComparison
{
    public static PlanTimeSuggestion Suggest(IEnumerable<PlannedLesson> plans, IEnumerable<string> lessonIds)
    {
        var ids = lessonIds.Distinct().ToHashSet();
        var matches = plans.Where(p => ids.Contains(p.LessonId)).ToArray();
        if (ids.Count == 0 || matches.Length != ids.Count) return new(null, null);
        static int? Total(IEnumerable<int?> minutes)
        {
            var values = minutes.ToArray();
            if (values.Any(v => v is null || v < 0)) return null;
            var sum = values.Sum(v => (long)v!.Value);
            return sum <= int.MaxValue ? (int)sum : null;
        }
        return new(Total(matches.Select(p => p.EstimatedMinutes)), Total(matches.Select(p => p.RevisedEstimatedMinutes ?? p.EstimatedMinutes)));
    }

    public static long? Difference(StudySession session) => session.EstimatedMinutes is int expected ? (long)session.Minutes - expected : null;

    public static string Describe(StudySession session)
    {
        var original = session.PlannedMinutes is int plan ? $"Planejamento original: {plan} min · " : "";
        if (session.EstimatedMinutes is not int expected) return $"{original}Sem estimativa · realizado: {session.Minutes} min";
        var difference = Difference(session)!.Value;
        var comparison = difference == 0 ? "mesmo tempo previsto" : difference > 0 ? $"{difference} min a mais" : $"{-difference} min a menos";
        return $"{original}Previsto: {expected} min · realizado: {session.Minutes} min · diferença: {difference:+0;-0;0} min ({comparison})";
    }

    public static IReadOnlyList<SubjectTimeTotal> Totals(IEnumerable<StudySession> sessions, DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException("Intervalo inválido.");
        return sessions.Where(s => s.Date >= start && s.Date <= end).GroupBy(s => s.SubjectId)
            .Select(g => new SubjectTimeTotal(g.Key, g.Count(), g.Sum(s => (long)s.Minutes),
                g.Count(s => s.EstimatedMinutes is not null), g.Where(s => s.EstimatedMinutes is not null).Sum(s => (long)s.EstimatedMinutes!.Value),
                g.Where(s => s.EstimatedMinutes is not null).Sum(s => (long)s.Minutes))).ToArray();
    }
}

// Whole-session totals by subject and start date; no inferred per-lesson allocation.
public sealed record SubjectTimeTotal(string SubjectId, int Sessions, long ActualMinutes, int SessionsWithEstimate, long EstimatedMinutes, long ActualMinutesWithEstimate)
{
    public long DifferenceMinutes => ActualMinutesWithEstimate - EstimatedMinutes;
}
