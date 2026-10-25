using System.Collections.Generic;
using System.Linq;
using DesiMerge.Configuration;
using DesiMerge.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesiMerge.Editor
{
    /// <summary>
    /// Creates the assets the foundation needs: the GameConfig asset, the Bootstrap scene, and
    /// the Bootstrap scene as build index 0. Safe to run repeatedly; existing assets are kept.
    /// Batch mode: -executeMethod DesiMerge.Editor.FoundationSetup.CreateFoundationAssets
    /// </summary>
    public static class FoundationSetup
    {
        public const string GameConfigPath = "Assets/_Project/ScriptableObjects/GameConfig.asset";
        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [MenuItem("Desi Merge/Setup/Create Foundation Assets")]
        public static void CreateFoundationAssets()
        {
            EnsureGameConfig();
            EnsureBootstrapScene();
            EnsureBootstrapSceneIsFirstInBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Foundation assets are in place.");
        }

        private static void EnsureGameConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath) != null)
            {
                return;
            }

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), GameConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Created {GameConfigPath}");
        }

        private static void EnsureBootstrapScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null)
            {
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Setup] Bootstrap scene not created: open scenes have unsaved changes.");
                return;
            }

            // Untitled scenes cannot be restored by path, so only saved scenes are reopened afterwards.
            var previousSetup = EditorSceneManager.GetSceneManagerSetup().Where(s => !string.IsNullOrEmpty(s.path)).ToArray();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            // Loaded after NewScene: opening a scene in Single mode unloads unused assets, invalidating earlier references.
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            var bootstrapObject = new GameObject("Bootstrap");
            var bootstrapper = bootstrapObject.AddComponent<GameBootstrapper>();
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            Debug.Log($"[Setup] Created {BootstrapScenePath}");

            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void EnsureBootstrapSceneIsFirstInBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != BootstrapScenePath).ToList();
            var updated = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(BootstrapScenePath, true) };
            updated.AddRange(scenes);
            EditorBuildSettings.scenes = updated.ToArray();
        }
    }
}
