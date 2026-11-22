namespace MentorRouletteTracker.DutyTracking;

public sealed record DeathTimestamp(TimeSpan Elapsed, string Name, string World)
{
    public int? PullNumber { get; init; }
    public bool BeforePull { get; init; }
}
