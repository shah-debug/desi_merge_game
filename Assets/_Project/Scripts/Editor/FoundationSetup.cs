using DesiMerge.Configuration;
using DesiMerge.Core;
using UnityEditor;
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

            EditorSceneUtility.CreateScene(BootstrapScenePath, () =>
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                var bootstrapObject = new GameObject("Bootstrap");
                var bootstrapper = bootstrapObject.AddComponent<GameBootstrapper>();
                var serialized = new SerializedObject(bootstrapper);
                serialized.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void EnsureBootstrapSceneIsFirstInBuild()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].path == BootstrapScenePath && scenes[0].enabled)
            {
                return;
            }

            EditorSceneUtility.SetLeadingBuildScenes(BootstrapScenePath);
        }
    }
}
