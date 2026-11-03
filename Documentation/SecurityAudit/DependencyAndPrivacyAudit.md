# Desi Merge — Android Build, Packages, Third-Party and Privacy Audit

Evidence: `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset`, `Scripts/Editor/OfflinePermissionFilter.cs`,
and the built APK `Builds/Android/DesiMerge-rc-unsigned-test.apk` (2026-11-03 00:04) read with
`aapt2 dump badging` / `dump xmltree` from the Unity-bundled SDK (build-tools 36.0.0).

## 1. Built APK facts (CONFIRMED with aapt2)

| Item | Value |
|---|---|
| Package | `com.DefaultCompany.DesiMerge` (Unity placeholder) |
| Version | versionCode 1, versionName 1.0 |
| SDK | min 26 (ProjectSettings), target 36, compile 36 |
| ABI | arm64-v8a only |
| Permissions requested | only `com.DefaultCompany.DesiMerge.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (added by AndroidX for non-exported runtime receivers) — **no INTERNET, no storage, no location, no ads ID** |
| Exported components | launcher activity only |
| `allowBackup` | not declared → Android default (enabled) |
| Signing | debug keystore (`androidUseCustomKeystore: 0`) |

## 2. Network and SDKs

* No third-party SDKs, no `Plugins/` folder, no custom Gradle or manifest.
* Release builds use `NullAnalyticsService` and `UnavailableRewardedAdService` (`GameServicesFactory.Create`, decided by
  `Debug.isDebugBuild`). `SimulatedRewardedAdService` and `LogAnalyticsService` exist only for development builds.
* `OfflinePermissionFilter` removes INTERNET from non-development manifests; the built APK confirms it.
* Runtime code contains no `UnityWebRequest`, socket or HTTP use (searched `UnityWebRequest|HttpClient|WWW|Socket` in
  `Assets/_Project/Scripts` excluding Editor: none).
* With no INTERNET permission the app cannot send data, so no data-safety items arise from network transfer.

## 3. Packages (`Packages/manifest.json`)

Used by the game: `com.unity.inputsystem`, `com.unity.ugui`, `com.unity.render-pipelines.universal`, `com.unity.2d.sprite`,
`com.unity.test-framework` (tests), physics2d/audio/particle/UI modules.

Present but not referenced by project code (INFORMATIONAL **DM-17**): `com.unity.visualscripting`,
`com.unity.learn.iet-framework` (Editor tutorials), `com.unity.collab-proxy`, `com.unity.2d.animation`, `2d.aseprite`,
`2d.psdimporter`, `2d.spriteshape`, `2d.tilemap(.extras)`, `2d.tooling`, `timeline`, and engine modules such as `terrain`,
`vehicles`, `cloth`, `ai`, `video`, `xr`, `unitywebrequest*`, `unityanalytics`. Engine code stripping (`unity.strip-engine-code`
in the manifest metadata) removes unused modules from the player; editor-only packages don't ship. Removing them would mainly
shorten import time and reduce attack surface on paper. Not verified which of them contribute bytes to the APK.

`submitAnalytics: 1` in ProjectSettings refers to Unity **Editor** usage analytics, not the player.

## 4. Release / policy risks

* **DM-12 (MEDIUM, CONFIRMED — release blocker, not a vulnerability):** placeholder application id
  `com.DefaultCompany.DesiMerge`, debug signing, versionCode 1. Google Play requires a release-signed app (upload key / Play
  App Signing), and the package name cannot change after the first upload. Must be decided before any store build.
* Target SDK 36 satisfies the current Google Play target-API requirement *as far as known at audit time*; the exact
  requirement date was not re-checked online for this audit.
* Privacy: no personal data is collected or stored; saves hold game stats only. A privacy policy is still normally required
  for a Play listing; Data safety form answers would be "no data collected / shared" given the confirmed permission set.
* **DM-20 (INFORMATIONAL):** release builds keep `Debug.Log*` calls (e.g. `[Save]` warnings, `[Build]`), visible in logcat.
  They contain save keys and exception messages, no personal data or secrets.
* No credentials, API keys or keystore passwords are present in the repository (searched for `password`, `apikey`,
  `secret`, `.keystore`, `.jks`: none outside documentation text).
