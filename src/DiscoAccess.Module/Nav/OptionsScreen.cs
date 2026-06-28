using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.Strings;
using DiscoAccess.Core.UI.Nav;
using Sunshine.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// The options screen as a rich, custom-navigated tree. Root Panel = [tabs (Settings, Controls), a
    /// content panel, the Reset button]. The content panel is rebuilt when the tab changes (poll-detected
    /// in <see cref="OnUpdate"/>) while the tabs and Reset stay put, so focus on the tab list survives the
    /// switch - this is where the dynamic-tree refresh lives.
    ///
    /// The Settings tab's content is the game's category sections (Graphics, Audio, ...), each a labeled
    /// list of its option controls read live through <see cref="OptionControl"/>. The Controls tab is the
    /// game's controller/keyboard reference; at the main menu it has no interactable controls, so its
    /// content is whatever interactable controls exist (the in-game rebinding buttons).
    /// </summary>
    public sealed class OptionsScreen : Screen
    {
        public override ViewType ViewType => Sunshine.Views.ViewType.OPTIONS;
        public override string ScreenName => Strings.ScreenOptions;

        // The rebuilt content panel and the tab it currently reflects (true = Settings), for detecting a
        // tab switch in OnUpdate. Held as a live reference into the tree we built, not cached game state.
        private Container _content;
        private bool _builtSettings;

        public override Container BuildRoot(IModHost host)
        {
            var root = new ScreenRoot();

            var h = SettingsHeaderController.singleton;
            if (h == null)
            {
                host.LogWarning("OptionsScreen: SettingsHeaderController.singleton is null; empty screen.");
                return root;
            }

            // Tabs: Settings, Controls. Navigated up/down; activating one runs the game's own tab switch.
            var tabs = new Container(ContainerShape.VerticalList);
            Selectable settingsBtn = h.settingsButton != null ? h.settingsButton.GetComponent<Selectable>() : null;
            Selectable controlsBtn = h.controlsButton != null ? h.controlsButton.GetComponent<Selectable>() : null;
            if (settingsBtn != null)
                tabs.Add(new OptionTab(settingsBtn, () => h.settingsView.activeInHierarchy, () => h.SelectSettingsView()));
            else
                host.LogWarning("OptionsScreen: Settings tab button has no Selectable; tab omitted.");
            if (controlsBtn != null)
                tabs.Add(new OptionTab(controlsBtn, () => h.controlsView.activeInHierarchy, () => h.SelectControlsView()));
            else
                host.LogWarning("OptionsScreen: Controls tab button has no Selectable; tab omitted.");
            root.Add(tabs);

            // Content (rebuilt on tab change).
            _content = new Container(ContainerShape.Panel);
            root.Add(_content);
            RebuildContent(host, h);

            // Reset settings: a screen-level button (its activation opens the game's confirm popup, which
            // the dialog reader announces).
            Selectable reset = ResetButton(h);
            if (reset != null)
                root.Add(new SelectableButton(reset));
            else
                host.LogWarning("OptionsScreen: Reset settings button not found; it will be unreachable.");

            return root;
        }

        public override bool OnUpdate(IModHost host, TraditionalNavigator nav)
        {
            var h = SettingsHeaderController.singleton;
            if (h == null || _content == null)
                return false;
            // A tab switch swaps which subview is active; refill the content panel to match. You normally
            // switch from the tab list, so focus survives; but an outside switch (a mouse click, which the
            // lever does not mute) can orphan the focused control, so re-home. The ScreenManager announces
            // the landing once after this returns.
            if (h.settingsView.activeInHierarchy == _builtSettings)
                return false;
            RebuildContent(host, h);
            // Re-home only if the switch orphaned focus (an outside switch while focus was inside the
            // content); switching from the tab list leaves focus on the tab, which the arrow move already
            // announced. The result tells the ScreenManager whether to announce the new landing.
            return nav.EnsureFocusValid();
        }

        private void RebuildContent(IModHost host, SettingsHeaderController h)
        {
            _builtSettings = h.settingsView.activeInHierarchy;
            _content.Clear();
            if (_builtSettings)
                BuildSettings(h.settingsView.transform);
            else
                BuildControls(h.controlsView.transform);
            if (_content.Children.Count == 0)
                host.LogWarning($"OptionsScreen: {(_builtSettings ? "Settings" : "Controls")} tab built no navigable content.");
        }

        // The Settings tab: every option control under the subview as one flat list in visual order. DE's
        // own category grouping is not read out - it added verbosity and the game's grouping does not map
        // cleanly (e.g. the dyslexic-font toggle sits under Audio).
        private void BuildSettings(Transform settings)
        {
            var list = new Container(ContainerShape.VerticalList);
            foreach (var osc in settings.GetComponentsInChildren<OptionSelectableController>(false))
                list.Add(new OptionControl(osc));
            if (list.Children.Count > 0)
                _content.Add(list);
        }

        // The Controls tab: a read-only controller/keyboard reference. Each entry is an action label
        // ("<key>-label" text node of whichever device panel is active) paired with its key, translated
        // from the sibling glyph's icon sprite. The keyboard panel's nodes are duplicated, so Unity
        // suffixes their names ("trigger-L1-label (1)"); match "-label" as a substring, not the end.
        private void BuildControls(Transform controls)
        {
            var list = new Container(ContainerShape.VerticalList);
            foreach (var t in controls.GetComponentsInChildren<TMP_Text>(false))
                if (t.gameObject.name.Contains("-label") && !string.IsNullOrWhiteSpace(t.text))
                    list.Add(new ControlReference(t, GlyphFor(t.transform)));
            if (list.Children.Count > 0)
                _content.Add(list);
        }

        // The key glyph paired with an action label is its sibling, named the same minus "-label"
        // (label "trigger-L1-label (1)" -> glyph "trigger-L1 (1)").
        private static Image GlyphFor(Transform label)
        {
            Transform parent = label.parent;
            if (parent == null)
                return null;
            Transform glyph = parent.Find(label.name.Replace("-label", ""));
            return glyph != null ? glyph.GetComponent<Image>() : null;
        }

        private static Selectable ResetButton(SettingsHeaderController h)
        {
            Transform t = h.settingsView != null ? h.settingsView.transform : null;
            while (t != null && t.name != "Options Screen")
                t = t.parent;
            if (t == null)
                return null;
            Transform reset = t.Find("Content/ResetSettings");
            return reset != null ? reset.GetComponent<Selectable>() : null;
        }
    }
}
