namespace MentorRouletteTracker.DutyTracking;

public enum DutyPullOutcome { InProgress, CombatEnded, Wipe, Reset, DutyCompleted, DutyLeft, Interrupted }

public sealed record DutyPull(int Number, TimeSpan? Started, TimeSpan? Ended = null,
    DutyPullOutcome Outcome = DutyPullOutcome.InProgress);
