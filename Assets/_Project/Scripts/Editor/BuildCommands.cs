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

        [MenuItem("Desi Merge/Build/Android Development APK")]
        public static void BuildAndroidDevelopment()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = AndroidOutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] Android result={summary.result} errors={summary.totalErrors} size={summary.totalSize} output={AndroidOutputPath}");

            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
