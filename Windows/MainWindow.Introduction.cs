using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace MentorRouletteTracker;

public partial class MainWindow
{
    private bool showIntroduction;

    private void FinishIntroduction(bool openSettingsAfter = false)
    {
        config.HasSeenIntroduction = true;
        config.Save();
        showIntroduction = false;
        if (openSettingsAfter) OpenSettings();
    }

    private void DrawIntroduction()
    {
        try
        {
            if (!ImGui.BeginChild("Introduction", Vector2.Zero, false)) return;
            DrawSectionHeading("Welcome to Noumenon");
            ImGui.Spacing();
            ImGui.TextWrapped("Noumenon keeps a history of the duties you record and the players you meet in them.");
            ImGui.Spacing();
            DrawSectionHeading("Recording");
            ImGui.TextWrapped("Recording is on by default and stays on unless you turn it off in Settings.");
            if (!config.RecordDuties)
                ImGui.TextWrapped("You currently have recording turned off.");
            ImGui.TextWrapped("In Settings, choose which duty types to record, whether to record Trust and Duty Support runs, and whether to track alliance members or opposing PvP players.");
            ImGui.Spacing();
            ImGui.Separator();
            DrawSectionHeading("Your tabs");
            ImGui.Spacing();
            ImGui.TextWrapped("Records: Open a saved duty to see its players, deaths, wipes, and any notes you've added. Right-click a duty to edit it, fix missing data, or delete it.");
            ImGui.Spacing();
            ImGui.TextWrapped("Statistics: See how your recorded encounters break down by race, gender, job, status, home world, and datacenter.");
            ImGui.Spacing();
            ImGui.TextWrapped("Duty Analysis: Look at your run times and wipes for each duty, plus a heatmap of when you've run duties.");
            ImGui.Spacing();
            ImGui.TextWrapped("People: See the players in your recorded history, how often you've met them, and which duties you ran together.");
            ImGui.Spacing();
            ImGui.TextWrapped("Summary: Look through your milestones and streaks, along with your personal PvP totals and best matches.");
            ImGui.Spacing();
            ImGui.TextWrapped("Use the filters to narrow what's shown in a tab. Your recording choices are set separately in Settings.");
            ImGui.Spacing();
            ImGui.TextWrapped("Settings also lets you choose the columns and milestones you see. You can include yourself in player statistics, customize the duty timer and theme, create backups, and export your history to CSV.");
            ImGui.Spacing();
            ImGui.Separator();
            DrawSectionHeading("PvP matches");
            ImGui.Spacing();
            DrawPvpStatsStatus();
            ImGui.TextWrapped("PvE recording works without PvP Tracker. PvP recording stays off until that plugin is enabled.");
            ImGui.Spacing();
            ImGui.TextWrapped("Use /noumenon or /nom to open this window. /lastduty opens your latest recorded duty.");
            ImGui.Spacing();
            if (ImGui.Button("Got it")) FinishIntroduction();
            ImGui.SameLine();
            if (ImGui.Button("Open settings")) FinishIntroduction(openSettingsAfter: true);
        }
        finally { ImGui.EndChild(); }
    }
}
