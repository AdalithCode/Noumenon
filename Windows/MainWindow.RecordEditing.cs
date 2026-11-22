using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;
using System.Globalization;
using System.Text.Json;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private DutyRecord? editDraft;
    private HashSet<string> editColumns = new();
    private long editHistoryVersion;
    private string editStarted = string.Empty;
    private string editOriginalStarted = string.Empty;
    private string editDuration = string.Empty;
    private readonly Dictionary<string, string> editScoreTexts = new();
    private readonly Dictionary<PartyMemberSnapshot, string> editDeathTexts = new();

    private sealed record EditablePvpScore(string Column, string? RateColumn, string Label, long Maximum,
        Func<PartyMemberSnapshot, long?> Read, Action<PartyMemberSnapshot, long?> Write);

    private static readonly EditablePvpScore[] EditablePvpScores =
    [
        new("pvpDamage", "pvpDamagePerMinute", "Your PvP damage", long.MaxValue, p => p.PvPDamageDealt, (p, v) => p.PvPDamageDealt = v),
        new("pvpKills", null, "Your PvP kills", int.MaxValue, p => p.PvPKills, (p, v) => p.PvPKills = (int?)v),
        new("pvpAssists", null, "Your PvP assists", int.MaxValue, p => p.PvPAssists, (p, v) => p.PvPAssists = (int?)v),
        new("pvpDamageTaken", "pvpDamageTakenPerMinute", "Your PvP damage taken", long.MaxValue, p => p.PvPDamageTaken, (p, v) => p.PvPDamageTaken = v),
        new("pvpHealingDone", "pvpHealingPerMinute", "Your PvP healing", long.MaxValue, p => p.PvPHealingDone, (p, v) => p.PvPHealingDone = v)
    ];

    private bool EditShows(string column) => editColumns.Contains(column);
    private bool EditShowsScore(EditablePvpScore score) => EditShows(score.Column) ||
        (score.RateColumn != null && EditShows(score.RateColumn));
    private bool EditShowsDuration => EditShows("time") ||
        (recordToEdit?.DutyType == "PvP" && editColumns.Any(id => id.EndsWith("PerMinute", StringComparison.Ordinal)));

    private void PrepareRecordEdit(DutyRecord record)
    {
        editDraft = JsonSerializer.Deserialize<DutyRecord>(JsonSerializer.Serialize(record))!;
        editColumns = TableColumnOptions["Records"].Where((_, i) => IsTableColumnVisible("Records", i))
            .Select(column => column.Id).ToHashSet();
        if (EditShows("type")) editDraft.DutyType = DutyClassification.DutyType(record);
        editHistoryVersion = config.History.ChangeVersion;
        editStarted = editOriginalStarted = record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        editDuration = record.TimeInDuty.ToString("c", CultureInfo.InvariantCulture);
        editScoreTexts.Clear();
        var recorder = RecorderPvpPlayer(editDraft);
        if (recorder != null)
            foreach (var score in EditablePvpScores.Where(EditShowsScore))
                editScoreTexts[score.Column] = score.Read(recorder)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        editDeathTexts.Clear();
        foreach (var player in editDraft.Party)
            editDeathTexts[player] = record.DutyType == "PvP"
                ? player.PvPScoreboardDeaths?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
                : player.DeathCount.ToString(CultureInfo.InvariantCulture);
    }

    private static void EditText(string label, string value, Action<string> update, int limit = 256)
    {
        if (ImGui.InputText(label, ref value, limit))
            update(value);
    }

    private static void EditChoice(string label, string value, IEnumerable<string> choices, Action<string> update)
    {
        if (!ImGui.BeginCombo(label, value))
            return;
        try
        {
            foreach (var choice in choices.Prepend(value).Distinct(StringComparer.Ordinal))
                if (ImGui.Selectable(choice, value == choice))
                    update(choice);
        }
        finally { ImGui.EndCombo(); }
    }

    private void DrawRecordEditFields()
    {
        if (editDraft == null)
            return;
        var draft = editDraft;
        if (EditShows("duty")) EditText("Duty", draft.DutyName, value => draft.DutyName = value);
        if (EditShows("type")) EditChoice("Type", DutyClassification.NormalizeType(draft.DutyType),
            ["Dungeons", "Guildhests", "Trials", "Extreme Trials", "Raids", "Savage Raids", "Ultimate Raids",
                "Alliance Raids", "PvP", "Deep Dungeons", "V&C Dungeon", "Chaotic Alliance Raid"], value => draft.DutyType = value);
        if (EditShows("job")) EditText("Your job", draft.PlayerJob, value => draft.PlayerJob = value, 16);
        if (EditShows("started")) ImGui.InputText("Started (yyyy-MM-dd HH:mm:ss)", ref editStarted, 32);
        if (EditShowsDuration) ImGui.InputText("Time in duty (hh:mm:ss)", ref editDuration, 32);
        if (EditShows("joined"))
        {
            var value = draft.JoinedInProgress;
            if (ImGui.Checkbox("Joined in progress", ref value)) draft.JoinedInProgress = value;
        }
        if (EditShows("wipes"))
        {
            var value = draft.WipeCount;
            if (ImGui.InputInt("Wipes", ref value)) draft.WipeCount = value;
        }
        if (EditShows("completed"))
        {
            var value = draft.Completed;
            if (ImGui.Checkbox("Completed", ref value)) draft.Completed = value;
        }
        if (recordToEdit?.DutyType == "PvP") DrawPvpRecordEditFields(draft);
        if (new[] { "players", "lalafell", "sprouts", "mentors", "deaths" }.Any(EditShows))
            DrawPlayerCountEditFields(draft);
    }

    private void DrawPvpRecordEditFields(DutyRecord draft)
    {
        if (EditShows("pvpPlacement"))
        {
            var placement = draft.PvPPlacement ?? 0;
            string[] options = DutyClassification.Mode(draft) is PvP.PvpMode.CrystallineConflict or PvP.PvpMode.RivalWings
                ? ["Unknown", "Win", "Loss"] : ["Unknown", "1st", "2nd", "3rd"];
            if (ImGui.Combo("Your team's placement", ref placement, options, options.Length))
                draft.PvPPlacement = placement == 0 ? null : placement;
        }
        foreach (var score in EditablePvpScores.Where(EditShowsScore))
        {
            if (!editScoreTexts.TryGetValue(score.Column, out var value))
            {
                ImGui.TextWrapped("Your recording player isn't in this duty's player list, so personal PvP scores can't be edited yet.");
                break;
            }
            if (ImGui.InputText(score.Label, ref value, 32)) editScoreTexts[score.Column] = value;
            if (score.RateColumn != null && EditShows(score.RateColumn))
                ImGui.TextDisabled("Per-minute values recalculate from the score and time in duty.");
        }
        if (EditShows("pvpPlacement") || EditablePvpScores.Any(EditShowsScore) || EditShows("deaths") || EditShows("completed"))
            ImGui.TextWrapped("Leave an unknown score blank. PvP Tracker may update these values if more results arrive.");
    }

    private void DrawPlayerCountEditFields(DutyRecord draft)
    {
        ImGui.Separator();
        ImGui.TextUnformatted("Player details for counts");
        ImGui.TextWrapped("Counts come from these players and your filters. Changing death totals doesn't change the death timeline.");
        for (var i = 0; i < draft.Party.Count; i++)
        {
            var player = draft.Party[i];
            ImGui.PushID(i);
            try
            {
                var label = string.IsNullOrWhiteSpace(player.Name) ? "New player" : $"{player.Name}@{player.World}";
                if (!ImGui.TreeNodeEx(label + "###EditPlayer", ImGuiTreeNodeFlags.SpanAvailWidth))
                    continue;
                try
                {
                    if (EditShows("players"))
                    {
                        var isRecorder = player.Name == draft.RecorderName && player.World == draft.RecorderWorld;
                        EditText("Name", player.Name, value => player.Name = value);
                        EditText("World", player.World, value => player.World = value, 64);
                        if (isRecorder)
                        {
                            draft.RecorderName = player.Name;
                            draft.RecorderWorld = player.World;
                        }
                    }
                    if (EditShows("lalafell"))
                    {
                        EditChoice("Race", player.Race, EncounterRaces.Skip(1).Append("Unknown"), value => player.Race = value);
                        EditChoice("Gender", player.Gender, ["Male", "Female", "Unknown"], value => player.Gender = value);
                    }
                    if (EditShows("sprouts") || EditShows("mentors"))
                        EditChoice("Status", player.Status, ["Sprout", "Mentor", "Trade Mentor", "Returner", "Party Member", "Unknown"], value => player.Status = value);
                    if (EditShows("deaths"))
                    {
                        var value = editDeathTexts.GetValueOrDefault(player, string.Empty);
                        if (ImGui.InputText("Deaths", ref value, 16)) editDeathTexts[player] = value;
                    }
                    if (EditShows("players") && ImGui.SmallButton("Remove player"))
                    {
                        draft.Party.RemoveAt(i--);
                        editDeathTexts.Remove(player);
                    }
                }
                finally { ImGui.TreePop(); }
            }
            finally { ImGui.PopID(); }
        }
        if (EditShows("players") && ImGui.Button("Add player"))
        {
            var player = new PartyMemberSnapshot { Race = "Unknown", Gender = "Unknown", Status = "Unknown" };
            draft.Party.Add(player);
            editDeathTexts[player] = draft.DutyType == "PvP" ? string.Empty : "0";
        }
    }

    private static bool ReadEditedScore(string text, long maximum, out long? score)
    {
        score = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!long.TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value) || value < 0 || value > maximum)
            return false;
        score = value;
        return true;
    }

    private bool TrySaveEditedRecord()
    {
        bool Error(string message) { editError = message; return false; }
        if (recordToEdit == null || editDraft == null || !config.History.Records.Contains(recordToEdit))
            return Error("This record is no longer in your history.");
        if (config.History.ChangeVersion != editHistoryVersion)
            return Error("History changed while you were editing. Cancel and reopen the editor to get the latest values.");
        var draft = editDraft;
        if (string.IsNullOrWhiteSpace(draft.DutyName)) return Error("Give the duty a name.");
        if (EditShows("type") && DutyClassification.DutyType(draft) != draft.DutyType)
            return Error("An (Ultimate) duty name uses Ultimate Raids. Correct the duty name too if its type is wrong.");
        if (EditShows("wipes") && (draft.WipeCount < 0 || draft.WipeCount > 99)) return Error("Keep wipes between 0 and 99.");
        if (EditShows("started") && editStarted != editOriginalStarted)
        {
            if (!DateTime.TryParseExact(editStarted, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var started))
                return Error("Use yyyy-MM-dd HH:mm:ss for the start time.");
            draft.Timestamp = started;
        }
        if (EditShowsDuration)
        {
            if (!TimeSpan.TryParseExact(editDuration, "c", CultureInfo.InvariantCulture, out var duration) || duration < TimeSpan.Zero)
                return Error("Use hh:mm:ss for time in duty, with no negative values.");
            if (draft.DeathTimestamps.Any(death => death.Elapsed > duration))
                return Error("Time in duty can't end before a recorded death timestamp.");
            draft.TimeInDuty = duration;
        }
        if (EditShows("players") && draft.Party.Any(p => string.IsNullOrWhiteSpace(p.Name)))
            return Error("Each player needs a name, or remove the empty player.");
        if (EditShows("players") && draft.Party.Select(p => (p.Name, p.World)).Distinct().Count() != draft.Party.Count)
            return Error("The same player and world are listed more than once.");
        if (EditShows("pvpPlacement") && draft.DutyType == "PvP" && draft.PvPPlacement == 3 &&
            DutyClassification.Mode(draft) is PvP.PvpMode.CrystallineConflict or PvP.PvpMode.RivalWings)
            return Error("Crystalline Conflict and Rival Wings results must be a win or a loss.");
        if (EditShows("deaths"))
            foreach (var player in draft.Party)
            {
                if (!ReadEditedScore(editDeathTexts.GetValueOrDefault(player, string.Empty), int.MaxValue, out var deaths) ||
                    (recordToEdit.DutyType != "PvP" && deaths == null)) return Error("Deaths must be a whole number of zero or more.");
                if (recordToEdit.DutyType == "PvP") player.PvPScoreboardDeaths = (int?)deaths;
                else player.DeathCount = (int)deaths!;
            }
        var recorder = draft.Party.FirstOrDefault(p => p.Name == draft.RecorderName && p.World == draft.RecorderWorld);
        if (recordToEdit.DutyType == "PvP" && recorder != null)
        {
            foreach (var score in EditablePvpScores.Where(EditShowsScore))
            {
                if (!ReadEditedScore(editScoreTexts.GetValueOrDefault(score.Column, string.Empty), score.Maximum, out var value))
                    return Error($"{score.Label} must be blank or a whole number from 0 to {score.Maximum:N0}.");
                score.Write(recorder, value);
            }
            if (EditShows("pvpPlacement")) recorder.PvPPlacement = draft.PvPPlacement;
        }
        if (EditShows("job") && recorder != null) recorder.Job = draft.PlayerJob;
        recordToEdit.DutyName = draft.DutyName.Trim();
        recordToEdit.DutyType = draft.DutyType;
        recordToEdit.PlayerJob = draft.PlayerJob;
        recordToEdit.Timestamp = draft.Timestamp;
        recordToEdit.TimeInDuty = draft.TimeInDuty;
        recordToEdit.WipeCount = draft.WipeCount;
        recordToEdit.JoinedInProgress = draft.JoinedInProgress;
        recordToEdit.Completed = draft.Completed;
        recordToEdit.PvPPlacement = draft.PvPPlacement;
        recordToEdit.Party = draft.Party;
        recordToEdit.RecorderName = draft.RecorderName;
        recordToEdit.RecorderWorld = draft.RecorderWorld;
        if (EditShows("players") || EditShows("lalafell"))
            recordToEdit.LalafellCount = draft.Party.Count(p => p.Race == "Lalafell" &&
                (p.Name != draft.RecorderName || p.World != draft.RecorderWorld));
        config.History.Save();
        editError = string.Empty;
        return true;
    }
}
