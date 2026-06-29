namespace DiscoAccess.Core.Settings
{
    /// <summary>
    /// One on/off mod setting: an authored label, a default, and the current value, loaded from and
    /// persisted through an <see cref="ISettingsStore"/>. This is the mod's own state, not game state, so
    /// it is legitimately held (the "never cache game state" rule is about game data). Feature code reads
    /// <see cref="Value"/>; the settings menu flips it with <see cref="Toggle"/>.
    /// </summary>
    public sealed class ToggleSetting
    {
        private readonly ISettingsStore _store;

        /// <summary>Stable persistence key (never spoken), e.g. "auto_read_dialogue".</summary>
        public string Key { get; }

        /// <summary>Authored, spoken label.</summary>
        public string Label { get; }

        public bool DefaultValue { get; }

        public bool Value { get; private set; }

        public ToggleSetting(string key, string label, bool defaultValue, ISettingsStore store)
        {
            Key = key;
            Label = label;
            DefaultValue = defaultValue;
            _store = store;
            Value = store.GetBool(key, defaultValue);
        }

        /// <summary>Flip the value, persist it, and return the new state.</summary>
        public bool Toggle()
        {
            Value = !Value;
            _store.SetBool(Key, Value);
            return Value;
        }
    }
}
