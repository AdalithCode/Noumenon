using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker.Storage;

public sealed class HistoryData
{
    public List<DutyRecord> Records { get; init; } = new();
    public DutyRecord? PendingDuty { get; set; }
    internal Dictionary<DutyRecord, string> RecordIds { get; } = new(ReferenceEqualityComparer.Instance);
}

internal readonly record struct DutyRow(
    DateTime Timestamp,
    DateTime? CheckpointUtc,
    string DutyName,
    string DutyType,
    bool JoinedInProgress,
    int WipeCount,
    int LalafellCount,
    bool Completed,
    string PlayerJob,
    string RecorderName,
    string RecorderWorld,
    TimeSpan TimeInDuty,
    string Note,
    int? PvPDeaths,
    bool? PvPRosterComplete,
    bool TrackPvpEnemies,
    int? PvPPlacement,
    bool PvPCombatStatsComplete,
    PvP.PvpMode PvPMode,
    bool PvPResultsComplete,
    bool PvPExtendedStatsComplete,
    bool PvPLookupFinished,
    bool DeathTimestampsRecorded,
    string DeathTimestampsJson,
    bool PullsRecorded,
    string PullsJson);

internal readonly record struct PlayerRow(
    string Name,
    string Job,
    string Race,
    string Gender,
    string World,
    string Status,
    int DeathCount,
    int? PvPScoreboardDeaths,
    string PvPTeam,
    bool? PvPIsEnemy,
    long? PvPDamageDealt,
    int? PvPKills,
    int? PvPAssists,
    int? PvPPlacement,
    long? PvPDamageTaken,
    long? PvPHealingDone);

internal sealed record DutyWrite(string Id, DutyRow Record, IReadOnlyList<PlayerRow> Party, bool Pending)
{
    public static DutyWrite Capture(string id, DutyRecord record, bool pending)
    {
        var row = new DutyRow(
            record.Timestamp.ToUniversalTime(),
            record.CheckpointUtc?.ToUniversalTime(),
            record.DutyName ?? string.Empty,
            record.DutyType ?? string.Empty,
            record.JoinedInProgress,
            record.WipeCount,
            record.LalafellCount,
            record.Completed,
            record.PlayerJob ?? string.Empty,
            record.RecorderName ?? string.Empty,
            record.RecorderWorld ?? string.Empty,
            record.TimeInDuty,
            record.Note ?? string.Empty,
            record.PvPDeaths,
            record.PvPRosterComplete,
            record.TrackPvpEnemies,
            record.PvPPlacement,
            record.PvPCombatStatsComplete,
            record.PvPMode,
            record.PvPResultsComplete,
            record.PvPExtendedStatsComplete,
            record.PvPLookupFinished,
            record.DeathTimestampsRecorded,
            System.Text.Json.JsonSerializer.Serialize(record.DeathTimestamps),
            record.PullsRecorded,
            System.Text.Json.JsonSerializer.Serialize(record.Pulls));
        var party = record.Party.Select(player => new PlayerRow(
            player.Name ?? string.Empty,
            player.Job ?? string.Empty,
            player.Race ?? string.Empty,
            player.Gender ?? string.Empty,
            player.World ?? string.Empty,
            player.Status ?? string.Empty,
            player.DeathCount,
            player.PvPScoreboardDeaths,
            player.PvPTeam ?? string.Empty,
            player.PvPIsEnemy,
            player.PvPDamageDealt,
            player.PvPKills,
            player.PvPAssists,
            player.PvPPlacement,
            player.PvPDamageTaken,
            player.PvPHealingDone)).ToArray();
        return new DutyWrite(id, row, party, pending);
    }

    public bool SameValues(DutyWrite other)
    {
        // DateTime equality compares ticks but leaves out the local/UTC kind.
        return Record == other.Record &&
            Record.Timestamp.Kind == other.Record.Timestamp.Kind &&
            Record.CheckpointUtc?.Kind == other.Record.CheckpointUtc?.Kind &&
            Pending == other.Pending && Party.SequenceEqual(other.Party);
    }

    public DutyRecord ToRecord()
    {
        var record = new DutyRecord
        {
            Timestamp = Record.Timestamp,
            CheckpointUtc = Record.CheckpointUtc,
            DutyName = Record.DutyName,
            DutyType = Record.DutyType,
            JoinedInProgress = Record.JoinedInProgress,
            WipeCount = Record.WipeCount,
            LalafellCount = Record.LalafellCount,
            Completed = Record.Completed,
            PlayerJob = Record.PlayerJob,
            RecorderName = Record.RecorderName,
            RecorderWorld = Record.RecorderWorld,
            TimeInDuty = Record.TimeInDuty,
            Note = Record.Note,
            PvPDeaths = Record.PvPDeaths,
            PvPRosterComplete = Record.PvPRosterComplete,
            TrackPvpEnemies = Record.TrackPvpEnemies,
            PvPPlacement = Record.PvPPlacement,
            PvPCombatStatsComplete = Record.PvPCombatStatsComplete,
            PvPMode = Record.PvPMode,
            PvPResultsComplete = Record.PvPResultsComplete,
            PvPExtendedStatsComplete = Record.PvPExtendedStatsComplete,
            PvPLookupFinished = Record.PvPLookupFinished,
            DeathTimestampsRecorded = Record.DeathTimestampsRecorded,
            DeathTimestamps = SqliteHistoryStore.ReadDeathTimestamps(Record.DeathTimestampsJson),
            PullsRecorded = Record.PullsRecorded,
            Pulls = SqliteHistoryStore.ReadPulls(Record.PullsJson),
            Party = Party.Where(player => Record.DutyType != "PvP" || !string.IsNullOrWhiteSpace(player.Name))
                .Select(player => new PartyMemberSnapshot
            {
                Name = player.Name,
                Job = player.Job,
                Race = player.Race,
                Gender = player.Gender,
                World = player.World,
                Status = player.Status,
                DeathCount = player.DeathCount,
                PvPScoreboardDeaths = player.PvPScoreboardDeaths,
                PvPTeam = player.PvPTeam,
                PvPIsEnemy = player.PvPIsEnemy,
                PvPDamageDealt = player.PvPDamageDealt,
                PvPKills = player.PvPKills,
                PvPAssists = player.PvPAssists,
                PvPPlacement = player.PvPPlacement,
                PvPDamageTaken = player.PvPDamageTaken,
                PvPHealingDone = player.PvPHealingDone
            }).ToList()
        };
        SqliteHistoryStore.ValidatePullLinks(record);
        return record;
    }
}

internal sealed record HistoryWrite(List<DutyWrite> Duties, bool PendingOnly = false);
