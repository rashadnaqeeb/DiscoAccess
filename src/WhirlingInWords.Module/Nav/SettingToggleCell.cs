using System.Collections.Generic;
using WhirlingInWords.Core.Settings;
using WhirlingInWords.Core.Strings;
using WhirlingInWords.Core.UI.Nav;

namespace WhirlingInWords.Module.Nav
{
    /// <summary>
    /// A navigable toggle for one boolean mod setting. Reads the live value at announce time and, on
    /// activate, flips and persists it; the navigator then re-announces the new state ("on"/"off").
    /// </summary>
    public sealed class SettingToggleCell : UIElement
    {
        private readonly ToggleSetting _setting;

        public SettingToggleCell(ToggleSetting setting) => _setting = setting;

        public override string Label => _setting.Label;
        public override string Role => Strings.ControlToggle;
        public override string Value => _setting.Value ? Strings.StatusOn : Strings.StatusOff;
        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Activate, () => _setting.Toggle());
        }
    }
}
