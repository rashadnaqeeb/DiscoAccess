using System.Collections.Generic;
using DiscoAccess.Core.Settings;
using Xunit;

namespace DiscoAccess.Tests
{
    public class ModSettingsTests
    {
        // An in-memory stand-in for the host's BepInEx-backed store.
        private sealed class FakeStore : ISettingsStore
        {
            public readonly Dictionary<string, bool> Saved = new Dictionary<string, bool>();
            public readonly Dictionary<string, int> SavedInts = new Dictionary<string, int>();
            public bool GetBool(string key, bool defaultValue) => Saved.TryGetValue(key, out bool v) ? v : defaultValue;
            public void SetBool(string key, bool value) => Saved[key] = value;
            public int GetInt(string key, int defaultValue) => SavedInts.TryGetValue(key, out int v) ? v : defaultValue;
            public void SetInt(string key, int value) => SavedInts[key] = value;
        }

        [Fact]
        public void AutoReadDialogue_DefaultsOn_WhenNothingStored()
        {
            var settings = new ModSettings(new FakeStore());
            Assert.True(settings.AutoReadDialogue.Value);
        }

        [Fact]
        public void Setting_LoadsStoredValue_OverDefault()
        {
            var store = new FakeStore();
            store.Saved["auto_read_dialogue"] = false;

            var settings = new ModSettings(store);

            Assert.False(settings.AutoReadDialogue.Value);
        }

        [Fact]
        public void Toggle_FlipsAndPersists()
        {
            var store = new FakeStore();
            var settings = new ModSettings(store);

            bool now = settings.AutoReadDialogue.Toggle();

            Assert.False(now);
            Assert.False(settings.AutoReadDialogue.Value);
            Assert.False(store.Saved["auto_read_dialogue"]);
            // A fresh ModSettings over the same store reads the persisted value back.
            Assert.False(new ModSettings(store).AutoReadDialogue.Value);
        }

        [Fact]
        public void All_ListsEverySetting()
        {
            var settings = new ModSettings(new FakeStore());
            Assert.Contains(settings.AutoReadDialogue, settings.All);
            Assert.Contains(settings.WallToneVolume, settings.All);
            Assert.Contains(settings.WallTonesContinuous, settings.All);
        }

        [Fact]
        public void WallToneVolume_DefaultsLowAndConvertsToFraction()
        {
            var settings = new ModSettings(new FakeStore());
            Assert.Equal(5, settings.WallToneVolume.Value);
            Assert.Equal(0.05f, settings.WallToneVolume.Fraction);
        }

        [Fact]
        public void WallTonesContinuous_DefaultsOff()
        {
            var settings = new ModSettings(new FakeStore());
            Assert.False(settings.WallTonesContinuous.Value);
        }

        [Fact]
        public void RangeSetting_StepsClampPersistAndReportBounds()
        {
            var store = new FakeStore();
            store.SavedInts["wall_tone_volume"] = 100; // start at the ceiling to exercise the upper bound
            var settings = new ModSettings(store);
            var vol = settings.WallToneVolume;

            // At the maximum, a further increase changes nothing and reports no move (the menu reads "maximum").
            Assert.False(vol.Increase());
            Assert.Equal(100, vol.Value);

            // A decrease steps by 5 and persists.
            Assert.True(vol.Decrease());
            Assert.Equal(95, vol.Value);
            Assert.Equal(95, store.SavedInts["wall_tone_volume"]);
            Assert.Equal(0.95f, vol.Fraction);

            // A fresh ModSettings over the same store reads the persisted value back.
            Assert.Equal(95, new ModSettings(store).WallToneVolume.Value);
        }

        [Fact]
        public void RangeSetting_FloorsAtZero()
        {
            var store = new FakeStore();
            store.SavedInts["wall_tone_volume"] = 5;
            var vol = new ModSettings(store).WallToneVolume;

            Assert.True(vol.Decrease());
            Assert.Equal(0, vol.Value);
            Assert.False(vol.Decrease()); // already at the floor
            Assert.Equal(0, vol.Value);
        }
    }
}
