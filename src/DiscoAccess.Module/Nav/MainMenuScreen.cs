using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.Strings;
using DiscoAccess.Core.UI.Nav;
using Sunshine.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// The title main menu as a single vertical list of its sidebar buttons (Continue, New Game, Load
    /// Game, Collage, Options, Quit). The buttons are the active, interactable Selectable children of the
    /// menu's content container, in sibling (visual) order; hidden entries (Quick Save, Save Game, the
    /// return-to-title Main Menu button) are inactive and skipped. The container is located from the live
    /// selection, which the game still holds at the menu when focus mode engages. The game reuses this
    /// view for the in-game pause menu too; <see cref="PauseMenuScreen"/> handles that case, so this is
    /// the fallback for the title screen and is not sealed.
    /// </summary>
    public class MainMenuScreen : Screen
    {
        public override ViewType ViewType => Sunshine.Views.ViewType.MAINMENU;
        public override string ScreenName => Strings.ScreenMainMenu;

        // The title menu's root is the bare list with NO Back: at the title screen Escape does nothing in
        // vanilla, so we must not wire it to the view's CloseOnEscapeKey (which collapses the menu). An
        // Escape here is left unconsumed; the pump then hands the keyboard back so the game's own (no-op at
        // the title) Escape runs. The pause menu, which DOES resume on Escape, adds its own closeable root.
        public override Container BuildRoot(IModHost host) => BuildList(host);

        /// <summary>The vertical list of the menu's active, interactable buttons in visual order. Shared
        /// with <see cref="PauseMenuScreen"/>, which wraps it in a closeable root.</summary>
        protected static Container BuildList(IModHost host)
        {
            var list = new Container(ContainerShape.VerticalList);

            Transform parent = MenuContent();
            if (parent == null)
            {
                host.LogWarning("MainMenuScreen: no live selection to locate the menu; list is empty.");
                return list;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                var selectable = child.GetComponent<Selectable>();
                if (selectable == null || !selectable.interactable) continue;
                // Collage opens DE's screenshot composition canvas, a visual screen with no accessible
                // path, so it is the one button we navigate to but refuse to open. A menu button knows the
                // view it opens via RelatedViewType; the Collage entry's is COLLAGEMODE.
                var menuButton = child.GetComponent<MainMenuButton>();
                if (menuButton != null && menuButton.RelatedViewType == ViewType.COLLAGEMODE)
                    list.Add(new BlockedButton(selectable, host, Strings.CollageInaccessible));
                else
                    list.Add(new SelectableButton(selectable));
            }
            return list;
        }

        /// <summary>The menu's button container, located from the live selection (its parent), or null
        /// when nothing is selected yet.</summary>
        protected static Transform MenuContent()
        {
            Selectable start = CurrentSelectable();
            return start != null ? start.transform.parent : null;
        }

        // The game's current selection: NavigationManager (it keeps the menu selection even after we
        // disable it), falling back to the EventSystem ground truth.
        private static Selectable CurrentSelectable()
        {
            var nav = NavigationManager.Singleton;
            Selectable sel = nav != null ? nav.GetCurrentSelectedSelectable() : null;
            if (sel != null) return sel;
            var es = EventSystem.current;
            var go = es != null ? es.currentSelectedGameObject : null;
            return go != null ? go.GetComponent<Selectable>() : null;
        }
    }
}
