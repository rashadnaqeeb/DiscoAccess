using System;
using WhirlingInWords.Dev;
using WhirlingInWords.Modularity;
using UnityEngine;

namespace WhirlingInWords.Host
{
    /// <summary>
    /// The one injected MonoBehaviour, registered once for the process (IL2CPP type registration is
    /// permanent, which is why this lives in the host and never reloads). Each frame it pumps the dev
    /// server's job queue, drives the current feature module's per-frame work, and watches for the F6
    /// reload hotkey. The module's body is read fresh from the loader each frame so a reload swaps it
    /// in with no further wiring.
    /// </summary>
    public sealed class HostPump : MonoBehaviour
    {
        // Required for an IL2CPP-injected MonoBehaviour.
        public HostPump(IntPtr ptr) : base(ptr) { }

#if DEBUG
        internal static DevServer DevServer;
#endif
        internal static ModuleLoader Loader;

        private void Update()
        {
#if DEBUG
            try
            {
                DevServer?.Pump();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning("DevServer.Pump: " + ex);
            }
#endif

            try
            {
                Loader?.Module?.Tick();
            }
            catch (Exception ex)
            {
                // Full exception (with stack); a per-frame Tick failure is otherwise unlocatable.
                Plugin.Logger?.LogWarning("Module.Tick: " + ex);
            }

#if DEBUG
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
            {
                Plugin.Logger?.LogInfo("F6: reloading module");
                // Through the dev server so the eval REPL is reset to the new module's types too,
                // matching POST /reload.
                DevServer?.ReloadFromHost();
            }
#endif
        }
    }
}
