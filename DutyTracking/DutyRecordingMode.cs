using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace MentorRouletteTracker.DutyTracking;

public enum DutyRecordingMode
{
    Unavailable,
    Standard,
    Explorer,
    NpcParty
}

internal static class DutyRecordingModeReader
{
    internal static unsafe DutyRecordingMode Read(uint territory)
    {
        var game = GameMain.Instance();
        if (game == null || game->CurrentTerritoryTypeId != territory || game->TerritoryLoadState != 2)
            return DutyRecordingMode.Unavailable;
        var events = EventFramework.Instance();
        var director = events == null ? null : events->GetContentDirector();
        if (director == null)
            return DutyRecordingMode.Unavailable;
        if ((director->ContentFlags & 1) != 0)
            return DutyRecordingMode.Explorer;
        var ui = UIState.Instance();
        if (ui == null)
            return DutyRecordingMode.Unavailable;
        return ui->Buddy.DutyHelperInfo.HasHelpers ? DutyRecordingMode.NpcParty : DutyRecordingMode.Standard;
    }
}
