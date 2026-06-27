using System;
using DiscoAccess.Core.Modularity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiscoAccess.Module
{
    /// <summary>
    /// The reloadable focus reader. Holds the per-frame body that used to live in the injected pump:
    /// poll DE's NavigationManager for the current uGUI selection and announce it when it changes,
    /// through the host's speech pipeline (navigation interrupts, per the house rule). This is the
    /// implementor the host loads by interface scan; future dialogue/inventory/world readers and any
    /// Harmony patches join it here.
    /// </summary>
    public sealed class FocusModule : IModModule
    {
        private IModHost _host;
        private Harmony _harmony;
        private IntPtr _lastSelected = IntPtr.Zero;

        public void Load(IModHost host)
        {
            _host = host;
            // A per-load id so a reload's Dispose unpatches exactly this load's patches. No patches yet;
            // future readers register them through this instance.
            _harmony = new Harmony("com.rashad.discoaccess.module");
        }

        public void Tick()
        {
            Selectable selected = CurrentSelectable();
            // Dedup on the native address. (A destroyed Selectable's address can in principle be
            // reused by a new one; in practice a menu rebuild passes through a null-selection frame,
            // which resets this, so that collision effectively can't slip through.)
            IntPtr ptr = selected != null ? selected.Pointer : IntPtr.Zero;
            if (ptr == _lastSelected)
                return;

            if (selected == null)
            {
                _lastSelected = ptr;
                return;
            }

            string text = FocusReader.Read(selected);
            if (!string.IsNullOrEmpty(text))
                _host.Speech.Speak(text, interrupt: true);

            // Advance only after a successful read/speak, so an exception in the speech path (caught
            // and logged by the host pump) leaves the change un-acknowledged and retried next frame
            // rather than permanently suppressed.
            _lastSelected = ptr;
        }

        // DE drives focus through NavigationManager, but at a freshly opened menu the uGUI EventSystem
        // records the selection a frame or more before NavigationManager does; fall back to it so the
        // initial focus is announced during that window (the dev FocusInspector/InputInjector do the same).
        private static Selectable CurrentSelectable()
        {
            var nav = NavigationManager.Singleton;
            Selectable sel = nav != null ? nav.GetCurrentSelectedSelectable() : null;
            if (sel != null)
                return sel;

            EventSystem es = EventSystem.current;
            GameObject go = es != null ? es.currentSelectedGameObject : null;
            return go != null ? go.GetComponent<Selectable>() : null;
        }

        public void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            _host = null;
        }
    }
}
