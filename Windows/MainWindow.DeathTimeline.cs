using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private void DrawDeathTimeline(DutyRecord record)
    {
        if (record.DutyType == "PvP" || !ImGui.CollapsingHeader("Deaths##DutyTimeline"))
            return;

        ImGui.TextWrapped("Pulls follow continuous observed combat. Pull time starts at combat entry; All deaths uses duty time.");
        if (!record.DeathTimestampsRecorded)
            ImGui.TextWrapped("Timestamps weren't recorded from the start of this duty. Older death totals are still kept.");

        if (!record.PullsRecorded)
            ImGui.TextWrapped("Pull boundaries weren't recorded from the start. Unassigned deaths stay separate.");

        foreach (var pull in record.Pulls)
        {
            var deaths = record.DeathTimestamps.Where(death => death.PullNumber == pull.Number && !death.BeforePull).ToList();
            ImGui.PushID(pull.Number);
            try
            {
                var outcome = pull.Outcome switch
                {
                    DutyPullOutcome.InProgress => "In progress",
                    DutyPullOutcome.CombatEnded => "Combat ended",
                    DutyPullOutcome.Wipe => "Wipe",
                    DutyPullOutcome.Reset => "Reset",
                    DutyPullOutcome.DutyCompleted => "Duty completed",
                    DutyPullOutcome.DutyLeft => "Left duty",
                    _ => "Recording interrupted"
                };
                if (!ImGui.TreeNodeEx($"Pull {pull.Number} — {outcome} ({deaths.Count} deaths)###Pull", ImGuiTreeNodeFlags.SpanAvailWidth))
                    continue;
                try
                {
                    if (pull.Started is { } start)
                    {
                        ImGui.TextUnformatted($"Started at {DeathTime(start)} into the duty");
                        if (pull.Ended is { } end) ImGui.TextUnformatted($"Duration: {DeathTime(end - start)}");
                    }
                    else ImGui.TextWrapped("The start wasn't observed. Times below are from duty entry.");
                    if (deaths.Count == 0) ImGui.TextDisabled("No deaths recorded for this pull.");
                    else DrawDeathRows("PullDeaths", deaths, pull.Started);
                }
                finally { ImGui.TreePop(); }
            }
            finally { ImGui.PopID(); }
        }

        var unassigned = record.DeathTimestamps.Where(death => death.PullNumber == null || death.BeforePull).ToList();
        if (unassigned.Count > 0 && ImGui.TreeNodeEx($"Unassigned deaths ({unassigned.Count})", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            try { DrawDeathRows("UnassignedDeaths", unassigned); }
            finally { ImGui.TreePop(); }
        }

        if (record.DeathTimestamps.Count == 0 && record.Pulls.Count == 0)
        {
            ImGui.TextDisabled(record.DeathTimestampsRecorded ? "No deaths recorded." : "No death timestamps available.");
            return;
        }

        if (record.DeathTimestamps.Count > 0 && ImGui.TreeNodeEx("All deaths (duty time)", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            try { DrawDeathRows("DutyDeathTimeline", record.DeathTimestamps); }
            finally { ImGui.TreePop(); }
        }
        ImGui.Spacing();
    }

    private static string DeathTime(TimeSpan time) => time.TotalHours >= 1
        ? $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
        : $"{time.Minutes:00}:{time.Seconds:00}";

    private void DrawDeathRows(string id, IEnumerable<DeathTimestamp> deaths, TimeSpan? pullStart = null)
    {
        if (!BeginStyledTable(id, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;
        try
        {
            ImGui.TableSetupColumn(pullStart.HasValue ? "Pull time" : "Duty time", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("000:00:00").X);
            ImGui.TableSetupColumn("Player");
            ImGui.TableHeadersRow();
            foreach (var death in deaths.OrderBy(death => death.Elapsed))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                var time = death.Elapsed - (pullStart ?? TimeSpan.Zero);
                ImGui.TextUnformatted(DeathTime(time < TimeSpan.Zero ? TimeSpan.Zero : time));
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(string.IsNullOrWhiteSpace(death.World)
                    ? death.Name : $"{death.Name}@{death.World}");
            }
        }
        finally { EndStyledTable(); }
        ImGui.Spacing();
    }
}
