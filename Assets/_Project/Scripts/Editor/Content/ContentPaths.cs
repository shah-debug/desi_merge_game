using System.IO;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    internal static class ContentPaths
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        public const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        public const string GameplayScenePath = ScenesFolder + "/Gameplay.unity";
        public const string UiPrefabsFolder = "Assets/_Project/Prefabs/UI";
        public const string ButtonPrefabPath = UiPrefabsFolder + "/ThemedButton.prefab";
        public const string SettingsPanelPrefabPath = UiPrefabsFolder + "/SettingsPanel.prefab";
        public const string CollectionPanelPrefabPath = UiPrefabsFolder + "/CollectionPanel.prefab";
        public const string AchievementsPanelPrefabPath = UiPrefabsFolder + "/AchievementsPanel.prefab";
        public const string ProgressionFolder = "Assets/_Project/ScriptableObjects/Progression";
        public const string AchievementsPath = ProgressionFolder + "/Achievements.asset";
        public const string DailyChallengesPath = ProgressionFolder + "/DailyChallenges.asset";
        public const string PowerUpsPath = ProgressionFolder + "/PowerUps.asset";
        public const string MergeProgressionPath = "Assets/_Project/ScriptableObjects/MergeProgression.asset";
        public const string ItemPrefabPath = "Assets/_Project/Prefabs/MergeItem.prefab";
        public const string ThemePath = "Assets/_Project/Settings/UI/UITheme.asset";
        public const string AudioLibraryPath = "Assets/_Project/Audio/AudioLibrary.asset";
        public const string SparkleMaterialPath = "Assets/_Project/Art/Materials/Sparkle.mat";
        public const string RingMaterialPath = "Assets/_Project/Art/Materials/MergeRing.mat";

        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    /// <summary>Sets private [SerializeField] members by name and fails loudly on typos.</summary>
    internal static class EditorRefs
    {
        public static void Set(Object target, params (string Property, object Value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (property, value) in values)
            {
                var field = serialized.FindProperty(property);
                if (field == null)
                {
                    Debug.LogError($"[Setup] {target.GetType().Name} has no serialized field '{property}'.");
                    continue;
                }

                switch (value)
                {
                    case null:
                        field.objectReferenceValue = null;
                        break;
                    case Object reference:
                        field.objectReferenceValue = reference;
                        break;
                    case float number:
                        field.floatValue = number;
                        break;
                    case int number:
                        field.intValue = number;
                        break;
                    case bool flag:
                        field.boolValue = flag;
                        break;
                    case string text:
                        field.stringValue = text;
                        break;
                    case Vector2 vector:
                        field.vector2Value = vector;
                        break;
                    case Color color:
                        field.colorValue = color;
                        break;
                    case Object[] array:
                        field.arraySize = array.Length;
                        for (var i = 0; i < array.Length; i++)
                        {
                            field.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                        }

                        break;
                    default:
                        Debug.LogError($"[Setup] Unsupported value type {value.GetType().Name} for '{property}'.");
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
