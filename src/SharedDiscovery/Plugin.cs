using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SharedDiscovery.Config;
using SharedDiscovery.Discovery;
using SharedDiscovery.Network;
using SharedDiscovery.Persistence;

namespace SharedDiscovery
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "lorenzo.valheim.shareddiscovery";
        public const string Name = "SharedDiscovery";
        public const string Version = "0.1.0";

        private Harmony? _harmony;

        internal static Plugin Instance { get; private set; } = null!;
        internal static ManualLogSource Log { get; private set; } = null!;
        internal static DiscoveryService Discovery { get; private set; } = null!;
        internal static VanillaDiscoveryService VanillaDiscovery { get; private set; } = null!;
        internal static DiscoveryNetwork Network { get; private set; } = null!;
        internal static DiscoverySessionState Session { get; private set; } = null!;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Bind(Config);

            WorldDiscoveryStoreAdapter store = new WorldDiscoveryStoreAdapter();
            Session = new DiscoverySessionState();
            Discovery = new DiscoveryService(store);
            VanillaDiscovery = new VanillaDiscoveryService();
            Network = new DiscoveryNetwork(Discovery, VanillaDiscovery);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll();

            Network.TryRegister();

            Log.LogInfo($"{Name} {Version} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            ResetSessionState(save: true);
            Log.LogInfo($"{Name} unloaded.");
        }

        internal static void ResetSessionState(bool save)
        {
            if (save)
            {
                Discovery.SaveCurrentWorld();
            }

            Session.Reset();
            Discovery.Clear();
            VanillaDiscovery.ClearPending();
            Network.ResetSession();
        }

        internal static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                Log.LogInfo(message);
            }
        }
    }
}
