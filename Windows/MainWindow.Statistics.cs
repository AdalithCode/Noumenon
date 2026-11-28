
using Dalamud.Bindings.ImGui;
using MentorRouletteTracker.DutyTracking;
using System.Numerics;

namespace MentorRouletteTracker
{
    public partial class MainWindow
    {
        private string? statisticsCategory;
        private static readonly string[] StatisticsCategories =
            ["Overview", "Race", "Gender", "Job", "Status", "Home world", "Datacenter"];

        private void DrawStatisticsTab()
        {
            var data = GetPlayerViewData();
            try
            {
                if (!ImGui.BeginChild("StatisticsContent", Vector2.Zero, false)) return;
                if (data.Records.Count == 0)
                {
                    ImGui.TextWrapped("No completed duties with these filters.");
                    return;
                }
                var overview = data.Overview;
                DrawDashboardCards("Statistics",
                [
                    new("Unique players", overview.UniquePlayers.ToString()),
                    new("Encounters", overview.TotalEncounters.ToString()),
                    new("Matching duties", overview.DutiesWithPlayers.ToString()),
                    new("Current drought", $"{overview.CurrentDrought} duties", $"Duties since the last {EncounterTargetLabel} encounter, using your filters.")
                ]);
                ImGui.Spacing();
                statisticsCategory = DrawCategoryPage("Statistics", StatisticsCategories, statisticsCategory, title =>
                {
                    switch (title)
                    {
                        case "Overview": DrawPlayerDroughtStatistics(data); break;
                        case "Race": DrawRaceStatistics(data); break;
                        case "Gender": DrawPlayerStatisticTable(data, PlayerStatistic.Gender); break;
                        case "Job": DrawPlayerStatisticTable(data, PlayerStatistic.Job); break;
                        case "Status": DrawPlayerStatisticTable(data, PlayerStatistic.Status); break;
                        case "Home world": DrawPlayerStatisticTable(data, PlayerStatistic.World); break;
                        case "Datacenter": DrawPlayerStatisticTable(data, PlayerStatistic.Datacenter); break;
                    }
                });
            }
            finally { ImGui.EndChild(); }
        }

        private void DrawPlayerDroughtStatistics(PlayerViewData data)
        {
            DrawSectionHeading("Encounter target");
            ImGui.Spacing();
            if (!BeginStyledTable("PlayerDroughtDetails", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
            try
            {
                ImGui.TableSetupColumn("Statistic");
                ImGui.TableSetupColumn("Value");
                DrawStatisticRow("Encounter target", EncounterTargetLabel);
                DrawStatisticRow("Longest drought", $"{data.Overview.LongestDrought} duties");
            }
            finally { EndStyledTable(); }
        }

        private static int? EffectiveDeaths(
            DutyRecord record,
            PartyMemberSnapshot player)
        {
            return record.DutyType == "PvP"
                ? player.PvPScoreboardDeaths
                : player.DeathCount;
        }

        private static int? TotalKnownDeaths(IEnumerable<int?> counts)
        {
            var known = counts.Where(count => count.HasValue).ToList();
            return known.Count == 0 ? null : known.Sum(count => count.GetValueOrDefault());
        }

        private void DrawRaceStatistics(PlayerViewData data)
        {
            DrawPlayerStatisticTable(data, PlayerStatistic.Race);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawPlayerStatisticTable(data, PlayerStatistic.RaceAndGender);
        }

        private void DrawPlayerStatisticTable(PlayerViewData data, PlayerStatistic statistic)
        {
            var (title, label, tableId) = statistic switch
            {
                PlayerStatistic.Race => ("Players by race", "Race", "raceStats"),
                PlayerStatistic.RaceAndGender => ("Players by race and gender", "Race", "raceGenderStats"),
                PlayerStatistic.Gender => ("Players by gender", "Gender", "genderStats"),
                PlayerStatistic.Job => ("Players by job", "Job", "jobStats"),
                PlayerStatistic.Status => ("Players by status", "Status", "statsStatusDistribution"),
                PlayerStatistic.World => ("Players by home world", "World", "statsWorldDistribution"),
                PlayerStatistic.Datacenter => ("Players by datacenter", "Datacenter", "statsDatacenterDistribution"),
                _ => throw new ArgumentOutOfRangeException(nameof(statistic))
            };
            DrawSectionHeading(title);
            ImGui.Spacing();
            var rows = GetPlayerStatistics(data, statistic);
            if (rows.Count == 0)
            {
                ImGui.TextDisabled("No players match these filters in this section.");
                return;
            }

            var key = StatisticTableKey(statistic);
            var detail = statistic == PlayerStatistic.RaceAndGender;
            var total = rows.Sum(row => row.Seen);
            if (BeginConfiguredTable(key, tableId, showDetail: detail, fitRows: rows.Count))
            {
                try
                {
                    SetupConfiguredColumns(key, label, detail);
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    if (sortSpecs.SpecsCount > 0 && IsTableColumnVisible(key, ConfiguredSortColumn(sortSpecs.Specs.ColumnIndex)) &&
                        (ConfiguredSortColumn(sortSpecs.Specs.ColumnIndex) != 1 || detail))
                    {
                        var spec = sortSpecs.Specs;
                        rows = GetSortedPlayerStatistics(data, statistic, ConfiguredSortColumn(spec.ColumnIndex),
                            spec.SortDirection == ImGuiSortDirection.Ascending);
                        sortSpecs.SpecsDirty = false;
                    }

                    foreach (var row in rows)
                    {
                        ImGui.TableNextRow();
                        DrawTableCell(0, row.Name);
                        DrawTableCell(1, row.Detail);
                        DrawTableCell(2, row.Seen.ToString());
                        DrawTableCell(3, row.Deaths?.ToString() ?? "-");
                        DrawTableCell(4, row.AverageDeaths?.ToString("F2") ?? "-");
                        DrawTableCell(5, $"{(double)row.Seen / total * 100:F1}%");
                        DrawPvpStatisticCells(6, row.Pvp, averages: true);
                    }
                }
                finally
                {
                    EndStyledTable();
                }
            }
        }
    }
}
