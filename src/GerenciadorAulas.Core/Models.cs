namespace GerenciadorAulas.Core;

public abstract record Entry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public abstract string? ParentId { get; }
}
public sealed record Student : Entry
{
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public override string? ParentId => null;
}
public sealed record SchoolYear : Entry
{
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly Start { get; set; } = new(DateTime.Today.Year, 2, 1);
    public DateOnly End { get; set; } = new(DateTime.Today.Year, 12, 15);
    public DayOfWeek[] StudyDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public List<DateRange> Breaks { get; set; } = [];
    public override string ParentId => StudentId;
}
public sealed record DateRange(DateOnly Start, DateOnly End, string Reason);
public sealed record Subject : Entry
{
    public string YearId { get; set; } = "";
    public string Name { get; set; } = "";
    public int LessonCount { get; set; } = 180;
    public int LessonsPerDay { get; set; } = 1;
    public int? EstimatedLessonMinutes { get; set; }
    public DayOfWeek[] StudyDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public override string ParentId => YearId;
}
public sealed record Lesson : Entry
{
    public string SubjectId { get; set; } = "";
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Completed { get; set; }
    public override string ParentId => SubjectId;
}
public sealed record PlannedLesson : Entry
{
    public string LessonId { get; set; } = "";
    public DateOnly OriginalDate { get; set; }
    public DateOnly? RevisedDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int? RevisedEstimatedMinutes { get; set; }
    public override string ParentId => LessonId;
}
public sealed record StudySession : Entry
{
    public string SubjectId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly? StartTime { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int Minutes { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int? PlannedMinutes { get; set; }
    public int Exercises { get; set; }
    public int Correct { get; set; }
    public string Notes { get; set; } = "";
    public string[] LessonIds { get; set; } = [];
    public string? DifficultyId { get; set; }
    public bool SessionFinished { get; set; }
    public override string ParentId => SubjectId;
}
public enum DifficultyStatus { Aberta, EmAcompanhamento, Superada }
public sealed record FollowUp(DateOnly Date, string Notes);
public sealed record Difficulty : Entry
{
    public string SubjectId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Theme { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] LessonIds { get; set; } = [];
    public DifficultyStatus Status { get; set; }
    public List<FollowUp> FollowUps { get; set; } = [];
    public override string ParentId => SubjectId;
}
public sealed record Activity : Entry
{
    public string SubjectId { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Title { get; set; } = "";
    public string Result { get; set; } = "";
    public string Notes { get; set; } = "";
    public override string ParentId => SubjectId;
}
public sealed record Occurrence : Entry
{
    public string YearId { get; set; } = "";
    public DateOnly Start { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly End { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Sensitive { get; set; }
    public string[] SubjectIds { get; set; } = [];
    public override string ParentId => YearId;
}
public sealed record Evidence : Entry
{
    public string RecordId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Hash { get; set; } = "";
    public long Size { get; set; }
    public bool Sensitive { get; set; }
    public override string ParentId => RecordId;
}
