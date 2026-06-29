using DiscoAccess.Core.Modularity;
using Sunshine;
using UnityEngine.UI;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// Runs the inventory's actions the game's own way. The primary action is the controller submit on a
    /// dock - equip, unequip, or use, whichever the game does for that slot and item - reached by making the
    /// dock the game's selection and running its submit handler (the path <see cref="SelectableButton"/>
    /// uses). The secondary action is the item's contextual interact button on the shared
    /// <see cref="InventoryTooltip"/> (READ, EAT, and the like), primed by selecting the dock first, the way
    /// <see cref="ThoughtCommit"/> drives the thought-cabinet tooltip. We drive the game's handlers rather
    /// than poke the model so its sound, animation, and list refresh all run.
    /// </summary>
    internal static class InventoryCommit
    {
        // Equip / unequip / use: the game's submit on the dock.
        public static void Primary(UIDragDock dock)
        {
            Selectable sel = InventoryAdapter.Selectable(dock);
            NavigationManager nav = NavigationManager.Singleton;
            nav.Select(sel);
            nav.Submit();
        }

        // Whether the focused item has a contextual interact action right now. The tooltip reflects the
        // focused (primed) item, so this is read for the dock the player is on.
        public static bool HasSecondary()
        {
            Button ib = InventoryAdapter.Tooltip()?.interactButton;
            return ib != null && ib.gameObject.activeInHierarchy && ib.interactable;
        }

        // The item's contextual interact: prime the tooltip on the dock, then click its interact button.
        public static void Secondary(UIDragDock dock, IModHost host)
        {
            NavigationManager.Singleton.Select(InventoryAdapter.Selectable(dock));
            Button ib = InventoryAdapter.Tooltip()?.interactButton;
            if (ib != null && ib.gameObject.activeInHierarchy && ib.interactable)
                ib.onClick.Invoke();
            else
                host.LogWarning($"InventoryCommit: no active interact button for dock '{dock.name}'.");
        }
    }
}
