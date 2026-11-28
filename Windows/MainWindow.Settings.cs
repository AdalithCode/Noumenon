using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace MentorRouletteTracker
{
    public partial class MainWindow
    {
        private bool? settingsExpansion;
        private bool? milestoneExpansion;
        private bool openResetSettingsConfirmation;
        private bool appearanceSettingsDirty;
        private readonly Stack<Vector4> settingsSectionBounds = new();

        private void SaveAppearanceChanges()
        {
            if (!appearanceSettingsDirty)
                return;
            config.Save();
            appearanceSettingsDirty = false;
        }

        private void DrawSettingsTab()
        {
            try
            {
                if (ImGui.BeginChild("SettingsContent", Vector2.Zero, false))
                    DrawSettingsContent();
            }
            finally
            {
                ImGui.EndChild();
            }
        }

        private void DrawSettingsContent()
        {
            if (ImGui.SmallButton("Collapse all##Settings"))
            {
                settingsExpansion = false;
                milestoneExpansion = false;
                ImGui.GetStateStorage().SetInt(ImGui.GetID("Custom colors"), 0);
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Expand all##Settings"))
            {
                settingsExpansion = true;
                milestoneExpansion = true;
            }
            ImGui.Spacing();

            var contentTypes = new (uint Id, string Name)[]
            {
                (2, "Dungeons"),
                (3, "Guildhests"),
                (4, "Trials"),
                (5, "Raids"),
                (6, "PvP"),
                (21, "Deep Dungeons"),
                (28, "Ultimate Raids"),
                (30, "V&C Dungeon"),
                (37, "Chaotic Alliance Raid")
            };

            if (BeginSettingsSection("Duty recording", defaultOpen: true))
            {
                try
                {
                    var armed = tracker.Armed;

                    if (ImGui.Checkbox("Record duties", ref armed))
                    {
                        tracker.Armed = armed;
                    }

                    ImGui.PushTextWrapPos();
                    try
                    {
                        ImGui.TextDisabled(
                            "On by default and stays on until you turn it off. Your choice is saved when you reload or restart.");
                    }
                    finally
                    {
                        ImGui.PopTextWrapPos();
                    }

                    ImGui.Spacing();
                    var recordNpcDuties = config.RecordNpcDuties;
                    if (DrawSettingsCheckbox("Record Trust and Duty Support runs", ref recordNpcDuties))
                    {
                        config.RecordNpcDuties = recordNpcDuties;
                        config.Save();
                    }
                    ImGui.Spacing();
                    DrawSettingsGroupHeading("Duty types");
                    ImGui.Spacing();
                    DrawDutyTypeOptions(contentTypes, timer: false);
                    ImGui.Spacing();
                    DrawSettingsGroupHeading("PvP");
                    DrawPvpStatsStatus();
                    var enemyTrackingDisabled = BeginPvpControl();
                    try
                    {
                        var trackEnemies = config.TrackPvpEnemies;
                        if (DrawSettingsCheckbox("Track opposing members in PvP matches", ref trackEnemies))
                        {
                            config.TrackPvpEnemies = trackEnemies;
                            config.Save();
                        }
                    }
                    finally
                    {
                        EndPvpControl(enemyTrackingDisabled);
                    }
                    ImGui.TextDisabled("New matches only.");
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Player tracking", defaultOpen: true))
            {
                try
                {
                    bool includeSelf = config.IncludeSelfInStatistics;

                    if (DrawSettingsCheckbox(
                        "Include yourself in statistics",
                        ref includeSelf))
                    {
                        config.IncludeSelfInStatistics = includeSelf;
                        config.Save();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Your data is still recorded when this is off. It just won't be included in statistics.");

                    bool trackAlliance = config.TrackAllianceMembers;

                    if (ImGui.Checkbox(
                        "Track alliance members",
                        ref trackAlliance))
                    {
                        config.TrackAllianceMembers = trackAlliance;
                        config.Save();
                    }

                    ImGui.Spacing();
                    var raceDisplay = config.UseMostRecentPlayerRace ? 1 : 0;
                    ImGui.TextUnformatted("Race shown in People");
                    ImGui.SetNextItemWidth(Math.Min(ImGui.GetContentRegionAvail().X, ImGui.GetFontSize() * 18));
                    if (ImGui.Combo("##PeopleRaceDisplay", ref raceDisplay,
                            ["Most observed", "Most recently seen"], 2))
                    {
                        config.UseMostRecentPlayerRace = raceDisplay == 1;
                        config.Save();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Uses the encounters matching your filters. Unknown races are skipped. Ties for most observed use the most recent race.");
                    ImGui.Spacing();
                    DrawSettingsGroupHeading("Encounter target");
                    DrawEncounterTargetSettings();
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Duty timer"))
            {
                try
                {
                    ImGui.TextDisabled("Choose which duties show the timer.");
                    ImGui.Spacing();
                    DrawDutyTypeOptions(contentTypes, timer: true);
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Privacy"))
            {
                try { DrawPrivacySettings(); }
                finally { EndSettingsSection(); }
            }
            ImGui.Spacing();
            if (BeginSettingsSection("Appearance"))
            {
                try
                {
                    DrawAppearanceSettings();
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Table columns"))
            {
                try
                {
                    DrawTableColumnSettings();
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Milestones"))
            {
                try
                {
                    DrawMilestoneCustomization();
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("History & exports"))
            {
                try
                {
                    DrawHistorySettings();
                }
                finally
                {
                    EndSettingsSection();
                }
            }

            ImGui.Spacing();
            if (BeginSettingsSection("Help"))
            {
                try
                {
                    if (ImGui.Button("Show introduction"))
                        showIntroduction = true;
                    if (ImGui.Button("Open Noumenon repository"))
                        Dalamud.Utility.Util.OpenLink("https://github.com/AdalithCode/Noumenon");
                    if (ImGui.Button("Report an issue"))
                        Dalamud.Utility.Util.OpenLink("https://github.com/AdalithCode/Noumenon/issues");
                    ImGui.TextWrapped("Found a bug or have an idea? Post it in the repository's Issues tab. For bugs, include what happened and how to reproduce it if you can.");
                }
                finally { EndSettingsSection(); }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (ImGui.Button("Reset all settings"))
                openResetSettingsConfirmation = true;
            settingsExpansion = null;
        }

        private bool BeginPvpControl(bool isPvp = true)
        {
            var disabled = isPvp && !tracker.PvpTrackingAvailable;
            if (disabled)
                ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.6f, 0.6f, 0.6f, 1f));
            ImGui.BeginDisabled(disabled);
            return disabled;
        }

        private static void EndPvpControl(bool disabled)
        {
            ImGui.EndDisabled();
            if (disabled)
                ImGui.PopStyleColor();
        }

        private void DrawPvpStatsStatus()
        {
            var enabled = tracker.PvpTrackingAvailable;
            ImGui.PushStyleColor(ImGuiCol.ChildBg, enabled ? new Vector4(0.06f, 0.20f, 0.09f, 1f) : new Vector4(0.24f, 0.06f, 0.05f, 1f));
            try
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 4));
                try
                {
                    try
                    {
                        if (ImGui.BeginChild("PvpStatsStatus", new Vector2(0, ImGui.GetTextLineHeight() + 10), true,
                            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                            ImGui.TextColored(enabled ? new Vector4(0.45f, 1f, 0.55f, 1f) : new Vector4(1f, 0.45f, 0.40f, 1f),
                                enabled ? "PvP Tracker enabled" : "PvP Tracker unavailable");
                    }
                    finally
                    {
                        ImGui.EndChild();
                    }
                }
                finally
                {
                    ImGui.PopStyleVar();
                }
            }
            finally
            {
                ImGui.PopStyleColor();
            }
            ImGui.TextWrapped(enabled
                ? "PvP Tracker supplies each match's player list, scores, and results to Noumenon."
                : "Enable PvP Tracker in /xlplugins to record PvP matches. Its repository has installation instructions.");
            if (ImGui.Button("Open PvP Tracker repository", new Vector2(
                    Math.Min(ImGui.GetContentRegionAvail().X, ImGui.GetFontSize() * 24), ImGui.GetFrameHeight() * 1.4f)))
                Dalamud.Utility.Util.OpenLink("https://github.com/wrath16/PvpStats");
            ImGui.Spacing();
        }

        private void DrawResetSettingsConfirmation()
        {
            const string title = "Reset all settings##ResetAllSettings";
            if (openResetSettingsConfirmation)
            {
                ImGui.OpenPopup(title);
                openResetSettingsConfirmation = false;
            }
            if (!ImGui.IsPopupOpen(title))
                return;
            const string message = "Reset settings, colors, columns, and filters to their defaults? Duty records and notes stay saved.";
            var style = ImGui.GetStyle();
            var width = ImGui.GetFontSize() * 28;
            var textHeight = ImGui.CalcTextSize(message, false, width - style.WindowPadding.X * 2).Y;
            var height = textHeight + style.WindowPadding.Y * 2 + style.ItemSpacing.Y * 3 +
                ImGui.GetFrameHeight() + ImGui.GetFrameHeightWithSpacing();
            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            ImGui.SetNextWindowPos(ImGui.GetWindowPos() + ImGui.GetWindowSize() * 0.5f,
                ImGuiCond.Always, new Vector2(0.5f));
            if (!ImGui.BeginPopupModal(title, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings))
                return;
            try
            {
                ImGui.TextWrapped(message);
                ImGui.Spacing();
                if (ImGui.Button("Reset settings"))
                {
                    ResetAllSettings();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##ResetSettings"))
                    ImGui.CloseCurrentPopup();
            }
            finally
            {
                ImGui.EndPopup();
            }
        }

        private void ResetAllSettings()
        {
            config.ResetPreferences();
            ClearFilters();
            filtersExpanded = false;
            selectedHeatmapMetric = 0;
            settingsExpansion = false;
            milestoneExpansion = false;
        }

        private void DrawHistorySettings()
        {
            UpdateCsvExport();
            UpdateHistoryBackup();
            ImGui.TextWrapped("Back up your history, export it to CSV, or clear saved records. Filters don't limit these actions.");
            ImGui.Spacing();
            ImGui.BeginDisabled(historyBackupTask != null);
            try
            {
                if (ImGui.Button("Create backup"))
                    StartHistoryBackup();
            }
            finally
            {
                ImGui.EndDisabled();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Saves duty history, player details, notes, and your current duty. Settings and CSV files aren't included.");
            ImGui.SameLine();
            if (ImGui.Button("Open backups folder"))
                OpenHistoryBackupsFolder();
            ImGui.Spacing();
            ImGui.BeginDisabled(historyBackupTask != null || !tracker.CanRestoreHistory);
            try
            {
                if (ImGui.Button("Restore backup..."))
                    OpenHistoryRestore();
            }
            finally { ImGui.EndDisabled(); }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Restore outside a duty. Your current history is backed up first. Settings stay as they are.");
            DrawHistoryRestore();
            if (!string.IsNullOrEmpty(lastBackupMessage))
            {
                ImGui.PushTextWrapPos();
                try
                {
                    ImGui.TextColored(backupFailed ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.4f, 1f, 0.4f, 1f),
                        lastBackupMessage);
                }
                finally
                {
                    ImGui.PopTextWrapPos();
                }
            }
            ImGui.Spacing();
            var anonymousCsv = config.AnonymousMode || config.AnonymizeCsvExports;
            ImGui.BeginDisabled(config.AnonymousMode);
            try
            {
                if (ImGui.Checkbox("Anonymize CSV exports", ref anonymousCsv))
                {
                    config.AnonymizeCsvExports = anonymousCsv;
                    config.Save();
                }
            }
            finally { ImGui.EndDisabled(); }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Replaces player names in new CSV files. Anonymous mode always anonymizes exports. Existing files and backups keep their original data.");
            ImGui.BeginDisabled(csvExportTask != null);
            try
            {
                if (ImGui.Button("Export CSV"))
                    StartCsvExport();
            }
            finally
            {
                ImGui.EndDisabled();
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear all records"))
            {
                ImGui.OpenPopup("ConfirmClear");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Clears saved history, even records hidden by filters. Your current duty, settings, exports, and backups stay.");

            if (ImGui.BeginPopup("ConfirmClear"))
            {
                try
                {
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 26);
                    try
                    {
                        ImGui.TextUnformatted("Clear all saved duty records?");
                        ImGui.TextWrapped("This removes saved duties, player details, and notes from your history. You can't undo it here.");
                        ImGui.Spacing();
                        ImGui.TextWrapped("Your current duty and settings stay. CSV exports, backups, and older recovery copies aren't deleted.");
                    }
                    finally
                    {
                        ImGui.PopTextWrapPos();
                    }

                    ImGui.Spacing();

                    if (ImGui.Button("Yes, clear"))
                    {
                        config.History.Records.Clear();
                        config.History.Save();

                        selectedRecord = null;
                        showDetailWindow = false;

                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                finally
                {
                    ImGui.EndPopup();
                }
            }

            if (!string.IsNullOrEmpty(lastExportMessage))
            {
                ImGui.Spacing();

                ImGui.TextColored(
                    exportFailed ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.4f, 1f, 0.4f, 1f),
                    lastExportMessage);
            }

            ImGui.Spacing();
        }

        private bool BeginSettingsSection(string title, bool defaultOpen = false)
        {
            if (settingsExpansion.HasValue)
                ImGui.SetNextItemOpen(settingsExpansion.Value, ImGuiCond.Always);
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            if (!DrawDropdownHeader(title, flags)) return false;
            BeginSettingsOutline(indent: true);
            return true;
        }

        private static bool DrawDropdownHeader(string title, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            var colors = ImGui.GetStyle().Colors;
            ImGui.PushStyleColor(ImGuiCol.Header, Vector4.Lerp(colors[(int)ImGuiCol.Header], colors[(int)ImGuiCol.HeaderHovered], 0.5f));
            try { return ImGui.CollapsingHeader(title, flags); }
            finally
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);
            }
        }

        private void EndSettingsSection()
        {
            EndSettingsOutline();
            ImGui.Spacing();
            ImGui.Unindent(12);
        }

        private void BeginSettingsOutline(bool indent = false)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            settingsSectionBounds.Push(new Vector4(min.X, min.Y, max.X, max.Y));
            if (indent)
            {
                ImGui.Indent(12);
                ImGui.Spacing();
            }
            ImGui.BeginGroup();
        }

        private void EndSettingsOutline()
        {
            ImGui.Dummy(new Vector2(0, 4));
            ImGui.EndGroup();
            var bounds = settingsSectionBounds.Pop();
            ImGui.GetWindowDrawList().AddRect(new Vector2(bounds.X, bounds.Y),
                new Vector2(bounds.Z, ImGui.GetItemRectMax().Y), ImGui.GetColorU32(ImGuiCol.Separator), 4f);
        }

        private static void DrawSettingsGroupHeading(string title)
        {
            DrawSectionHeading(title);
            ImGui.Separator();
            ImGui.Spacing();
        }

        private static int SettingsChoiceColumns(IEnumerable<string> labels)
        {
            var minimumWidth = labels.Max(label => ImGui.CalcTextSize(label).X) +
                ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X + 16;
            return Math.Clamp((int)(ImGui.GetContentRegionAvail().X / Math.Max(1, minimumWidth)), 1, 3);
        }

        private static bool DrawSettingsCheckbox(string label, ref bool value)
        {
            var style = ImGui.GetStyle();
            if (ImGui.CalcTextSize(label).X + ImGui.GetFrameHeight() + style.ItemInnerSpacing.X <= ImGui.GetContentRegionAvail().X)
                return ImGui.Checkbox(label, ref value);
            ImGui.BeginGroup();
            try
            {
                var changed = ImGui.Checkbox("##" + label, ref value);
                ImGui.SameLine(0, style.ItemInnerSpacing.X);
                ImGui.PushTextWrapPos();
                try { ImGui.TextUnformatted(label); }
                finally { ImGui.PopTextWrapPos(); }
                if (ImGui.IsItemClicked())
                {
                    value = !value;
                    changed = true;
                }
                return changed;
            }
            finally { ImGui.EndGroup(); }
        }

        private void DrawDutyTypeOptions((uint Id, string Name)[] contentTypes, bool timer)
        {
            var minimumWidth = contentTypes.Max(type => ImGui.CalcTextSize(type.Name).X) +
                ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X + 16;
            var columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / Math.Max(1, minimumWidth)), 1, 3);
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 4));
            try
            {
                if (!ImGui.BeginTable(timer ? "TimerDutyTypes" : "RecordingDutyTypes", columns, ImGuiTableFlags.SizingStretchSame)) return;
                try
                {
                    foreach (var contentType in contentTypes.OrderBy(type => Array.IndexOf(OrderedDutyTypes, type.Name)))
                    {
                        ImGui.TableNextColumn();
                        var disabled = BeginPvpControl(contentType.Id == 6);
                        try
                        {
                            var selected = timer ? config.TimerVisibleContentTypeIds : config.TrackedContentTypeIds;
                            var enabled = selected.Contains(contentType.Id);
                            if (ImGui.Checkbox($"{contentType.Name}##{(timer ? "TimerVisibility" : "DutyTracking")}", ref enabled))
                            {
                                if (enabled) selected.Add(contentType.Id);
                                else selected.Remove(contentType.Id);
                                config.Save();
                            }
                        }
                        finally { EndPvpControl(disabled); }
                    }
                }
                finally { ImGui.EndTable(); }
            }
            finally { ImGui.PopStyleVar(); }
        }

        private void DrawAppearanceSettings()
        {
            DrawThemeSettings();
            if (settingsExpansion.HasValue)
                ImGui.SetNextItemOpen(settingsExpansion.Value, ImGuiCond.Always);
            if (!ImGui.TreeNodeEx("Custom colors", ImGuiTreeNodeFlags.SpanAvailWidth)) return;
            BeginSettingsOutline();
            try { DrawCustomColorSettings(); }
            finally
            {
                EndSettingsOutline();
                ImGui.TreePop();
                ImGui.Spacing();
            }
        }

        private void DrawCustomColorSettings()
        {
            ImGui.PushTextWrapPos();
            try
            {
                ImGui.TextDisabled("Your theme and colors are saved just for you. Use Opacity to control transparency. Reset colors returns to your Dalamud theme.");
            }
            finally
            {
                ImGui.PopTextWrapPos();
            }
            ImGui.Spacing();

            ImGui.BeginDisabled(WindowAppearance.SelectedTheme(config) != "Custom");
            try
            {
                DrawBackgroundColor("Window background", config.WindowBackgroundColor,
                    ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.WindowBg)), value => config.WindowBackgroundColor = value);
                DrawBackgroundColor("Panel background", config.PanelBackgroundColor,
                    ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.ChildBg)), value => config.PanelBackgroundColor = value);
                DrawBackgroundColor("Table headers", config.TableHeaderBackgroundColor,
                    WindowAppearance.DefaultTableHeader, value => config.TableHeaderBackgroundColor = value);
                DrawBackgroundColor("Table rows", config.TableRowBackgroundColor,
                    ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TableRowBg)), value => config.TableRowBackgroundColor = value);
                DrawBackgroundColor("Alternate table rows", config.AlternateRowBackgroundColor,
                    WindowAppearance.DefaultAlternateRow, value => config.AlternateRowBackgroundColor = value);

            }
            finally { ImGui.EndDisabled(); }

            if (ImGui.Button("Reset colors"))
            {
                config.AppearanceTheme = "Dalamud";
                config.WindowBackgroundColor = null;
                config.PanelBackgroundColor = null;
                config.TableHeaderBackgroundColor = null;
                config.TableRowBackgroundColor = null;
                config.AlternateRowBackgroundColor = null;
                config.TabColor = null;
                config.SelectedTabColor = null;
                config.HoveredTabColor = null;
                config.ButtonColor = null;
                config.HoveredButtonColor = null;
                config.PressedButtonColor = null;
                config.ControlBackgroundColor = null;
                config.HoveredControlColor = null;
                config.ActiveControlColor = null;
                config.AccentColor = null;
                appearanceSettingsDirty = true;
                SaveAppearanceChanges();
            }
            ImGui.Spacing();
            ImGui.TextDisabled("Table preview");
            if (BeginStyledTable("AppearancePreview", 2,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                try
                {
                    ImGui.TableSetupColumn("Row");
                    ImGui.TableSetupColumn("Example");
                    ImGui.TableHeadersRow();
                    for (var i = 0; i < 3; i++)
                    {
                        ImGui.TableNextRow();
                        DrawTableCell(0, $"Row {i + 1}");
                        DrawTableCell(1, i % 2 == 0 ? "Table row color" : "Alternate row color");
                    }
                }
                finally
                {
                    EndStyledTable();
                }
            }
            ImGui.Spacing();
            var wrap = config.WrapTableHeaders;
            if (ImGui.Checkbox("Wrap headers in very narrow columns", ref wrap))
            {
                config.WrapTableHeaders = wrap;
                config.Save();
            }
        }

        private void DrawBackgroundColor(string label, uint? saved, Vector4 fallback, Action<uint?> update)
        {
            ImGui.PushID(label);
            try
            {
                ImGui.TextUnformatted(label);
                var color = saved.HasValue ? ImGui.ColorConvertU32ToFloat4(saved.Value) : fallback;
                var customized = saved.HasValue;
                var resetWidth = ImGui.CalcTextSize("Default").X + ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ItemSpacing.X;
                ImGui.SetNextItemWidth(Math.Max(1, Math.Min(280, ImGui.GetContentRegionAvail().X - resetWidth)));
                if (ImGui.ColorEdit4("##Color", ref color, ImGuiColorEditFlags.NoAlpha))
                {
                    if (!customized)
                        color.W = 1f;
                    update(ImGui.ColorConvertFloat4ToU32(color));
                    appearanceSettingsDirty = true;
                    customized = true;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                    SaveAppearanceChanges();
                ImGui.SameLine();
                if (customized)
                {
                    if (ImGui.SmallButton("Reset"))
                    {
                        update(null);
                        appearanceSettingsDirty = true;
                        SaveAppearanceChanges();
                    }
                }
                else
                {
                    ImGui.TextDisabled("Default");
                }
                var opacity = color.W * 100f;
                var opacityLabelWidth = ImGui.CalcTextSize("Opacity").X + ImGui.GetStyle().ItemSpacing.X;
                ImGui.SetNextItemWidth(Math.Max(1, Math.Min(280, ImGui.GetContentRegionAvail().X - opacityLabelWidth)));
                if (ImGui.SliderFloat("Opacity", ref opacity, 0, 100, "%.0f%%"))
                {
                    color.W = opacity / 100f;
                    update(ImGui.ColorConvertFloat4ToU32(color));
                    appearanceSettingsDirty = true;
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                    SaveAppearanceChanges();
            }
            finally
            {
                ImGui.PopID();
            }
            ImGui.Spacing();
        }

        private void DrawMilestoneCustomization()
        {
            config.HiddenMilestones ??= new();
            RefreshMilestonesIfNeeded(objectTable.LocalPlayer?.Name.TextValue);

            var changed = false;
            if (ImGui.Button("Show all milestones") && config.HiddenMilestones.Count > 0)
            {
                config.HiddenMilestones.Clear();
                changed = true;
            }
            ImGui.Separator();

            if (milestoneSections.Count == 0)
                ImGui.TextWrapped("Complete a duty or clear the duty filters to see the milestone options.");
            foreach (var section in milestoneSections)
            {
                ImGui.PushID(section.Title);
                try
                {
                    if (milestoneExpansion.HasValue)
                        ImGui.SetNextItemOpen(milestoneExpansion.Value, ImGuiCond.Always);
                    if (ImGui.TreeNodeEx(MilestoneTitle(section.Title), ImGuiTreeNodeFlags.SpanAvailWidth))
                    {
                        BeginSettingsOutline();
                        try
                        {
                            foreach (var row in section.Rows)
                            {
                                var key = MilestoneKey(section.Title, row.Label);
                                var visible = !config.HiddenMilestones.Contains(key);
                                var toggled = false;
                                var pvpDisabled = BeginPvpControl(section.Title is "PvP player stats" or "Your PvP results" || row.Label.Contains("(PvP)", StringComparison.Ordinal));
                                try
                                {
                                    toggled = ImGui.Checkbox($"{EncounterMilestoneLabel(row.Label)}##{row.Label}", ref visible);
                                }
                                finally
                                {
                                    EndPvpControl(pvpDisabled);
                                }
                                if (!toggled)
                                    continue;
                                if (visible)
                                    config.HiddenMilestones.Remove(key);
                                else
                                    config.HiddenMilestones.Add(key);
                                changed = true;
                            }
                        }
                        finally
                        {
                            EndSettingsOutline();
                            ImGui.TreePop();
                            ImGui.Spacing();
                        }
                    }
                }
                finally
                {
                    ImGui.PopID();
                }
            }
            milestoneExpansion = null;

            if (changed)
                config.Save();
        }

    }
}
