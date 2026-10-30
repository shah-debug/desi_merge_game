using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DesiMerge.Editor
{
    /// <summary>
    /// Desi Merge is offline and its code makes no network calls, but Unity adds android.permission.INTERNET to the
    /// generated manifest by default. For non-development builds this removes it, so the store listing doesn't show a
    /// permission the game never uses. Development builds keep it (the profiler connects over the network).
    /// Remove this class if a network SDK (e.g. a real ad provider) is ever added.
    /// </summary>
    public sealed class OfflinePermissionFilter : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        private static bool _developmentBuild;

        private static readonly Regex InternetPermission =
            new Regex(@"\s*<uses-permission[^>]*android:name=""android\.permission\.INTERNET""[^>]*/>", RegexOptions.Compiled);

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report) =>
            _developmentBuild = (report.summary.options & BuildOptions.Development) != 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (_developmentBuild)
            {
                return;
            }

            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[Build] No manifest at {manifestPath}; INTERNET permission not checked.");
                return;
            }

            var manifest = File.ReadAllText(manifestPath);
            var filtered = RemoveInternetPermission(manifest);
            if (filtered != manifest)
            {
                File.WriteAllText(manifestPath, filtered);
                Debug.Log("[Build] Removed the unused INTERNET permission (offline release build).");
            }
        }

        public static string RemoveInternetPermission(string manifest) => InternetPermission.Replace(manifest, string.Empty);
    }
}
