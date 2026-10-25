using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesiMerge.Editor
{
    internal static class EditorSceneUtility
    {
        /// <summary>
        /// Creates and saves a new scene, then reopens the scenes that were open before.
        /// <paramref name="populate"/> runs with the new scene active. Load any assets it needs inside it:
        /// opening a scene in Single mode unloads unused assets, invalidating references loaded earlier.
        /// Returns false if the user kept unsaved changes.
        /// </summary>
        public static bool CreateScene(string path, Action populate)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning($"[Setup] {path} not created: open scenes have unsaved changes.");
                return false;
            }

            // Untitled scenes cannot be restored by path, so only saved scenes are reopened afterwards.
            var previousSetup = EditorSceneManager.GetSceneManagerSetup().Where(s => !string.IsNullOrEmpty(s.path)).ToArray();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Setup] Created {path}");

            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            return true;
        }

        /// <summary>Puts <paramref name="orderedPaths"/> first in Build Settings (enabled), keeping any other scenes after them.</summary>
        public static void SetLeadingBuildScenes(params string[] orderedPaths)
        {
            var updated = new List<EditorBuildSettingsScene>();
            foreach (var path in orderedPaths)
            {
                updated.Add(new EditorBuildSettingsScene(path, true));
            }

            updated.AddRange(EditorBuildSettings.scenes.Where(s => !orderedPaths.Contains(s.path)));
            EditorBuildSettings.scenes = updated.ToArray();
        }
    }
}
