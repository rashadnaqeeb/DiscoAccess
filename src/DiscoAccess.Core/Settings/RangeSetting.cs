namespace DiscoAccess.Core.Settings
{
    /// <summary>
    /// One numeric mod setting on a 0..100 percent scale (a volume), adjusted in fixed <see cref="Step"/>
    /// increments and persisted through an <see cref="ISettingsStore"/>. The settings menu steps it with
    /// <see cref="Increase"/>/<see cref="Decrease"/> (which report whether the value actually moved, so the
    /// menu can announce a bound); feature code reads <see cref="Fraction"/> as a 0..1 gain.
    /// </summary>
    public sealed class RangeSetting : ModSetting
    {
        private readonly ISettingsStore _store;

        public int DefaultValue { get; }
        public int Step { get; }

        /// <summary>Current value, a whole percent in [0, 100].</summary>
        public int Value { get; private set; }

        /// <summary>The value as a 0..1 gain, for an audio system to scale by.</summary>
        public float Fraction => Value / 100f;

        public RangeSetting(string key, string label, int defaultValue, int step, ISettingsStore store)
            : base(key, label)
        {
            DefaultValue = Clamp(defaultValue);
            Step = step;
            _store = store;
            Value = Clamp(store.GetInt(key, defaultValue));
        }

        /// <summary>Step up one increment; returns false (and changes nothing) when already at the maximum.</summary>
        public bool Increase() => Adjust(Step);

        /// <summary>Step down one increment; returns false (and changes nothing) when already at the minimum.</summary>
        public bool Decrease() => Adjust(-Step);

        private bool Adjust(int delta)
        {
            int next = Clamp(Value + delta);
            if (next == Value) return false;
            Value = next;
            _store.SetInt(Key, next);
            return true;
        }

        private static int Clamp(int v) => v < 0 ? 0 : v > 100 ? 100 : v;
    }
}
