using BepInEx.Logging;
using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.Speech;

namespace DiscoAccess.Modularity
{
    /// <summary>
    /// The host's <see cref="IModHost"/>: routes the module's logging through the BepInEx logger and
    /// hands it the shared speech pipeline. Lives in the default load context with Core, so the module
    /// (in its collectible context) sees the same <see cref="IModHost"/> type identity.
    /// </summary>
    internal sealed class ModHost : IModHost
    {
        private readonly ManualLogSource _log;

        public ModHost(ManualLogSource log, SpeechPipeline speech)
        {
            _log = log;
            Speech = speech;
        }

        public SpeechPipeline Speech { get; }

        public void LogInfo(string message) => _log.LogInfo(message);
        public void LogWarning(string message) => _log.LogWarning(message);
        public void LogError(string message) => _log.LogError(message);
    }
}
