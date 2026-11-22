namespace MentorRouletteTracker.DutyTracking
{
    public class DutyRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime? CheckpointUtc { get; set; }
        public string DutyName { get; set; } = string.Empty;
        public string DutyType { get; set; } = string.Empty;
        public bool JoinedInProgress { get; set; }
        public int WipeCount { get; set; }
        public int LalafellCount { get; set; }
        public bool Completed { get; set; }
        public string PlayerJob { get; set; } = string.Empty;
        public string RecorderName { get; set; } = string.Empty;
        public string RecorderWorld { get; set; } = string.Empty;
        public TimeSpan TimeInDuty { get; set; }
        public List<PartyMemberSnapshot> Party { get; set; } = new();
        public List<DeathTimestamp> DeathTimestamps { get; set; } = new();
        public bool DeathTimestampsRecorded { get; set; }
        public List<DutyPull> Pulls { get; set; } = new();
        public bool PullsRecorded { get; set; }
        public string Note { get; set; } = string.Empty;
        public int? PvPDeaths { get; set; }
        public bool? PvPRosterComplete { get; set; }
        public bool TrackPvpEnemies { get; set; } = true;
        public int? PvPPlacement { get; set; }
        public bool PvPCombatStatsComplete { get; set; }
        public PvP.PvpMode PvPMode { get; set; }
        public bool PvPResultsComplete { get; set; }
        public bool PvPExtendedStatsComplete { get; set; }
        public bool PvPLookupFinished { get; set; }

        internal DutyPull? ActivePull => Pulls.LastOrDefault() is { Ended: null } pull ? pull : null;

        internal void ClosePull(TimeSpan elapsed, DutyPullOutcome outcome)
        {
            var pull = ActivePull;
            if (pull == null) return;
            var end = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
            if (pull.Started is { } start && end < start) end = start;
            foreach (var death in DeathTimestamps.Where(death => death.PullNumber == pull.Number))
                if (end < death.Elapsed) end = death.Elapsed;
            Pulls[^1] = pull with { Ended = end, Outcome = outcome };
        }
    }
}
