using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DesiMerge.Editor
{
    /// <summary>
    /// Command-line builds, e.g.:
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod DesiMerge.Editor.BuildCommands.BuildAndroidDevelopment -logFile build.log
    /// Uses the scenes enabled in Build Settings and the current Player Settings.
    /// </summary>
    public static class BuildCommands
    {
        private const string AndroidOutputPath = "Builds/Android/DesiMerge-dev.apk";
        private const string AndroidReleaseCandidatePath = "Builds/Android/DesiMerge-rc-unsigned-test.apk";

        [MenuItem("Desi Merge/Build/Android Development APK")]
        public static void BuildAndroidDevelopment() => BuildAndroid(AndroidOutputPath, BuildOptions.Development);

        /// <summary>
        /// Release configuration (no Development flag: no simulated ads, no development fallbacks) for validation and
        /// device testing. Without a project keystore Unity signs it with the debug key, so it is NOT uploadable to Play.
        /// </summary>
        [MenuItem("Desi Merge/Build/Android Release-Config APK (test signing)")]
        public static void BuildAndroidReleaseConfig() => BuildAndroid(AndroidReleaseCandidatePath, BuildOptions.None);

        // ---------- Google Play release ----------

        /// <summary>Permanent once the app is on Google Play: never change it after the first upload.</summary>
        public const string ApplicationId = "com.desimerge.game";
        public const string CompanyName = "Desi Merge";
        public const string Version = "1.0.0";
        /// <summary>Must go up by at least 1 for every upload to Play (1, 2, 3, ...).</summary>
        public const int VersionCode = 1;

        /// <summary>Default upload keystore location: outside the project, so it can never be committed.</summary>
        public static string DefaultKeystorePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DesiMergeKeys", "desimerge-upload.keystore");

        /// <summary>Writes the store identity (app id, company, version) into Player Settings. Safe to run again.</summary>
        [MenuItem("Desi Merge/Release/Apply Store Identity")]
        public static void ApplyStoreIdentity()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.bundleVersion = Version;
            // Never lower an existing version code (Play rejects a code that isn't higher than the last upload).
            PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode, VersionCode);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] Store identity: {ApplicationId} {Version} ({PlayerSettings.Android.bundleVersionCode})");
        }

        /// <summary>Store identity + Play Store icon in one batch call (no signing, no build).</summary>
        public static void PrepareStoreRelease()
        {
            ApplyStoreIdentity();
            Content.LauncherIconBuilder.WriteStoreIcon();
        }

        /// <summary>
        /// Signed Android App Bundle (.aab) for Google Play, plus the native debug symbols zip Play Console asks for.
        /// The keystore lives outside the project (<see cref="DefaultKeystorePath"/> or DESIMERGE_KEYSTORE). Passwords are
        /// read from the DESIMERGE_KEYSTORE_PASS / DESIMERGE_KEY_PASS environment variables for this build only and are
        /// cleared from Player Settings afterwards; they are never written to the project.
        /// </summary>
        [MenuItem("Desi Merge/Release/Google Play AAB (signed)")]
        public static void BuildGooglePlayBundle()
        {
            var keystore = Environment.GetEnvironmentVariable("DESIMERGE_KEYSTORE");
            keystore = string.IsNullOrEmpty(keystore) ? DefaultKeystorePath : keystore;
            var storePass = Environment.GetEnvironmentVariable("DESIMERGE_KEYSTORE_PASS");
            var alias = Environment.GetEnvironmentVariable("DESIMERGE_KEY_ALIAS");
            alias = string.IsNullOrEmpty(alias) ? "upload" : alias;
            var keyPass = Environment.GetEnvironmentVariable("DESIMERGE_KEY_PASS");
            keyPass = string.IsNullOrEmpty(keyPass) ? storePass : keyPass;
            if (!File.Exists(keystore) || string.IsNullOrEmpty(storePass))
            {
                Debug.LogError($"[Build] Upload keystore not found at {keystore} or DESIMERGE_KEYSTORE_PASS not set. No bundle was built.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            ApplyStoreIdentity();
            var outputPath = $"Builds/GooglePlay/DesiMerge-{Version}-{PlayerSettings.Android.bundleVersionCode}.aab";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var previousSymbols = EditorUserBuildSettings.androidCreateSymbols;
            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
                BuildAndroid(outputPath, BuildOptions.None, appBundle: true);
            }
            finally
            {
                // Leave no signing secrets or personal paths behind; test APKs go back to debug signing.
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = string.Empty;
                PlayerSettings.Android.keyaliasName = string.Empty;
                EditorUserBuildSettings.androidCreateSymbols = previousSymbols;
                AssetDatabase.SaveAssets();
            }
        }

        private const string WindowsOutputPath = "Builds/Windows/DesiMerge.exe";

        /// <summary>
        /// Windows 64-bit player for quick desktop testing (run Builds/Windows/DesiMerge.exe). Portrait game in a
        /// window; the Android build remains the release target.
        /// </summary>
        [MenuItem("Desi Merge/Build/Windows Test Build")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WindowsOutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"[Build] Windows result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} " +
                      $"size={summary.totalSize} time={summary.totalTime} output={WindowsOutputPath}");

            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void BuildAndroid(string outputPath, BuildOptions buildOptions, bool appBundle = false)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = buildOptions,
            };

            EditorUserBuildSettings.buildAppBundle = appBundle;
            var report = BuildPipeline.BuildPlayer(options);
            EditorUserBuildSettings.buildAppBundle = false;
            var summary = report.summary;
            Debug.Log($"[Build] Android result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} " +
                      $"size={summary.totalSize} time={summary.totalTime} scenes={string.Join(",", scenes)} output={outputPath}");

            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
