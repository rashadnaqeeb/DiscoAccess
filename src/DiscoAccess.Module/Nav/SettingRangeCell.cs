using System.Collections.Generic;
using DiscoAccess.Core.Settings;
using DiscoAccess.Core.Strings;
using DiscoAccess.Core.UI.Nav;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// A navigable slider for one numeric (percent) mod setting. Reads the live value at announce time and
    /// steps it on Left/Right (decrease/increase); the navigator re-announces the new percentage, or names
    /// the bound ("minimum"/"maximum") when a step at the end moved nothing.
    /// </summary>
    public sealed class SettingRangeCell : UIElement
    {
        private readonly RangeSetting _setting;

        public SettingRangeCell(RangeSetting setting) => _setting = setting;

        public override string Label => _setting.Label;
        public override string Role => Strings.ControlSlider;
        public override string Value => Strings.Percent(_setting.Value);

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Decrease, () => _setting.Decrease());
            yield return new ElementAction(ActionIds.Increase, () => _setting.Increase());
        }
    }
}
