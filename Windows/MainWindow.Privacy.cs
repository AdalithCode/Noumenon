using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private readonly PlayerAnonymizer playerAnonymizer = new();
    private long privacyHistoryVersion = -1;
    private long privacyReplacementVersion = -1;
    private int privacyRecordCount = -1;
    private DutyRecord? privacyPendingDuty;
    private int privacyPendingPlayers = -1;

    private PlayerAnonymizer GetPlayerAnonymizer()
    {
        if (!config.StorageAvailable) return playerAnonymizer;
        var history = config.History;
        if (privacyReplacementVersion != history.ReplacementVersion)
        {
            playerAnonymizer.Clear();
            privacyHistoryVersion = -1;
            privacyReplacementVersion = history.ReplacementVersion;
        }
        if (privacyHistoryVersion != history.ChangeVersion || privacyRecordCount != history.Records.Count ||
            !ReferenceEquals(privacyPendingDuty, history.PendingDuty) || privacyPendingPlayers != (history.PendingDuty?.Party.Count ?? 0))
        {
            IEnumerable<DutyRecord> records = history.Records;
            if (history.PendingDuty is { } pending) records = records.Append(pending);
            playerAnonymizer.Refresh(records);
            privacyHistoryVersion = history.ChangeVersion;
            privacyRecordCount = history.Records.Count;
            privacyPendingDuty = history.PendingDuty;
            privacyPendingPlayers = history.PendingDuty?.Party.Count ?? 0;
        }
        return playerAnonymizer;
    }

    private string DisplayPlayerName(string name, string world, string? job = null) => config.AnonymousMode
        ? GetPlayerAnonymizer().Name(name, world, config.AnonymousUseJobs, job) : name;
    private string DisplayPlayerName(PartyMemberSnapshot player) => DisplayPlayerName(player.Name, player.World, player.Job);
    private string DisplayPlayerWorld(string world) => config.AnonymousMode && config.AnonymousHideWorlds ? "-" : world;
    private string DisplayPlayerIdentity(string name, string world, string? job = null)
    {
        var label = DisplayPlayerName(name, world, job);
        return string.IsNullOrWhiteSpace(world) || config.AnonymousMode && config.AnonymousHideWorlds ? label : $"{label}@{world}";
    }

    private void DrawPrivacySettings()
    {
        var anonymous = config.AnonymousMode;
        if (ImGui.Checkbox("Anonymous mode", ref anonymous))
        {
            config.AnonymousMode = anonymous;
            globalPlayerSearchText = string.Empty;
            globalDutySearchText = string.Empty;
            config.Save();
        }
        ImGui.TextWrapped("Hide character names for screenshots or streaming. Original names stay in your local history and backups. Notes and record editing are hidden while this is on.");
        var style = config.AnonymousUseJobs ? 1 : 0;
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 18);
        if (ImGui.Combo("Player labels", ref style, ["Player 1, Player 2...", "Job labels (WHM 1, DRG 2...)"], 2))
        {
            config.AnonymousUseJobs = style == 1;
            config.Save();
        }
        var hideWorlds = config.AnonymousHideWorlds;
        if (ImGui.Checkbox("Hide home worlds beside players", ref hideWorlds))
        {
            config.AnonymousHideWorlds = hideWorlds;
            config.Save();
        }
        ImGui.TextWrapped("World and datacenter statistics still show their group names. Duty details use the job recorded for that run. Player summaries use the most observed job across your history; ties use the most recent.");
    }
}
