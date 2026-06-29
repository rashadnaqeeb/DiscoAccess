using System;
using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.Strings;
using DiscoAccess.Core.UI.Nav;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// The mod's own settings menu, a navigable overlay opened with Ctrl+M from anywhere. It maps to no game
    /// <see cref="Sunshine.Views.ViewType"/>: the <see cref="ScreenManager"/> pushes it as an overlay that
    /// floats above the game, owns the keyboard, and closes on Escape (or Ctrl+M again). Built fresh on each
    /// open from the live setting values, one toggle per mod setting.
    /// </summary>
    public sealed class ModMenuScreen
    {
        /// <summary>Spoken when the menu opens; the landed setting then queues behind.</summary>
        public string Title => Strings.ScreenModMenu;

        public Container BuildRoot(IModHost host, Action onClose)
        {
            var root = new OverlayRoot(onClose);
            var list = new Container(ContainerShape.VerticalList);
            foreach (var setting in host.Settings.Toggles)
                list.Add(new SettingToggleCell(setting));
            root.Add(list);
            return root;
        }
    }
}
