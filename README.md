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
- **Statistics:** your recorded encounters grouped by race, gender, job, status,
  home world, and datacenter.
- **Duty Analysis:** run times, wipes, a day/time heatmap, and recent versus overall
  performance. Type a recent-run count, use -/+, or pick 5, 10, 25, or 50.
- **People:** players you've met, how often you've met them, and your shared duty
  history. Choose their most observed or most recently seen race in Settings.
- **Summary:** milestones, streaks, and your lifetime PvP totals and personal bests.

Statistics, Duty Analysis, Summary, and Settings show one category at a time.
Pick one on the left; in smaller windows, the category buttons move to the top.

Use the filters to narrow what's shown. Recent performance compares completed
runs entered from the start, using those same filters. Overall includes the recent
runs too, and unknown death counts are excluded from the death average.

**Settings** is where you choose what gets recorded, including Trust/Duty Support
runs, alliance members, and PvP opponents. Explorer Mode isn't recorded. You can
also choose your columns, reorder them, pick your milestones, set
up the duty timer, and change the theme or colors.

The introduction appears once. Read it again under **Settings → Help**, where
you'll also find the repository and issue-report links.

## Anonymous mode

Use **Settings → Privacy** to replace character names with Player 1/Player 2 or
job labels such as WHM 1. Labels stay consistent across views, and you can hide
home worlds beside players too. Group statistics still use the original records.

Notes and record editing are hidden while Anonymous mode is on. Your local history
and backups keep the real names. New CSV exports are anonymized automatically in
this mode; **Anonymize CSV exports** under History & exports also works on its own.

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
<img width="1178" height="531" alt="image" src="https://github.com/user-attachments/assets/7aff110a-25bf-4993-a4ed-fc3df44ae073" />

### People and filters
<img width="1170" height="753" alt="image" src="https://github.com/user-attachments/assets/a73c2580-7bb6-4565-8f53-229e392a8593" />

### Statistics
<img width="1176" height="511" alt="image" src="https://github.com/user-attachments/assets/418a15d5-152f-4424-bff0-42b009936e2d" />


### Duty Analysis
<img width="1173" height="770" alt="image" src="https://github.com/user-attachments/assets/f1ed6fe9-ee48-44c8-92cf-9ddf5f7cd3e7" />

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
