using System.Linq;
using UnityEditor;
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

        private static void BuildAndroid(string outputPath, BuildOptions buildOptions)
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

            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(options);
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
