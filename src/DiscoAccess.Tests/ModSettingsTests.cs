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
            public bool GetBool(string key, bool defaultValue) => Saved.TryGetValue(key, out bool v) ? v : defaultValue;
            public void SetBool(string key, bool value) => Saved[key] = value;
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
        public void Toggles_ListsEverySetting()
        {
            var settings = new ModSettings(new FakeStore());
            Assert.Contains(settings.AutoReadDialogue, settings.Toggles);
        }
    }
}
