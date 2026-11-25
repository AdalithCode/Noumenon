# Noumenon

A duty tracker for FFXIV. Keep your past runs, see who you've played with, and look
through your stats.

Still a beta I'm sharing with friends, so expect bugs. If something looks wrong,
let me know what happened.

## Install

1. Open `/xlsettings` and go to **Experimental**.
2. Add this under **Custom Plugin Repositories**:

   ```text
   https://raw.githubusercontent.com/AdalithCode/Noumenon/main/repo.json
   ```

3. Save, open `/xlplugins`, and search for **Noumenon** to install it.

## Using it

Open with `/noumenon` or `/nom`. Use `/lastduty` to open your latest saved duty.

Recording is on by default and stays on unless you turn it off in Settings.
Your choice is saved between reloads and restarts. You can also use `/nom disarm`
and `/nom arm`.

- **Records:** past runs, players, deaths, wipes, notes, and PvE death timestamps
  grouped by pull. Click a duty for details; right-click to edit or delete it.
- **People:** players you've met, how often you've met them, and your shared duty
  history. Choose their most observed or most recently seen race in Settings.
- **Statistics:** your recorded encounters grouped by race, gender, job, status,
  home world, and datacenter.
- **Duty Analysis:** run times, wipes, a day/time heatmap, and recent versus overall
  performance. Type a recent-run count, use -/+, or pick 5, 10, 25, or 50.
- **Summary:** milestones, streaks, and your lifetime PvP totals and personal bests.

Use the filters to narrow what's shown. Recent performance compares completed
runs entered from the start, using those same filters. Overall includes the recent
runs too, and unknown death counts are excluded from the death average.

**Settings** is where you choose what gets recorded, including Trust/Duty Support
runs, alliance members, and PvP opponents. Explorer Mode isn't recorded. You can
also choose your columns, reorder them, pick your statistics and milestones, set
up the duty timer, and change the theme or colors.

The introduction appears once. Read it again under **Settings → Help**, where
you'll also find the repository and issue-report links.

## PvP

Install and enable [PvP Tracker](https://github.com/wrath16/PvpStats) for PvP
recording. Without it, PvP recording stays disabled. You don't need to untick
anything, and PvE still works.

Frontline, Crystalline Conflict, and Rival Wings are supported. Player details
show teammates and opponents, scores, and results, including 1st/2nd/3rd in
Frontline. Choose the PvP columns you want in Settings, including damage, kills,
assists, deaths, damage taken, healing, and per-minute values.

Results can take a few minutes to appear. If a record is marked with a red **[!]**,
open its details or right-click it and choose **Fix missing data**. You can correct
the recording character, player list, and missing PvP information. Leave scores
you don't know blank.

## Screenshots

### Records and duty details

> Screenshot goes here: a few saved duties with one PvE duty open, showing the
> party, death counts, notes, and an expanded pull in the death timeline.

### People and filters

> Screenshot goes here: the People tab with the job/world filters open and a
> player's duty history alongside it. Use a player you've encountered a few times.

### Statistics

> Screenshot goes here: race or home-world statistics with a few useful columns
> enabled. Keep the selection readable rather than showing every possible column.

### Duty Analysis

> Screenshot goes here: Duties by day and time with Recent performance below it.
> Use more than 10 matching full runs so the recent and overall values can differ.

### PvP records

> Screenshot goes here: a completed Frontline record showing side, damage, kills,
> deaths, assists, and placement. Include damage taken/healing if they fit cleanly.

## History and updates

History is stored locally. Backups, restore, CSV export, and **Clear all records**
are under **Settings → History & exports**. Clearing records keeps your current
duty, settings, CSV exports, and older backup/recovery copies.

Update through `/xlplugins`. If an update gets a file-in-use error, disable
Noumenon, restart the game, update it, then enable it again.

## Known issues and limits

- **English only.** Use the English game client. Other languages aren't supported.
- **NA only.** World and datacenter grouping is built around Aether, Crystal,
  Dynamis, and Primal. Other regions aren't supported.
- **Death counts can miss things.** Tracking depends on what your client sees.
  Deaths outside object range may be missed; that behavior still needs live testing.
- **PvP scoreboards can be partial.** Available stats are kept, but some players or
  scores may stay unknown after automatic lookups finish.
- **PvP edits can be updated later.** PvP Tracker may update manually edited values
  if more results arrive.

Actual crash/disconnect recovery still needs live testing. The automated recovery
checks passed, but that isn't the same as testing a real game crash or disconnect.

Found something else? [Report it in Issues](https://github.com/AdalithCode/Noumenon/issues)
with what happened and how to reproduce it if you can.
