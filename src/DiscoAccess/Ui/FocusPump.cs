using System;
using DiscoAccess.Core.Speech;
using UnityEngine;
using UnityEngine.UI;

namespace DiscoAccess.Ui
{
    /// <summary>
    /// The UI focus pump. DE drives menu focus through NavigationManager over uGUI Selectables; we
    /// poll the current selection once per frame and announce it when it changes. (House rule from
    /// the reference mods: announce from the update loop; navigation interrupts.)
    /// </summary>
    public sealed class FocusPump : MonoBehaviour
    {
        // Required for an IL2CPP-injected MonoBehaviour.
        public FocusPump(IntPtr ptr) : base(ptr) { }

        private IntPtr _lastSelected = IntPtr.Zero;

        private void Update()
        {
            try
            {
                var nav = NavigationManager.Singleton;
                if (nav == null)
                    return;

                Selectable selected = nav.GetCurrentSelectedSelectable();
                IntPtr ptr = selected != null ? selected.Pointer : IntPtr.Zero;
                if (ptr == _lastSelected)
                    return;

                _lastSelected = ptr;
                if (selected == null)
                    return;

                string text = FocusReader.Read(selected);
                if (!string.IsNullOrEmpty(text))
                    SpeechPipeline.Instance?.Speak(text, interrupt: true);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning("FocusPump.Update: " + ex.Message);
            }
        }
    }
}
