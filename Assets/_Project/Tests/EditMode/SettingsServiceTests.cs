using DesiMerge.Persistence;
using DesiMerge.Settings;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class SettingsServiceTests
    {
        [Test]
        public void FirstRun_UsesConfiguredDefaults()
        {
            var defaults = new SettingsData { musicVolume = 0.25f, sfxVolume = 0.5f, hapticsEnabled = false };

            var settings = new SettingsService(new InMemorySaveStore(), defaults);

            Assert.AreEqual(0.25f, settings.MusicVolume);
            Assert.AreEqual(0.5f, settings.SfxVolume);
            Assert.IsFalse(settings.HapticsEnabled);
        }

        [Test]
        public void ChangingSettings_DoesNotMutateDefaultsAsset()
        {
            var defaults = new SettingsData { musicVolume = 0.25f };
            var settings = new SettingsService(new InMemorySaveStore(), defaults);

            settings.MusicVolume = 1f;

            Assert.AreEqual(0.25f, defaults.musicVolume);
        }

        [Test]
        public void Volumes_AreClamped()
        {
            var settings = new SettingsService(new InMemorySaveStore(), new SettingsData());

            settings.MusicVolume = 2f;
            settings.SfxVolume = -1f;

            Assert.AreEqual(1f, settings.MusicVolume);
            Assert.AreEqual(0f, settings.SfxVolume);
        }

        [Test]
        public void Changed_RaisedOnlyForActualChanges()
        {
            var settings = new SettingsService(new InMemorySaveStore(), new SettingsData { hapticsEnabled = true });
            var count = 0;
            settings.Changed += () => count++;

            settings.HapticsEnabled = true;
            settings.HapticsEnabled = false;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SavedSettings_TakePrecedenceOverDefaults()
        {
            var store = new InMemorySaveStore();
            var first = new SettingsService(store, new SettingsData());
            first.SfxVolume = 0.1f;
            first.Flush();

            var second = new SettingsService(store, new SettingsData { sfxVolume = 0.9f });

            Assert.AreEqual(0.1f, second.SfxVolume, 0.0001f);
        }
    }
}
