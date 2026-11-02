# Desi Merge: Records Screen (Achievements and Score Tabs)

`RecordsPanel` replaces the old Awards panel on the main menu (the "RECORDS" tile) and is also in the in-game menu. It is a modal with a navy header "RECORDS", a close button, two tabs, scrollable content, and the usual scale/fade transition.

## Achievements tab (real data from `IAchievementService`)

One card per configured achievement (11 shipped):
- **Icon by type:** games → play, merges → grid, best score → crown, streak → calendar, "discover X" → **that item's own art**. It is dimmed while locked.
- **Title, description, progress bar and "current / target"** come from the service's own `ProgressOf` and `TargetOf`.
- **Locked or unlocked:** unlocked cards show the teal check and a mint background.
- **Reward:** "Hammer ×2" while locked, "Received" once unlocked. Achievement rewards are applied automatically at unlock (Phase 6.6), so there is no claim button. The "claimed" state is the unlocked state.
- **Summary:** "n / 11 unlocked".

## Score tab (local records, this phone only)

There is **no online leaderboard and no ranking against other players**; the tab says "Personal records on this phone".
- **Personal best** (saved best score), **games**, **merges**, **day streak (best)**: existing `IPlayerProgressService` statistics.
- **Biggest item:** art and name of the highest item ever made (`HighestItemId`).
- **Rank line:** current score rank and the next one, from `IMilestoneService` (the real rank system).
- **Recent games:** the last 10 finished games, newest first, with "Today", "Yesterday" or "n days ago" and a **NEW BEST** badge where it applied.

### Score history (new, small, versioned)

- `player_progress.json` schema **3** adds `recentGames: [{score, day, newBest}]`, capped at 10.
- Recorded in `SubmitFinalScore`, the same call that already saves the best score. A revived game updates its own entry instead of adding one.
- Older saves (v2) load with an empty history; best score, statistics and collection are untouched (tested).
- The UI only displays the list; it never calculates scores.

## Updates

The panel listens to the progress service's `Changed` event and the achievements' `Unlocked` event, and refreshes when opened. Data stays consistent after restarts, game over, unlocks and app restarts, because it is always read from the services.
