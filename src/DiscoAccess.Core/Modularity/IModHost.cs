using DiscoAccess.Core.Speech;

namespace DiscoAccess.Core.Modularity
{
    /// <summary>
    /// The services the permanent host lends to a reloadable module: a logging seam (kept here so
    /// Core stays free of any BepInEx/Unity reference) and the shared speech pipeline. The host
    /// implements this; the module receives it in <see cref="IModModule.Load"/> and calls back through
    /// it. Loaded in the default load context so host and module agree on this interface's identity.
    /// </summary>
    public interface IModHost
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);

        /// <summary>The single funnel for everything the mod says (the host owns its lifetime).</summary>
        SpeechPipeline Speech { get; }
    }
}
