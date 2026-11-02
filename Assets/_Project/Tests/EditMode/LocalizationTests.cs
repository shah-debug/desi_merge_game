using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Configuration;
using DesiMerge.Localization;
using DesiMerge.Persistence;
using DesiMerge.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>The shipped translation table, language switching, Urdu shaping and the saved language choice.</summary>
    public class LocalizationTests
    {
        private const string TablePath = "Assets/_Project/Localization/Strings.txt";
        private const string GameConfigPath = "Assets/_Project/ScriptableObjects/GameConfig.asset";

        private TextAsset _table;

        [SetUp]
        public void SetUp()
        {
            _table = AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath);
            Assert.IsNotNull(_table, "Translation table missing");
            Loc.Initialize(_table.text, null, "en");
        }

        [TearDown]
        public void TearDown() => Loc.Initialize(_table != null ? _table.text : null, null, "en");

        [Test]
        public void Table_HasAColumnForEveryLanguage_AndWellFormedRows()
        {
            var lines = _table.text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToArray();
            var header = lines[0].Split('\t');
            Assert.AreEqual("key", header[0]);
            foreach (var language in Loc.Languages.Where(l => l.Code != "en"))
            {
                CollectionAssert.Contains(header, language.Code);
            }

            var keys = new System.Collections.Generic.HashSet<string>();
            foreach (var line in lines.Skip(1))
            {
                var cells = line.Split('\t');
                Assert.AreEqual(header.Length, cells.Length, "Row has the wrong cell count: " + cells[0]);
                Assert.IsTrue(keys.Add(cells[0]), "Duplicate key: " + cells[0]);
            }
        }

        [Test]
        public void Table_TranslationsKeepTheSamePlaceholders()
        {
            var lines = _table.text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToArray();
            var placeholder = new Regex(@"\{\d\}");
            foreach (var line in lines.Skip(1))
            {
                var cells = line.Split('\t');
                var expected = placeholder.Matches(cells[0]).Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray();
                for (var i = 1; i < cells.Length; i++)
                {
                    if (cells[i].Length == 0)
                    {
                        continue;
                    }

                    var actual = placeholder.Matches(cells[i]).Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray();
                    CollectionAssert.AreEqual(expected, actual, $"Placeholders differ for '{cells[0]}' in column {i}");
                }
            }
        }

        [Test]
        public void SwitchingLanguage_TranslatesAndRaisesChanged()
        {
            var changes = 0;
            void Count() => changes++;
            Loc.Changed += Count;
            try
            {
                Assert.AreEqual("PLAY", Loc.T("PLAY"));
                Loc.SetLanguage("fr");
                Assert.AreEqual("JOUER", Loc.T("PLAY"));
                Loc.SetLanguage("zh");
                Assert.AreEqual("开始", Loc.T("PLAY"));
                Assert.AreEqual("需要 7 钻石。玩游戏并领取奖励来获得更多。", Loc.F("Need {0} diamonds. Play and claim rewards to earn more.", 7));
                Assert.AreEqual(2, changes);
            }
            finally
            {
                Loc.Changed -= Count;
            }
        }

        [Test]
        public void UnknownKeysAndUnknownLanguages_FallBackToEnglish()
        {
            Loc.SetLanguage("de");
            Assert.AreEqual("Some text nobody translated", Loc.T("Some text nobody translated"));
            Loc.Initialize(_table.text, null, "xx");
            Assert.AreEqual("en", Loc.Current);
            Assert.AreEqual("PLAY", Loc.T("PLAY"));
        }

        [Test]
        public void Urdu_IsShapedIntoPresentationForms_InVisualOrder()
        {
            Loc.SetLanguage("ur");
            Assert.IsTrue(Loc.IsRightToLeft);
            Assert.AreEqual("کھیلیں", Loc.Raw("PLAY"));
            var shaped = Loc.T("PLAY");
            Assert.IsTrue(shaped.All(c => c >= 'ﭐ' && c <= '﻿'), "Every letter should be a presentation form: " + Codes(shaped));

            // Visual order is reversed: the last logical letter (final noon-ghunna) is drawn first, on the left.
            Assert.AreEqual(UrduShaper.ShapeLine("کھیلیں"), shaped);
            Assert.AreEqual('ﮟ', shaped[0], "Final noon ghunna first: " + Codes(shaped));
        }

        [Test]
        public void Urdu_KeepsNumbersLeftToRight()
        {
            Loc.SetLanguage("ur");
            var shaped = Loc.F("Best  {0}", 1250);
            StringAssert.Contains("1250", shaped);
        }

        [Test]
        public void FontFor_UsesTheUrduFontOnlyForUrdu()
        {
            var original = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var urdu = Font.CreateDynamicFontFromOSFont("Arial", 18);
            Loc.Initialize(_table.text, urdu, "en");
            Assert.AreSame(original, Loc.FontFor(original));
            Loc.SetLanguage("ur");
            Assert.AreSame(urdu, Loc.FontFor(original));
            Loc.SetLanguage("es");
            Assert.AreSame(original, Loc.FontFor(original));
        }

        [Test]
        public void ChosenLanguage_IsSavedAndLoadedAgain()
        {
            var store = new InMemorySaveStore();
            var first = new DesiMerge.Settings.SettingsService(store, new SettingsData());
            Assert.AreEqual("en", first.Language, "English is the default");
            first.Language = "ms";
            first.Flush();

            var second = new DesiMerge.Settings.SettingsService(store, new SettingsData());
            Assert.AreEqual("ms", second.Language);
        }

        [Test]
        public void GameConfig_ShipsTheTableAndUrduFont()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.LocalizationTable, "GameConfig.localizationTable not assigned");
            Assert.IsNotNull(config.UrduFont, "GameConfig.urduFont not assigned");
            Assert.IsTrue(config.UrduFont.HasCharacter('ﮟ'), "Urdu font lacks noon ghunna final form");
        }

        private static string Codes(string s) => string.Join(" ", s.Select(c => ((int)c).ToString("X4")));
    }
}
