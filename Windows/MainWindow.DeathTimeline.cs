using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private void DrawDeathTimeline(DutyRecord record)
    {
        if (record.DutyType == "PvP" || !ImGui.CollapsingHeader("Deaths##DutyTimeline"))
            return;

        ImGui.TextWrapped("Time since entering the duty, when your client detected the death.");
        if (!record.DeathTimestampsRecorded)
            ImGui.TextWrapped("Timestamps weren't recorded from the start of this duty. Older death totals are still kept.");

        if (record.DeathTimestamps.Count == 0)
        {
            ImGui.TextDisabled(record.DeathTimestampsRecorded ? "No deaths recorded." : "No death timestamps available.");
            return;
        }

        if (!BeginStyledTable("DutyDeathTimeline", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            return;
        try
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("000:00:00").X);
            ImGui.TableSetupColumn("Player");
            ImGui.TableHeadersRow();
            foreach (var death in record.DeathTimestamps.OrderBy(death => death.Elapsed))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                var time = death.Elapsed;
                ImGui.TextUnformatted(time.TotalHours >= 1
                    ? $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
                    : $"{time.Minutes:00}:{time.Seconds:00}");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(string.IsNullOrWhiteSpace(death.World)
                    ? death.Name : $"{death.Name}@{death.World}");
            }
        }
        finally { EndStyledTable(); }
        ImGui.Spacing();
    }
}
