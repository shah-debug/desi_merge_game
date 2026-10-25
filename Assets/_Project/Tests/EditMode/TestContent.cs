using System.Collections.Generic;
using DesiMerge.Configuration;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>Builds gameplay configuration in code and destroys everything it created.</summary>
    internal sealed class TestContent
    {
        private readonly List<Object> _created = new List<Object>();

        public Sprite Sprite { get; private set; }

        public TestContent()
        {
            var texture = Track(new Texture2D(4, 4));
            Sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f));
        }

        public MergeItemDefinition Definition(string id, float radius = 0.4f, int score = 10, Sprite sprite = null)
        {
            var definition = Track(ScriptableObject.CreateInstance<MergeItemDefinition>());
            definition.Set(id, id, sprite ?? Sprite, Color.white, radius, 1f, score);
            return definition;
        }

        /// <summary>Levels l0..l(n-1) with radius 0.3 + 0.1 * level and score 10 * (level + 1).</summary>
        public MergeProgression Progression(int levels = 4, int spawnable = 2)
        {
            var definitions = new List<MergeItemDefinition>();
            for (var i = 0; i < levels; i++)
            {
                definitions.Add(Definition($"l{i}", 0.3f + 0.1f * i, 10 * (i + 1)));
            }

            return Progression(definitions, spawnable);
        }

        public MergeProgression Progression(IEnumerable<MergeItemDefinition> definitions, int spawnable)
        {
            var progression = Track(ScriptableObject.CreateInstance<MergeProgression>());
            progression.Set(definitions, spawnable);
            return progression;
        }

        public GameplayConfig Config(MergeProgression progression, FinalLevelMergeRule finalRule = FinalLevelMergeRule.ClearBoth)
        {
            var config = Track(ScriptableObject.CreateInstance<GameplayConfig>());
            config.Set(progression, null, finalRule: finalRule);
            return config;
        }

        public void DestroyAll()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
