using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.DutyState;

namespace MentorRouletteTracker.DutyTracking;

public partial class DutyTrackerService
{
    private bool combatStateObserved;
    private bool waitForCombatReset;
    private DateTime? outOfCombatSince;
    private int? trailingWipePull;
    private TimeSpan? wipeElapsed;
    private static readonly TimeSpan CombatEndGrace = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan WipeDeathGrace = TimeSpan.FromSeconds(5);

    private TimeSpan DutyElapsed(DateTime now)
    {
        var elapsed = now.ToUniversalTime() - current!.Timestamp.ToUniversalTime();
        return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
    }

    private void ResetPullTracking()
    {
        combatStateObserved = false;
        waitForCombatReset = false;
        outOfCombatSince = null;
        trailingWipePull = null;
        wipeElapsed = null;
    }

    private void SavePullCheckpoint(DateTime now)
    {
        current!.CheckpointUtc = now;
        config.History.SavePending();
    }

    private void UpdatePullState()
    {
        if (current == null || current.DutyType == "PvP") return;
        var now = DateTime.UtcNow;
        var inCombat = condition[ConditionFlag.InCombat] || partyList.Any(member =>
            member.GameObject is ICharacter character && (character.StatusFlags & StatusFlags.InCombat) != 0);
        if (inCombat)
        {
            outOfCombatSince = null;
            if (!waitForCombatReset && current.ActivePull == null)
            {
                current.Pulls.Add(new DutyPull(current.Pulls.Count + 1, combatStateObserved ? DutyElapsed(now) : null));
                trailingWipePull = null;
                wipeElapsed = null;
                SavePullCheckpoint(now);
            }
        }
        else
        {
            waitForCombatReset = false;
            // A local death can briefly clear the combat flag while the fight continues.
            if (objectTable.LocalPlayer?.IsDead == true) outOfCombatSince = null;
            else if (current.ActivePull != null)
            {
                outOfCombatSince ??= now;
                if (now - outOfCombatSince.Value >= CombatEndGrace)
                {
                    current.ClosePull(DutyElapsed(outOfCombatSince.Value), DutyPullOutcome.CombatEnded);
                    SavePullCheckpoint(now);
                    outOfCombatSince = null;
                }
            }
        }
        combatStateObserved = true;
    }

    private void PrepareWipedPull()
    {
        if (current?.DutyType == "PvP" || current == null) return;
        var elapsed = DutyElapsed(DateTime.UtcNow);
        if (current.ActivePull == null && current.Pulls.LastOrDefault() is
            { Ended: { } ended, Outcome: DutyPullOutcome.CombatEnded or DutyPullOutcome.Reset } recent &&
            elapsed >= ended && elapsed - ended <= WipeDeathGrace)
            current.Pulls[^1] = recent with { Ended = null, Outcome = DutyPullOutcome.InProgress };
        if (current.ActivePull == null &&
            (wipeElapsed == null || elapsed - wipeElapsed > WipeDeathGrace))
            current.Pulls.Add(new DutyPull(current.Pulls.Count + 1, null));
    }

    private void FinishWipedPull()
    {
        if (current?.DutyType == "PvP" || current == null) return;
        var now = DateTime.UtcNow;
        trailingWipePull = current.ActivePull?.Number ?? trailingWipePull;
        wipeElapsed = DutyElapsed(now);
        current.ClosePull(wipeElapsed.Value, DutyPullOutcome.Wipe);
        waitForCombatReset = true;
        outOfCombatSince = null;
        SavePullCheckpoint(now);
    }

    private int? PullForDeath(TimeSpan elapsed)
    {
        if (current!.ActivePull is { } active)
            return active.Started > elapsed ? null : active.Number;
        if (trailingWipePull is { } number && wipeElapsed is { } wiped &&
            elapsed >= wiped && elapsed - wiped <= WipeDeathGrace && current.Pulls.LastOrDefault()?.Number == number)
        {
            var pull = current.Pulls[^1];
            if (elapsed > pull.Ended) current.Pulls[^1] = pull with { Ended = elapsed };
            return number;
        }
        return null;
    }

    private void OnDutyRecommenced(IDutyStateEventArgs args)
    {
        if (current == null || current.DutyType == "PvP") return;
        CaptureResetDeaths();
        var now = DateTime.UtcNow;
        current.ClosePull(DutyElapsed(now), DutyPullOutcome.Reset);
        waitForCombatReset = true;
        outOfCombatSince = null;
        SavePullCheckpoint(now);
    }

    private void InterruptPullAtCheckpoint()
    {
        if (current == null || current.DutyType == "PvP") return;
        var hadActivePull = current.ActivePull != null;
        current.ClosePull(DutyElapsed(current.CheckpointUtc ?? DateTime.UtcNow), DutyPullOutcome.Interrupted);
        ResetPullTracking();
        if (current.Pulls.LastOrDefault() is { Ended: { } ended, Outcome: DutyPullOutcome.Wipe or DutyPullOutcome.Reset } &&
            DutyElapsed(DateTime.UtcNow) - ended <= WipeDeathGrace)
            waitForCombatReset = true;
        if (hadActivePull) config.History.SavePending();
    }
}
