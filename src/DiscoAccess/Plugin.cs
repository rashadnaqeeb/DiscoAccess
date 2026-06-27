using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DiscoAccess.Core.Speech;
using DiscoAccess.Core.Strings;
using DiscoAccess.Engine;
using DiscoAccess.Speech;
using DiscoAccess.Ui;
using Il2CppInterop.Runtime.Injection;

namespace DiscoAccess
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BasePlugin
    {
        public const string Guid = "com.rashad.discoaccess";
        public const string Name = "DiscoAccess";
        public const string Version = "0.1.0";

        internal static ManualLogSource Logger;

        private PrismBackend _prism;

        public override void Load()
        {
            Logger = Log;
            Log.LogInfo($"{Name} {Version} loading");

            _prism = new PrismBackend(Log);
            _prism.Initialize();

            SpeechPipeline.Instance = new SpeechPipeline(_prism, new StopwatchClock());
            SpeechPipeline.Instance.Speak(Strings.ModLoaded, interrupt: true);

            ClassInjector.RegisterTypeInIl2Cpp<FocusPump>();
            AddComponent<FocusPump>();

            Log.LogInfo($"{Name} loaded");
        }
    }
}
