using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;
using MentorRouletteTracker.PvP;
using System.Globalization;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private bool editRepairMode;
    private bool repairRosterComplete;
    private readonly Dictionary<PartyMemberSnapshot, Dictionary<string, string>> repairScoreTexts = new();

    private void StartRecordRepair(DutyRecord record)
    {
        StartEditingRecord(record);
        editRepairMode = true;
        repairRosterComplete = record.PvPRosterComplete == true;
        IsOpen = true;
    }

    private Dictionary<string, string> RepairScores(PartyMemberSnapshot player)
    {
        if (!repairScoreTexts.TryGetValue(player, out var scores))
        {
            scores = EditablePvpScores.ToDictionary(score => score.Column,
                score => score.Read(player)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            repairScoreTexts.Add(player, scores);
        }
        return scores;
    }

    private void DrawRecordRepairFields(DutyRecord draft)
    {
        ImGui.TextWrapped("Fill in what you know. Anything still missing stays unknown. Changes apply when you save.");
        var recorder = string.IsNullOrWhiteSpace(draft.RecorderName) ? "Choose your character" : $"{draft.RecorderName}@{draft.RecorderWorld}";
        if (ImGui.BeginCombo("Recording character", recorder))
        {
            try
            {
                foreach (var player in draft.Party.Where(player => !string.IsNullOrWhiteSpace(player.Name)))
                    if (ImGui.Selectable($"{player.Name}@{player.World}",
                            player.Name == draft.RecorderName && player.World == draft.RecorderWorld))
                    {
                        draft.RecorderName = player.Name;
                        draft.RecorderWorld = player.World;
                    }
            }
            finally { ImGui.EndCombo(); }
        }
        EditText("Your character's name", draft.RecorderName, value => draft.RecorderName = value);
        EditText("Your home world", draft.RecorderWorld, value => draft.RecorderWorld = value, 64);
        var pvp = draft.DutyType == "PvP";
        if (!pvp) ImGui.TextWrapped("Changing death totals doesn't change the recorded death timeline.");
        if (pvp)
        {
            ImGui.Checkbox("This recorded player list is complete", ref repairRosterComplete);
            ImGui.TextWrapped("Check this once everyone you wanted to record is listed. Leave unknown scores blank. PvP Tracker may update these values if more results arrive.");
        }
        ImGui.Separator();
        for (var i = 0; i < draft.Party.Count; i++)
        {
            var player = draft.Party[i];
            ImGui.PushID(i);
            try
            {
                var label = string.IsNullOrWhiteSpace(player.Name) ? "New player" : $"{player.Name}@{player.World}";
                if (!ImGui.TreeNodeEx(label + "###RepairPlayer", ImGuiTreeNodeFlags.SpanAvailWidth)) continue;
                try
                {
                    var wasRecorder = player.Name == draft.RecorderName && player.World == draft.RecorderWorld;
                    EditText("Name", player.Name, value => player.Name = value);
                    EditText("World", player.World, value => player.World = value, 64);
                    if (wasRecorder)
                    {
                        draft.RecorderName = player.Name;
                        draft.RecorderWorld = player.World;
                    }
                    EditText("Job", player.Job, value => player.Job = value, 16);
                    EditChoice("Race", player.Race, EncounterRaces.Skip(1).Append("Unknown"), value => player.Race = value);
                    EditChoice("Gender", player.Gender, ["Unknown", "Male", "Female"], value => player.Gender = value);
                    EditChoice("Status", player.Status, ["Unknown", "Party Member", "Sprout", "Mentor", "Trade Mentor", "Returner"], value => player.Status = value);
                    var deaths = editDeathTexts.GetValueOrDefault(player, pvp ? string.Empty : "0");
                    if (ImGui.InputText("Deaths", ref deaths, 32)) editDeathTexts[player] = deaths;
                    if (pvp)
                    {
                        var side = player.PvPIsEnemy.HasValue ? player.PvPIsEnemy.Value ? 2 : 1 : 0;
                        if (ImGui.Combo("Side", ref side, ["Unknown", "Teammate", "Enemy"], 3))
                            player.PvPIsEnemy = side == 0 ? null : side == 2;
                        var placement = player.PvPPlacement ?? 0;
                        string[] results = DutyClassification.Mode(draft) == PvpMode.Frontline
                            ? ["Unknown", "1st", "2nd", "3rd"] : ["Unknown", "Win", "Loss"];
                        if (ImGui.Combo("Result", ref placement, results, results.Length))
                            player.PvPPlacement = placement == 0 ? null : placement;
                        var scores = RepairScores(player);
                        foreach (var score in EditablePvpScores)
                        {
                            var value = scores[score.Column];
                            if (ImGui.InputText(score.Label.Replace("Your PvP ", "", StringComparison.Ordinal), ref value, 32))
                                scores[score.Column] = value;
                        }
                    }
                    if (ImGui.SmallButton("Remove player"))
                    {
                        draft.Party.RemoveAt(i--);
                        editDeathTexts.Remove(player);
                        repairScoreTexts.Remove(player);
                    }
                }
                finally { ImGui.TreePop(); }
            }
            finally { ImGui.PopID(); }
        }
        if (ImGui.Button("Add player"))
        {
            var player = new PartyMemberSnapshot { Job = "Unknown", Race = "Unknown", Gender = "Unknown", Status = "Unknown" };
            draft.Party.Add(player);
            editDeathTexts[player] = pvp ? string.Empty : "0";
        }
    }

    private bool TrySaveRecordRepair(DutyRecord record, DutyRecord draft)
    {
        bool Error(string message) { editError = message; return false; }
        if (draft.Party.Any(player => string.IsNullOrWhiteSpace(player.Name) || string.IsNullOrWhiteSpace(player.World)))
            return Error("Each player needs a name and home world. Remove any empty entries.");
        if (draft.Party.Select(player => (player.Name, player.World)).Distinct().Count() != draft.Party.Count)
            return Error("The same player and world are listed more than once.");
        if (string.IsNullOrWhiteSpace(draft.RecorderName) != string.IsNullOrWhiteSpace(draft.RecorderWorld))
            return Error("Enter both your character name and home world, or leave both blank.");
        var pvp = record.DutyType == "PvP";
        foreach (var player in draft.Party)
        {
            if (!ReadEditedScore(editDeathTexts.GetValueOrDefault(player, pvp ? string.Empty : "0"), int.MaxValue, out var deaths) ||
                (!pvp && deaths == null)) return Error($"Deaths for {player.Name} must be a whole number of zero or more.");
            if (pvp)
            {
                player.PvPScoreboardDeaths = (int?)deaths;
                if (player.PvPPlacement.HasValue && !PvpMatchResult.IsValidPlacement(DutyClassification.Mode(draft), player.PvPPlacement))
                    return Error($"Choose a valid result for {player.Name}.");
                var scores = RepairScores(player);
                foreach (var score in EditablePvpScores)
                {
                    if (!ReadEditedScore(scores[score.Column], score.Maximum, out var value))
                        return Error($"The {score.Label.Replace("Your PvP ", "", StringComparison.Ordinal)} score for {player.Name} must be blank or a whole number from 0 to {score.Maximum:N0}.");
                    score.Write(player, value);
                }
            }
            else player.DeathCount = (int)deaths!;
        }
        if (pvp)
        {
            var hasPlayers = draft.Party.Count > 0;
            record.PvPRosterComplete = repairRosterComplete && hasPlayers;
            record.PvPCombatStatsComplete = repairRosterComplete && hasPlayers && draft.Party.All(player =>
                player.PvPScoreboardDeaths.HasValue && player.PvPDamageDealt.HasValue && player.PvPKills.HasValue &&
                player.PvPAssists.HasValue && player.PvPIsEnemy.HasValue);
            record.PvPResultsComplete = repairRosterComplete && hasPlayers && draft.Party.All(player => player.PvPPlacement.HasValue);
            record.PvPExtendedStatsComplete = repairRosterComplete && hasPlayers && draft.Party.All(player =>
                player.PvPDamageTaken.HasValue && player.PvPHealingDone.HasValue);
            if (record.Completed && record.PvPRosterComplete == true && record.PvPCombatStatsComplete &&
                record.PvPResultsComplete && record.PvPExtendedStatsComplete) record.PvPLookupFinished = true;
        }
        record.Party = draft.Party;
        record.RecorderName = draft.RecorderName.Trim();
        record.RecorderWorld = draft.RecorderWorld.Trim();
        var recorder = record.Party.FirstOrDefault(player => player.Name == record.RecorderName && player.World == record.RecorderWorld);
        if (recorder != null)
        {
            record.PlayerJob = recorder.Job;
            if (pvp) record.PvPPlacement = recorder.PvPPlacement;
        }
        record.LalafellCount = record.Party.Count(player => player.Race == "Lalafell" &&
            (player.Name != record.RecorderName || player.World != record.RecorderWorld));
        config.History.Save();
        editError = string.Empty;
        return true;
    }
}
