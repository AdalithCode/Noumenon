using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private sealed record RunPerformance(int Runs, double Seconds, double? Deaths, int KnownDeathRuns, double Wipes);
    private sealed record PerformanceRun(double Seconds, double? Deaths, int Wipes);
    private sealed record DutyPerformance(List<PerformanceRun> Runs, RunPerformance Recent, RunPerformance Overall);

    private static RunPerformance SummarizePerformance(List<PerformanceRun> runs)
    {
        var known = runs.Select(run => run.Deaths).Where(value => value.HasValue).ToList();
        return new RunPerformance(runs.Count, runs.Average(run => run.Seconds), known.Average(),
            known.Count, runs.Average(run => run.Wipes));
    }

    private DutyPerformance? GetDutyPerformance(DutyAnalysisData data)
    {
        if (data.FullRuns.Count == 0) return null;
        var recentCount = Math.Clamp(config.RecentPerformanceRuns, 1, data.FullRuns.Count);
        if (data.Performance is { } cached)
        {
            if (cached.Recent.Runs == recentCount) return cached;
            return data.Performance = cached with
            {
                Recent = SummarizePerformance(cached.Runs.Take(recentCount).ToList())
            };
        }
        var runs = data.FullRuns.OrderByDescending(record => record.Timestamp)
            .Select(record => new PerformanceRun(record.TimeInDuty.TotalSeconds, KnownDeathsForRun(record), record.WipeCount)).ToList();
        return data.Performance = new DutyPerformance(runs, SummarizePerformance(runs.Take(recentCount).ToList()),
            SummarizePerformance(runs));
    }

    private static string PerformanceChange(double? recent, double? overall, string lower, string higher, bool time = false)
    {
        if (!recent.HasValue || !overall.HasValue) return "-";
        var delta = recent.Value - overall.Value;
        if (Math.Abs(delta) < (time ? 0.5 : 0.005)) return "No change";
        var amount = time ? FormatDuration(TimeSpan.FromSeconds(Math.Round(Math.Abs(delta)))) : Math.Abs(delta).ToString("F2");
        return $"{amount} {(delta < 0 ? lower : higher)}";
    }

    private void SetRecentRunCount(int count)
    {
        count = Math.Max(1, count);
        if (config.RecentPerformanceRuns == count) return;
        config.RecentPerformanceRuns = count;
        appearanceSettingsDirty = true;
    }

    private void DrawRecentRunControls(int totalRuns)
    {
        var recentCount = Math.Clamp(config.RecentPerformanceRuns, 1, totalRuns);
        var overallLabel = $"runs. Overall: {totalRuns} runs.";
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Recent:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(Math.Max(ImGui.GetFontSize() * 6,
            Math.Min(ImGui.GetFontSize() * 12, ImGui.GetContentRegionAvail().X -
                ImGui.CalcTextSize(overallLabel).X - ImGui.GetStyle().ItemSpacing.X)));
        if (ImGui.InputInt("##RecentPerformanceRuns", ref recentCount, 1, 10))
            SetRecentRunCount(Math.Clamp(recentCount, 1, totalRuns));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Type a run count or use -/+. Only completed runs entered from the start count. Filters apply to both samples; Overall includes Recent.");
        ImGui.SameLine();
        ImGui.TextWrapped(overallLabel);

        void Preset(string label, int count)
        {
            var selected = config.RecentPerformanceRuns == count;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            try
            {
                if (ImGui.Button(label + "##RecentRuns")) SetRecentRunCount(count);
            }
            finally { if (selected) ImGui.PopStyleColor(); }
        }
        foreach (var count in new[] { 5, 10, 25, 50 })
        {
            if (count != 5) ImGui.SameLine();
            Preset(count.ToString(), count);
        }
    }

    private void DrawRecentPerformance(DutyAnalysisData data)
    {
        DrawSectionHeading("Recent performance");
        ImGui.Spacing();
        if (data.FullRuns.Count == 0)
        {
            ImGui.TextWrapped("No completed runs entered from the start match these filters.");
            return;
        }
        DrawRecentRunControls(data.FullRuns.Count);
        var performance = GetDutyPerformance(data);
        if (performance == null) return;
        var recent = performance.Recent;
        var overall = performance.Overall;
        ImGui.Spacing();
        if (BeginStyledTable("RecentPerformance", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn("Average", ImGuiTableColumnFlags.WidthStretch, 1.5f);
                ImGui.TableSetupColumn("Recent");
                ImGui.TableSetupColumn("Overall");
                ImGui.TableSetupColumn("Change", ImGuiTableColumnFlags.WidthStretch, 1.5f);
                ImGui.TableHeadersRow();
                void Row(string label, string current, string baseline, string difference)
                {
                    ImGui.TableNextRow();
                    DrawTableCell(0, label);
                    DrawTableCell(1, current);
                    DrawTableCell(2, baseline);
                    DrawTableCell(3, difference);
                }
                Row("Run time", FormatDuration(TimeSpan.FromSeconds(recent.Seconds)), FormatDuration(TimeSpan.FromSeconds(overall.Seconds)),
                    PerformanceChange(recent.Seconds, overall.Seconds, "faster", "slower", time: true));
                Row("Deaths per run", recent.Deaths?.ToString("F2") ?? "-", overall.Deaths?.ToString("F2") ?? "-",
                    PerformanceChange(recent.Deaths, overall.Deaths, "fewer", "more"));
                Row("Wipes per run", recent.Wipes.ToString("F2"), overall.Wipes.ToString("F2"),
                    PerformanceChange(recent.Wipes, overall.Wipes, "fewer", "more"));
            }
            finally { EndStyledTable(); }
        }
    }
}
