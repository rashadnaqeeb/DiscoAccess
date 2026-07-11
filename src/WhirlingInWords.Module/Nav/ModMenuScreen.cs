using System;
using WhirlingInWords.Core.Modularity;
using WhirlingInWords.Core.Settings;
using WhirlingInWords.Core.Strings;
using WhirlingInWords.Core.UI.Nav;

namespace WhirlingInWords.Module.Nav
{
    /// <summary>
    /// The mod's own settings menu, a navigable <see cref="ModOverlay"/> opened with F12 from anywhere
    /// and closed on Escape (or F12 again). Built fresh on each open from the live setting values, one
    /// control per mod setting.
    /// </summary>
    public sealed class ModMenuScreen : ModOverlay
    {
        /// <summary>Spoken when the menu opens; the landed setting then queues behind.</summary>
        public override string Title => Strings.ScreenModMenu;

        public override Container BuildRoot(IModHost host, Action onClose)
        {
            var root = new OverlayRoot(onClose);
            var list = new Container(ContainerShape.VerticalList);
            foreach (var setting in host.Settings.All)
            {
                switch (setting)
                {
                    case ToggleSetting toggle: list.Add(new SettingToggleCell(toggle)); break;
                    case RangeSetting range: list.Add(new SettingRangeCell(range)); break;
                }
            }
            root.Add(list);
            return root;
        }
    }
}
