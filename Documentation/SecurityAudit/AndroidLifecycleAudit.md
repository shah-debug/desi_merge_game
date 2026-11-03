# Desi Merge — Audio and Android Lifecycle Audit

Static inspection (2026-11-03). Earlier phases tested some of this on a Pixel 7 Pro (install/launch); no lifecycle
experiment was performed for this audit.

## 1. Lifecycle handling (from code)

| Event | Handler | Effect |
|---|---|---|
| `OnApplicationPause(true)` | `AppLifecycleHook` → `GameBootstrapper.FlushAll`; `GameplayEntryPoint.OnApplicationPause` → `Loop.HandleAppInterrupted` | All dirty services saved; a running game pauses (timeScale 0, input off, music paused) |
| `OnApplicationFocus(false)` | Same as above (gameplay ignores focus in the Editor) | Save + pause |
| `OnApplicationPause(false)` (resume) | `Resumed` event (no subscriber needed) | Game stays on the pause card until the player resumes — intended |
| `OnApplicationQuit` | `FlushAll` | Save |
| Bootstrapper destroyed | `OnDestroy` → `FlushAll` | Save |
| Scene unload while paused | `GameplayLoop.Dispose` restores `timeScale = 1` | No frozen menu |
| Force-stop / process kill | No callback | Data since the last flush is lost — see below |

## 2. Where progress can be lost

| Scenario | Lost | Kept |
|---|---|---|
| Home / call / notification shade, then the OS kills the app | The current board, score and that run's Diamonds (**DM-01**) | Everything flushed on pause: merges, discoveries, missions, settings, claims |
| Hard crash or `am force-stop` while playing, without a prior pause | Above, plus anything changed since the last flush: Diamonds spent on power-ups (DM-05), skin-level discoveries | Claims and grants (flushed immediately) |
| Crash between the two writes of a skin purchase | Skin ownership while Diamonds are spent (DM-06) | — |
| Crash during a file write | Nothing: `.tmp` + `File.Replace` keeps the previous file; `.bak` holds one older version | — |
| App uninstall | All local saves unless restored by Android auto-backup (DM-16). The 2026-11-02 "lost progress" report traced to an uninstall, not to code. | — |

## 3. Audio

| Check | Result |
|---|---|
| AudioListener | Exactly one, owned by `AudioService` on the persistent bootstrapper host (fixed in Phase 6.6) |
| Duplicate music across scenes | One music `AudioSource`; `PlayMusic` with the current track only un-pauses (`AudioService.PlayMusic`) |
| Restart while paused | `Restart` → `Begin` → `PlayMusic(Gameplay)` un-pauses the same track — no silent music |
| Game over | `StopMusic()` then a one-shot cue |
| Volume / mute persistence | `SettingsService` saves on panel close, language change and `FlushAll`; `AudioService` re-applies on `Settings.Changed` |
| SFX overlap | Fixed-size voice pool (`AudioLibrary.SfxVoices`), round-robin — bounded |
| Missing clip | `ReportMissingOnce` logs once and continues silently |
| Android audio focus / call interruption | Unity's player pauses audio when the app loses focus; the game also pauses music. Not verified on a device in this audit. |

No audio finding above INFORMATIONAL.

## 4. Findings

* **DM-01 (MEDIUM, CODE-VERIFIED)** — see GameplayWeaknesses.md.
* **DM-05, DM-06 (LOW)** — see SaveAndRewardSecurity.md.
* **DM-11 (LOW, CODE-VERIFIED path / UNVERIFIED on device):** `FileSaveStore.Write` falls back to copy → delete → move when
  `File.Replace` throws `PlatformNotSupportedException` (lines 61–66). Between `File.Delete(path)` and `File.Move` a crash
  leaves no main file (the `.bak` copy still exists and `Load` would recover it). Whether IL2CPP-on-Android takes the
  `File.Replace` path was not verified; saves have been observed to persist on the Pixel, which shows *a* path works.
* **DM-16 (INFORMATIONAL, CONFIRMED):** the APK manifest does not set `android:allowBackup`, so Android's default (enabled)
  applies and saves can be restored by Google backup on reinstall. Positive for players; nothing secret is stored.
