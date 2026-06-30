namespace DiscoAccess.Core.Settings
{
    /// <summary>
    /// The shared surface of one mod setting: a stable persistence <see cref="Key"/> (never spoken) and an
    /// authored, spoken <see cref="Label"/>. <see cref="ModSettings"/> holds every setting through this base
    /// in declaration order, so the settings menu can list them as one sequence and pick a cell by concrete
    /// type (<see cref="ToggleSetting"/>, <see cref="RangeSetting"/>).
    /// </summary>
    public abstract class ModSetting
    {
        /// <summary>Stable persistence key (never spoken), e.g. "wall_tone_volume".</summary>
        public string Key { get; }

        /// <summary>Authored, spoken label.</summary>
        public string Label { get; }

        protected ModSetting(string key, string label)
        {
            Key = key;
            Label = label;
        }
    }
}
