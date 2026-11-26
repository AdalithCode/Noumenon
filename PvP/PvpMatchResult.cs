namespace MentorRouletteTracker.PvP;

public enum PvpMode
{
    Unknown,
    CrystallineConflict,
    Frontline,
    RivalWings
}

public readonly record struct PvpMatchReference(string MatchId, DateTime DutyStartTime, PvpMode Mode);

public sealed class PvpPlayerResult
{
    public string Name { get; init; } = string.Empty;
    public string World { get; init; } = string.Empty;
    public string Job { get; set; } = "Unknown";
    public int? Deaths { get; set; }
    public string Team { get; set; } = string.Empty;
    public long? DamageDealt { get; set; }
    public long? DamageTaken { get; set; }
    public long? HealingDone { get; set; }
    public int? Kills { get; set; }
    public int? Assists { get; set; }
    public int? Placement { get; set; }
}

public class PvpMatchResult
{
    internal static bool IsValidPlacement(PvpMode mode, int? placement) => mode switch
    {
        PvpMode.Frontline => placement is >= 1 and <= 3,
        PvpMode.CrystallineConflict or PvpMode.RivalWings => placement is 1 or 2,
        _ => false
    };

    public string MatchId { get; set; } = string.Empty;
    public DateTime DutyStartTime { get; set; }
    public PvpMode Mode { get; set; }
    public string RecorderName { get; set; } = string.Empty;
    public string RecorderWorld { get; set; } = string.Empty;
    public int ExpectedPlayerCount { get; set; }
    public int? Placement { get; set; }
    public bool HasFinalRoster { get; set; }
    public bool RosterComplete => HasFinalRoster || ExpectedPlayerCount > 0 && Players.Count >= ExpectedPlayerCount;
    public Dictionary<string, PvpPlayerResult> Players { get; set; } = new(StringComparer.Ordinal);
}
