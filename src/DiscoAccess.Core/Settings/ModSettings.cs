using System.Collections.Generic;
using static DiscoAccess.Core.Strings.Strings;

namespace DiscoAccess.Core.Settings
{
    /// <summary>
    /// The mod's settings: each one declared once here, loaded from the store on construction and persisted
    /// as it changes. The host owns the single instance (built with its concrete <see cref="ISettingsStore"/>)
    /// and lends it to the module through <c>IModHost.Settings</c>, the same way it lends the speech pipeline,
    /// so the values survive a module hot-reload. Feature code reads a setting by its strongly-typed property
    /// (<see cref="AutoReadDialogue"/>); the settings menu iterates <see cref="Toggles"/>.
    /// </summary>
    public sealed class ModSettings
    {
        private readonly List<ToggleSetting> _toggles = new List<ToggleSetting>();

        /// <summary>Every toggle setting, in declaration order, for the settings menu to list.</summary>
        public IReadOnlyList<ToggleSetting> Toggles => _toggles;

        /// <summary>Speak each new conversation line automatically as it is delivered. Off lands the cursor
        /// on the line silently, leaving the player to read it on their own terms.</summary>
        public ToggleSetting AutoReadDialogue { get; }

        public ModSettings(ISettingsStore store)
        {
            AutoReadDialogue = Add(new ToggleSetting(
                "auto_read_dialogue", SettingAutoReadDialogue, defaultValue: true, store));
        }

        private ToggleSetting Add(ToggleSetting setting)
        {
            _toggles.Add(setting);
            return setting;
        }
    }
}
