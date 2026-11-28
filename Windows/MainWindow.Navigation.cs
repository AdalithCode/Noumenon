using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private sealed record DashboardCard(string Label, string Value, string? Tooltip = null);

    private string DrawCategoryPage(string id, string[] categories, string? selected, Action<string> draw,
        Func<string, string>? label = null, float bottomSpace = 0)
    {
        if (categories.Length == 0) return string.Empty;
        if (!categories.Contains(selected)) selected = categories[0];
        ImGui.PushID(id);
        try
        {
            if (ImGui.GetContentRegionAvail().X >= ImGui.GetFontSize() * 45)
            {
                var height = Math.Max(1, ImGui.GetContentRegionAvail().Y - bottomSpace);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
                try
                {
                    if (ImGui.BeginChild("Categories", new Vector2(ImGui.GetFontSize() * 12, height), true))
                    {
                        ImGui.TextDisabled("Categories");
                        ImGui.Spacing();
                        selected = DrawCategoryChoices(categories, selected!, compact: false, label);
                    }
                }
                finally
                {
                    ImGui.EndChild();
                    ImGui.PopStyleVar(2);
                }
                ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
                try
                {
                    if (ImGui.BeginChild("CategoryContent", new Vector2(0, height), false)) draw(selected!);
                }
                finally { ImGui.EndChild(); }
            }
            else
            {
                selected = DrawCategoryChoices(categories, selected!, compact: true, label);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                draw(selected);
            }
        }
        finally { ImGui.PopID(); }
        return selected!;
    }

    private static string CategoryDisplayLabel(string text) => text switch
    {
        "Home world" => "Home World",
        "Duty types" => "Duty Types",
        "By duty" => "By Duty",
        "Duty recording" => "Duty Recording",
        "Player tracking" => "Player Tracking",
        "Duty timer" => "Duty Timer",
        "Table columns" => "Table Columns",
        "History & exports" => "History & Exports",
        "PvP results" => "PvP Results",
        "Duty records" => "Duty Records",
        "Player records" => "Player Records",
        "Streaks & droughts" => "Streaks & Droughts",
        "Activity records" => "Activity Records",
        _ => text
    };

    private static string DrawCategoryChoices(string[] categories, string selected, bool compact,
        Func<string, string>? label = null)
    {
        var columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (ImGui.GetFontSize() * 13)), 1, 3);
        if (compact && !ImGui.BeginTable("CategoryButtons", columns, ImGuiTableFlags.SizingStretchSame)) return selected;
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.06f, 0.5f));
        try
        {
            foreach (var category in categories)
            {
                if (compact) ImGui.TableNextColumn();
                var text = CategoryDisplayLabel(label?.Invoke(category) ?? category);
                var width = ImGui.GetContentRegionAvail().X;
                if (ImGui.Selectable($"{text}###Category{category}", selected == category,
                        ImGuiSelectableFlags.None, new Vector2(0, ImGui.GetFrameHeight() + 4)))
                    selected = category;
                if (selected == category)
                {
                    var start = ImGui.GetItemRectMin();
                    var end = ImGui.GetItemRectMax();
                    ImGui.GetWindowDrawList().AddRectFilled(
                        new Vector2(start.X, start.Y + 4), new Vector2(start.X + 3, end.Y - 4),
                        ImGui.ColorConvertFloat4ToU32(WindowAppearance.Accent), 1f);
                }
                if (ImGui.IsItemHovered() && ImGui.CalcTextSize(text).X > width * 0.88f)
                    ImGui.SetTooltip(text);
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            if (compact) ImGui.EndTable();
        }
        return selected;
    }

    private bool DrawDashboardCards(string id, IReadOnlyList<DashboardCard> cards)
    {
        if (cards.Count == 0) return false;
        var columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (ImGui.GetFontSize() * 12)), 1, cards.Count);
        var height = ImGui.GetTextLineHeightWithSpacing() * 3.5f + 16;
        ImGui.PushID(id);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 8));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
        try
        {
            if (!ImGui.BeginTable("OverviewCards", columns, ImGuiTableFlags.SizingStretchSame)) return false;
            try
            {
                foreach (var card in cards)
                {
                    ImGui.TableNextColumn();
                    try
                    {
                        if (ImGui.BeginChild("Card##" + card.Label, new Vector2(0, height), true,
                                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        {
                            ImGui.TextDisabled(card.Label);
                            ImGui.Spacing();
                            using (dashboardFont?.Push())
                                DrawDashboardText(card.Value);
                        }
                    }
                    finally { ImGui.EndChild(); }
                    if (card.Tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(card.Tooltip);
                }
            }
            finally { ImGui.EndTable(); }
        }
        finally
        {
            ImGui.PopStyleVar(3);
            ImGui.PopID();
        }
        return true;
    }

    private static void DrawDashboardText(string text)
    {
        ImGui.PushTextWrapPos();
        try { ImGui.TextUnformatted(text); }
        finally { ImGui.PopTextWrapPos(); }
    }
}
