# Desi Merge — UI, Navigation and Screen Flow Audit

Static inspection (2026-11-03) of `UI/*.cs`, `Gameplay/GameplayLoop.cs`, the scene builders in
`Scripts/Editor/Content/` (they generate every scene and prefab), and the Android back paths. Nothing was clicked at runtime for
this audit.

## 1. Navigation graph

```
[Splash (Bootstrap)] --2 s intro + async load--> [Main menu]
[Main menu]
  ├─ PLAY ───────────────────────────────> [Gameplay]
  ├─ Profile card ──> [Ranks] ──X/back/outside──> menu
  ├─ Diamonds box (display only)
  ├─ Settings ──> [Settings] (language dropdown inside) ──DONE/back/outside──> menu
  ├─ Daily Rewards ──> [Daily Rewards] ──claim──> [Reward popup] ──collect──> Daily Rewards
  ├─ Missions ──> [Missions] (Daily / Weekly / Completed tabs, Today's challenge) ──claim──> [Reward popup]
  ├─ Collection ──> [Collection] (skin pages ◀ ▶)
  ├─ Records ──> [Records] (Achievements / Score tabs)
  └─ Android back on the bare menu ──> Application.Quit (DM-14)
[Gameplay]
  ├─ ☰ menu (pauses) ──> Resume | Records | Collection | Skins | Daily Rewards | Missions | Settings | Help (tutorial) | Home
  ├─ Skin tag (pauses) ──> [Skins] only ──close──> resume
  ├─ Diamonds "+" (pauses) ──> [Daily Rewards] only ──close──> resume
  ├─ Hammer ──> focus shade (X / back / tap Hammer again cancels) ──tap item──> smash
  ├─ Shuffle ──> immediate
  ├─ Android back ──> cancel Hammer, else [Pause card] (Resume | Restart → confirm | Main menu)
  └─ danger line ──> [Game over] (Play again | Main menu | Continue — hidden in release, no ad provider)
```

## 2. Per-screen review

| Screen | Entry | Exit | State / persistence deps | Invalid states / risks found |
|---|---|---|---|---|
| Splash | Bootstrap scene | Intro done **and** load done | none | none (`SplashScreen` callback fires once, `_introDone` latch) |
| Main menu | Splash, Gameplay "Main menu" | Play, back→quit | Progress, DailyRewards, Missions (dots), Cosmetics | DM-14 (no quit confirmation) |
| Settings | Menu gear, game ☰ | DONE, back, outside tap | SettingsService (saved on close and on language change) | dropdown overlay covers the header while open (by design); taps > 400 px outside the card close Settings while the list is open (harmless) |
| Daily Rewards | Menu tile, game "+", game ☰ | Close/back/outside | DailyRewardService, countdown `Update` only while open | DM-02 (clock) |
| Missions | Menu tile, game ☰ | Close/back/outside | MissionService | DM-03, DM-04 |
| Collection / Records / Ranks / Skins | Menu / ☰ / Skin tag | Close/back/outside | Progress, Achievements, Milestones, Cosmetics | Skins purchase DM-06 |
| Pause card | Android back during play | Resume, Restart (confirm), Main menu, outside tap = Resume | GameState Paused, timeScale 0 | outside-tap resume is intentional (user request 2026-11-02) |
| Game over | Danger line | Play again, Main menu | Progress flushed in `EndGame` | Outside tap ignored (no close button name) — correct |
| Hammer focus | Hammer button | X, back, Hammer again, successful smash, pause, game over | PowerUpController.TargetingId | none found; bands block HUD taps; X is a pointer blocker |
| Tutorial | First game | Skip / last page | Settings.TutorialCompleted (flushed) | blocks drops while visible (`BlocksPointer`) |

## 3. Cross-cutting checks

| Concern | Result |
|---|---|
| Missing button handlers | Every builder-created button is wired in its panel's `Awake`/`Bind`; HUD uses null-safe `AddListener`. The removed pause button is passed as `null` and `GameplayHud` tolerates it. |
| Duplicate handlers | `SettingsPanel.WireLanguagePicker` is guarded by `_languageWired`; `CollectionPanel` removes before adding. Other panels add listeners once in `Awake`/`Bind`. |
| Modal stacking | Menu screens open one at a time (`MenuPanel`/`GameplayMenu.OpenOnly`); reward popup sits above its panel and is closed first by back. |
| Input leaking to the playfield | All in-game screens open through `MenuRequested` → pause → `InputEnabled = false`. |
| Outside-tap close | `OutsideTapClose` presses the modal's own CloseButton/BackButton/DoneButton/ResumeButton, so saving/resume logic is reused; Game over and reward popup have none of those and ignore outside taps. |
| Safe area | `SafeAreaFitter` on every canvas; notch screenshots existed in earlier test runs (not re-run for this audit). |

## 4. Findings

* **DM-14 (INFORMATIONAL, CODE-VERIFIED):** `MainMenuEntryPoint.OnBackPressed` (230–236) calls `Application.Quit()` on the
  first back press with no panel open. Progress is saved on quit, so no data loss; players may exit by accident.
* **DM-13 (MEDIUM, CONFIRMED):** the UI changes of 2026-11-02/03 (localization, HUD redesign, glass pass, focus shade,
  dropdown, outside-tap close, splash) were built and installed but **the EditMode/PlayMode suites were not run afterwards**,
  so screen behaviour is unverified by automation. See FinalWeaknessReport.md §17.
* **DM-22 (INFORMATIONAL, UNVERIFIED):** Chinese text uses an OS font chosen by name (`Loc.FontForLanguage`,
  `Font.CreateDynamicFontFromOSFont`); on a device without any of the listed CJK fonts the labels would show boxes. Pixel
  devices ship Noto CJK; other OEMs were not checked.
