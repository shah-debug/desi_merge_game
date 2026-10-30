# Desi Merge: Phase 6.6 Audit

Date: 2026-10-30. Starting point: commit `e2d162a` (Phase 6.5), clean working tree. Everything below was checked in the project files (scene YAML, assets, code), not assumed.

## What was inspected

`PhaseStatus.md`, the Phase 6 QA report and the Phase 6.5 reports; every runtime script under `Scripts/` (audio, settings, persistence, progression, power-ups, cosmetics, UI, gameplay loop); the three scenes (`Bootstrap`, `MainMenu`, `Gameplay`) as YAML; `AudioLibrary.asset` and the audio import settings; all ScriptableObject catalogs; `ProjectSettings/AudioManager.asset`.

## The 15 questions

| # | Question | Finding (root cause) |
|---|---|---|
| 1 | Why does it still look like a prototype? | The item art and HUD were redone in 6.5, but the **experience** around the game was missing. The game started on Unity's default splash, then a black Bootstrap camera. There was no first-time help. The menu offered only Play, Collection, Awards and Settings. Nothing rewarded coming back (no daily reward, missions or ranks), and the game-over card showed only the score. |
| 2 | Why do objects appear as simple circles? | **They no longer do in the current project.** Since 6.5 all 10 items have their own illustrated sprite, and the test `EveryLevel_HasItsOwnIllustratedSprite_DrawnUntinted` enforces it. `Placeholder/Circle.png` is only a fallback. Circles can only appear in a build made **before** Phase 6.5 (e.g. the Phase 6 APK). Rebuild the APK to see the art. |
| 3 | Why do icons look generic? | 6.5 shipped 17 icons, but several meanings had none, so screens reused a star or trophy or showed text only. Gift (daily rewards), calendar, missions, medal (rank), back and unlock were missing. **Added** in the same drawing style. |
| 4 | Why aren't transitions smooth? | The existing `PanelTransition`, entrance and punch animations were fine, but: several menu panels could be opened on top of each other (`CanOpenPanel` only checked scene loading); claims had no feedback; progress bars jumped; there was no splash or onboarding transition. **Fixed** with one-panel-at-a-time navigation, the reward popup, animated progress bars, the splash and the tutorial. |
| 5 | **Why don't sounds play?** | **No `AudioListener` existed anywhere.** Each scene had one Camera (`--- !u!20`) and zero AudioListeners (`--- !u!81`). The scene builders and `FoundationSetup` create cameras with `AddComponent<Camera>()`, which never adds a listener (only the Editor's "New Scene" template does). Without an enabled listener, Unity mixes nothing, so every AudioSource "played" silently. The tests checked clip assignment and volume values, not audible output, so this went unnoticed. |
| 6 | Are music/SFX AudioSources configured correctly? | Yes: one music source plus a round-robin SFX pool, 2D (`spatialBlend = 0`), `playOnAwake` off, on the Bootstrap object, which is `DontDestroyOnLoad`. |
| 7 | AudioMixer groups and exposed parameters? | **None exist in the project.** That is not a bug: volumes are applied per source (`AudioMix.Effective`). No mixer was added, because it would add a second path to keep in sync without fixing anything. |
| 8 | Are mute and volume actually applied? | Yes. `AudioService` listens to `ISettingsService.Changed` and recomputes every source volume immediately. Proven by the tests `AudioService_MusicVolumeAndMute_ApplyImmediately` and the existing settings tests. |
| 9 | Paused, destroyed or duplicated on scene change? | No. The host survives scene loads; `PlayMusic` with the same track does not restart; a scene played directly in the Editor uses its own host, which is destroyed with that scene. The existing test counts AudioSources after menu → gameplay (now 1 + 8 voices). |
| 10 | Does an AudioListener exist and stay enabled? | **No (root cause, see #5).** Fixed: `AudioService` adds exactly one `AudioListener` to its host, so there is one for the whole session. Tests check exactly one listener in the real menu scene. |
| 11 | Clips assigned and imported correctly? | All 6 were assigned (ADPCM SFX / Vorbis music, mono). SFX had `preloadAudioData = false`, which delays the first play while the clip loads. **Changed** to preload for SFX. The `3D` import flag has no effect because sources are 2D. |
| 12 | Do gameplay events call the audio service? | Drop, merge, game over, button taps and music: yes. **Missing:** power-up use (reused the drop sound), pause/resume, reward claim, unlock/discovery, mission complete, new best. **Added** 7 cues with original synthesized clips. |
| 13 | Do scene transitions reset audio or settings? | No. Settings and audio are application-lifetime services created once by the bootstrapper. |
| 14 | Are UI controls connected to their systems? | Settings sliders and toggles are bound and saved. The haptics flag is stored but nothing vibrates, so **no vibration toggle is shown** (it would be a fake control). Every new button was verified by a PlayMode test. |
| 15 | Is there a complete progression and reward experience? | **No.** There was one daily challenge per day, achievements without rewards, a dormant coin balance (`Rewards.cs`: "the game design has no currency economy"), and a cosmetics foundation nothing used. **Implemented:** 7-day rewards, daily and weekly missions, score ranks, achievement rewards and one reward popup. All rewards are real power-up charges. |

## Economy decision

The only real, spendable inventory is **saved power-up charges** (Hammer, Shuffle; maximum 9 each, from Phase 5). Every new reward pays in those. Coins stay dormant and are **not shown**, because there is nothing to spend them on, and the brief forbids fake currency. The **shop is deferred** for the same reason: the cosmetics foundation exists, but no cosmetic changes how the game looks yet, so a shop would sell nothing real.

## Other findings

- **Unity splash:** it was on by default. It is now off (`PlayerSettings.SplashScreen.show = false`, allowed for all Unity 6 licenses) and replaced by the branded splash in `Bootstrap.unity`.
- **Android back:** it already worked for the existing panels. It is extended to the new screens in topmost-first order: reward popup, then panels, then app exit.
- **Short screens:** the 1560-tall results card would not fit a 3:4 screen. Modal cards now join the existing `ResponsiveScaler`, so they shrink on short screens, as the HUD already did.
