using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Progression;
using DesiMerge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Phase 6.6 content: reward calendar, missions, score milestones, extra achievements, new sound cues, the
    /// Bootstrap splash and the rebuilt scenes. Data assets are only created or extended, never overwritten, so a
    /// designer's edits survive. Batch mode: -executeMethod DesiMerge.Editor.Content.ExperienceContentSetup.Apply
    /// </summary>
    public static class ExperienceContentSetup
    {
        public const string DailyRewardsPath = ContentPaths.ProgressionFolder + "/DailyRewards.asset";
        public const string MissionsPath = ContentPaths.ProgressionFolder + "/Missions.asset";
        public const string MilestonesPath = ContentPaths.ProgressionFolder + "/Milestones.asset";

        [MenuItem("Desi Merge/Setup/Apply Phase 6.6 Experience (rebuilds scenes)")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Apply Phase 6.6?",
                    "Adds the new icons, sounds and reward catalogs, rebuilds Bootstrap's splash and regenerates MainMenu.unity, " +
                    "Gameplay.unity and the UI prefabs. Existing data assets and saves are kept.", "Apply", "Cancel"))
            {
                return;
            }

            GameArtGenerator.EnsureAll(overwrite: false);
            AudioSynth.EnsureAll();
            EnsureData();
            AssetDatabase.SaveAssets();
            ContentSetup.RebuildGeneratedScenesAndPrefabs();
            BuildSplash();
            PlayerSettings.SplashScreen.show = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Phase 6.6 experience applied.");
        }

        internal static void EnsureData()
        {
            EnsureDailyRewards();
            EnsureMissions();
            EnsureMilestones();
            ExtendAchievements();
            EnsureAudioCues();
            ConfigureGameConfig();
        }

        private static RewardDefinition Charges(string powerUp, int amount) =>
            new RewardDefinition { type = RewardType.PowerUpCharges, amount = amount, powerUpId = powerUp };

        private static void EnsureDailyRewards()
        {
            if (AssetDatabase.LoadAssetAtPath<DailyRewardCatalog>(DailyRewardsPath) != null)
            {
                return;
            }

            var catalog = ScriptableObject.CreateInstance<DailyRewardCatalog>();
            catalog.Set(new[]
            {
                new DailyRewardDay { title = "A Hammer to start", rewards = new[] { Charges("hammer", 1) } },
                new DailyRewardDay { title = "A Shuffle", rewards = new[] { Charges("shuffle", 1) } },
                new DailyRewardDay { title = "Another Hammer", rewards = new[] { Charges("hammer", 1) } },
                new DailyRewardDay { title = "Another Shuffle", rewards = new[] { Charges("shuffle", 1) } },
                new DailyRewardDay { title = "Double Hammer", rewards = new[] { Charges("hammer", 2) } },
                new DailyRewardDay { title = "Double Shuffle", rewards = new[] { Charges("shuffle", 2) } },
                new DailyRewardDay { title = "Mela bundle!", rewards = new[] { Charges("hammer", 2), Charges("shuffle", 2) }, special = true },
            });
            ContentPaths.EnsureFolder(ContentPaths.ProgressionFolder);
            AssetDatabase.CreateAsset(catalog, DailyRewardsPath);
        }

        private static MissionDefinition Mission(string id, MissionPeriod period, MissionType type, long target, string title, string description,
            RewardDefinition reward, string itemId = null, string powerUpId = null) =>
            new MissionDefinition
            {
                id = id, period = period, type = type, target = target, title = title, description = description, reward = reward,
                itemId = itemId ?? string.Empty, powerUpId = powerUpId ?? string.Empty,
            };

        private static void EnsureMissions()
        {
            if (AssetDatabase.LoadAssetAtPath<MissionCatalog>(MissionsPath) != null)
            {
                return;
            }

            const MissionPeriod daily = MissionPeriod.Daily;
            const MissionPeriod weekly = MissionPeriod.Weekly;
            var catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            catalog.Set(new[]
            {
                Mission("d_play_1", daily, MissionType.CompleteGames, 1, "Warm Up", "Finish 1 game", Charges("hammer", 1)),
                Mission("d_merge_10", daily, MissionType.Merges, 10, "Quick Mixer", "Merge 10 times", Charges("shuffle", 1)),
                Mission("d_score_300", daily, MissionType.ScoreInOneGame, 300, "Good Round", "Score 300 in one game", Charges("hammer", 1)),
                Mission("d_play_3", daily, MissionType.CompleteGames, 3, "Three Rounds", "Finish 3 games", Charges("shuffle", 1)),
                Mission("d_merge_25", daily, MissionType.Merges, 25, "Busy Hands", "Merge 25 times", Charges("hammer", 1)),
                Mission("d_create_roti", daily, MissionType.CreateItem, 1, "Garam Roti", "Make a Roti (or better)", Charges("shuffle", 1), itemId: "roti"),
                Mission("d_use_hammer", daily, MissionType.UsePowerUps, 1, "Smash!", "Use the Hammer once", Charges("shuffle", 1), powerUpId: "hammer"),
                Mission("d_use_shuffle", daily, MissionType.UsePowerUps, 1, "Mix It Up", "Use Shuffle once", Charges("hammer", 1), powerUpId: "shuffle"),
                Mission("d_score_500", daily, MissionType.ScoreInOneGame, 500, "Big Round", "Score 500 in one game", Charges("hammer", 1)),
                Mission("d_no_power", daily, MissionType.GamesWithoutPowerUps, 1, "Pure Skill", "Finish a game without power-ups", Charges("shuffle", 1)),
                Mission("w_play_15", weekly, MissionType.CompleteGames, 15, "Regular Player", "Finish 15 games this week", Charges("hammer", 2)),
                Mission("w_merge_150", weekly, MissionType.Merges, 150, "Merge Mela", "Merge 150 times this week", Charges("shuffle", 2)),
                Mission("w_score_1000", weekly, MissionType.ScoreInOneGame, 1000, "Thousand Club", "Score 1000 in one game", Charges("hammer", 2)),
                Mission("w_create_chai", weekly, MissionType.CreateItem, 1, "Chai Time", "Make a Chai (or better)", Charges("shuffle", 2), itemId: "chai"),
                Mission("w_power_5", weekly, MissionType.UsePowerUps, 5, "Power Player", "Use 5 power-ups", Charges("hammer", 2)),
                Mission("w_no_power_3", weekly, MissionType.GamesWithoutPowerUps, 3, "No Help Needed", "Finish 3 games without power-ups", Charges("shuffle", 2)),
            });
            AssetDatabase.CreateAsset(catalog, MissionsPath);
        }

        private static MilestoneDefinition Milestone(string id, string title, long score, string description, RewardDefinition reward) =>
            new MilestoneDefinition { id = id, title = title, score = score, description = description, reward = reward };

        private static void EnsureMilestones()
        {
            if (AssetDatabase.LoadAssetAtPath<MilestoneCatalog>(MilestonesPath) != null)
            {
                return;
            }

            var catalog = ScriptableObject.CreateInstance<MilestoneCatalog>();
            catalog.Set(new[]
            {
                Milestone("rank_100", "Naya Khiladi", 100, "Your first 100 points", Charges("hammer", 1)),
                Milestone("rank_250", "Bazaar Regular", 250, "A best score of 250", Charges("shuffle", 1)),
                Milestone("rank_500", "Chai Champion", 500, "A best score of 500", Charges("hammer", 2)),
                Milestone("rank_800", "Dhaba Star", 800, "A best score of 800", Charges("shuffle", 2)),
                Milestone("rank_1200", "Truck-Art Ustaad", 1200, "A best score of 1200", Charges("hammer", 2)),
                Milestone("rank_1800", "Rickshaw Racer", 1800, "A best score of 1800", Charges("shuffle", 3)),
                Milestone("rank_2500", "Bus Badshah", 2500, "A best score of 2500", Charges("hammer", 3)),
            });
            AssetDatabase.CreateAsset(catalog, MilestonesPath);
        }

        /// <summary>Adds the Phase 6.6 achievements and gives rewards to shipped achievements that have none. IDs are kept.</summary>
        private static void ExtendAchievements()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(ContentPaths.AchievementsPath);
            if (catalog == null)
            {
                return;
            }

            var serialized = new SerializedObject(catalog);
            var list = serialized.FindProperty("achievements");
            var rewards = new Dictionary<string, RewardDefinition>
            {
                { "first_game", Charges("hammer", 1) }, { "games_10", Charges("shuffle", 2) }, { "merges_50", Charges("hammer", 1) },
                { "merges_500", Charges("hammer", 3) }, { "score_500", Charges("shuffle", 2) }, { "discover_chai", Charges("hammer", 2) },
                { "discover_bus", Charges("shuffle", 3) }, { "streak_3", Charges("hammer", 2) }, { "first_merge", Charges("shuffle", 1) },
                { "score_1000", Charges("hammer", 2) }, { "discover_aam", Charges("shuffle", 1) },
            };
            var additions = new[]
            {
                new AchievementDefinition { id = "first_merge", title = "First Merge", description = "Merge two items", type = AchievementType.TotalMerges, target = 1 },
                new AchievementDefinition { id = "discover_aam", title = "Aam Ki Baat", description = "Discover the Aam", type = AchievementType.DiscoverItem, target = 1, itemId = "aam" },
                new AchievementDefinition { id = "score_1000", title = "Thousand Points", description = "Reach a best score of 1000", type = AchievementType.BestScore, target = 1000 },
            };

            var existing = new HashSet<string>();
            for (var i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                var id = element.FindPropertyRelative("id").stringValue;
                existing.Add(id);
                var reward = element.FindPropertyRelative("reward");
                if (reward.FindPropertyRelative("type").enumValueIndex == (int)RewardType.None && rewards.TryGetValue(id, out var value))
                {
                    WriteReward(reward, value);
                }
            }

            foreach (var addition in additions)
            {
                if (existing.Contains(addition.id))
                {
                    continue;
                }

                list.arraySize++;
                var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                element.FindPropertyRelative("id").stringValue = addition.id;
                element.FindPropertyRelative("title").stringValue = addition.title;
                element.FindPropertyRelative("description").stringValue = addition.description;
                element.FindPropertyRelative("type").enumValueIndex = (int)addition.type;
                element.FindPropertyRelative("target").longValue = addition.target;
                element.FindPropertyRelative("itemId").stringValue = addition.itemId ?? string.Empty;
                WriteReward(element.FindPropertyRelative("reward"), rewards[addition.id]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void WriteReward(SerializedProperty reward, RewardDefinition value)
        {
            reward.FindPropertyRelative("type").enumValueIndex = (int)value.type;
            reward.FindPropertyRelative("amount").intValue = value.amount;
            reward.FindPropertyRelative("powerUpId").stringValue = value.powerUpId;
        }

        private static void EnsureAudioCues()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(ContentPaths.AudioLibraryPath);
            if (library == null)
            {
                return;
            }

            var wanted = new (SfxCue Cue, string Path, float Volume)[]
            {
                (SfxCue.PowerUp, AudioSynth.PowerUpPath, 0.8f), (SfxCue.RewardClaim, AudioSynth.RewardPath, 0.9f),
                (SfxCue.MissionComplete, AudioSynth.MissionPath, 0.8f), (SfxCue.Unlock, AudioSynth.UnlockPath, 0.8f),
                (SfxCue.Pause, AudioSynth.PausePath, 0.6f), (SfxCue.Resume, AudioSynth.ResumePath, 0.6f), (SfxCue.NewBest, AudioSynth.NewBestPath, 1f),
            };
            var sfx = new List<AudioLibrary.SfxEntry>(library.SfxEntries);
            var changed = false;
            foreach (var (cue, path, volume) in wanted)
            {
                if (!library.TryGetSfx(cue, out _, out _))
                {
                    sfx.RemoveAll(entry => entry.cue == cue);
                    sfx.Add(new AudioLibrary.SfxEntry { cue = cue, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path), volume = volume });
                    changed = true;
                }
            }

            if (changed)
            {
                library.Set(sfx.ToArray(), new List<AudioLibrary.MusicEntry>(library.MusicEntries).ToArray(), Mathf.Max(library.SfxVoices, 8));
                EditorUtility.SetDirty(library);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioSynth.SfxFolder }))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var settings = importer.defaultSampleSettings;
                if (!settings.preloadAudioData)
                {
                    settings.preloadAudioData = true;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath);
            if (config == null)
            {
                return;
            }

            var serialized = new SerializedObject(config);
            void Assign(string property, Object value)
            {
                var field = serialized.FindProperty(property);
                if (field.objectReferenceValue == null)
                {
                    field.objectReferenceValue = value;
                }
            }

            Assign("dailyRewards", AssetDatabase.LoadAssetAtPath<DailyRewardCatalog>(DailyRewardsPath));
            Assign("missions", AssetDatabase.LoadAssetAtPath<MissionCatalog>(MissionsPath));
            Assign("milestones", AssetDatabase.LoadAssetAtPath<MilestoneCatalog>(MilestonesPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds (or replaces) the splash canvas in Bootstrap.unity and hands it to the bootstrapper.</summary>
        internal static void BuildSplash()
        {
            var scene = EditorSceneManager.OpenScene(FoundationSetup.BootstrapScenePath, OpenSceneMode.Single);
            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;
            GameBootstrapper bootstrapper = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Splash")
                {
                    Object.DestroyImmediate(root);
                    continue;
                }

                bootstrapper = bootstrapper != null ? bootstrapper : root.GetComponent<GameBootstrapper>();
                var camera = root.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.backgroundColor = theme.background;
                }
            }

            var canvas = factory.Canvas("Splash", 100);
            var canvasRoot = canvas.transform;
            MainMenuSceneBuilder.BuildBackground(factory, canvasRoot);
            var logo = UiFactory.Place(UiFactory.Rect("Logo", canvasRoot), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(900f, 900f));
            var group = logo.gameObject.AddComponent<CanvasGroup>();
            var glow = factory.Image(logo, "Glow", theme.glow, theme.accent);
            UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(560f, 560f));
            var flower = factory.Ornament(logo, "Flower", new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), 460f, 0.9f);
            flower.transform.SetSiblingIndex(1);
            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath);
            var mango = progression != null && progression.IndexOf("aam") >= 0 ? progression.Get(progression.IndexOf("aam")).Sprite : null;
            if (mango != null)
            {
                var art = factory.Image(logo, "Mango", mango, Color.white);
                art.preserveAspect = true;
                UiFactory.Place(art.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(330f, 330f));
            }

            var plate = factory.Panel(logo, "Plate", theme.deep, theme.accent, 8f).rectTransform;
            UiFactory.Place(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(820f, 220f));
            var title = factory.Text(plate, "Title", "<color=#FFB627>Desi</color> Merge", theme.titleSize, theme.textLight, outline: true);
            GameplaySceneBuilder.AddShadow(title.gameObject, theme, 0.5f, 10f);
            UiFactory.Stretch(title.rectTransform, 20f, 20f, 10f, 20f);
            var tagline = factory.Text(logo, "Tagline", "Milao  •  Barhao  •  Jeeto", theme.bodySize + 4, theme.deep, body: true);
            UiFactory.Place(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(800f, 60f));

            var spinner = factory.Ornament(canvasRoot, "Spinner", new Vector2(0.5f, 0f), new Vector2(0f, 220f), 110f, 0.9f).rectTransform;
            var splash = canvas.gameObject.AddComponent<SplashScreen>();
            EditorRefs.Set(splash, ("group", group), ("logo", logo), ("spinner", spinner));
            if (bootstrapper != null)
            {
                EditorRefs.Set(bootstrapper, ("splash", splash));
            }
            else
            {
                Debug.LogError("[Setup] Bootstrap.unity has no GameBootstrapper; the splash is not connected.");
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
