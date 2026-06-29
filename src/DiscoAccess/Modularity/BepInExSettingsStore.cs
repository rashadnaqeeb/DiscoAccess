using System.Collections.Generic;
using BepInEx.Configuration;
using DiscoAccess.Core.Settings;

namespace DiscoAccess.Modularity
{
    /// <summary>
    /// Persists the mod's settings through BepInEx's <see cref="ConfigFile"/> (the host plugin's own
    /// <c>Config</c>), so each setting lands in a single TOML file under <c>BepInEx/config</c> that survives
    /// game restarts and is editable by hand. Each key binds a <see cref="ConfigEntry{T}"/> once and is then
    /// reused; setting its value auto-saves the file.
    /// </summary>
    internal sealed class BepInExSettingsStore : ISettingsStore
    {
        private const string Section = "Settings";
        private readonly ConfigFile _config;
        private readonly Dictionary<string, ConfigEntry<bool>> _bools = new Dictionary<string, ConfigEntry<bool>>();

        public BepInExSettingsStore(ConfigFile config) => _config = config;

        private ConfigEntry<bool> Bind(string key, bool defaultValue)
        {
            if (!_bools.TryGetValue(key, out ConfigEntry<bool> entry))
            {
                entry = _config.Bind(Section, key, defaultValue);
                _bools[key] = entry;
            }
            return entry;
        }

        public bool GetBool(string key, bool defaultValue) => Bind(key, defaultValue).Value;

        public void SetBool(string key, bool value) => Bind(key, value).Value = value;
    }
}
