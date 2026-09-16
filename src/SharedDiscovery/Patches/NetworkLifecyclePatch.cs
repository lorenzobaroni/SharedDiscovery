using HarmonyLib;
using SharedDiscovery.Discovery;

namespace SharedDiscovery.Patches
{
    [HarmonyPatch(typeof(ZNet), "Awake")]
    internal static class ZNetAwakePatch
    {
        private static void Postfix()
        {
            Plugin.Network.TryRegister();
        }
    }

    [HarmonyPatch(typeof(ZNet), "LoadWorld")]
    internal static class ZNetLoadWorldPatch
    {
        private static void Postfix()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            Plugin.DebugLog("SharedDiscovery host world bootstrap after ZNet.LoadWorld.");
            if (WorldContext.TryGetCurrentWorldId(out string worldId))
            {
                Plugin.DebugLog($"World UID: {worldId}.");
            }

            Plugin.Discovery.EnsureWorldLoaded();
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class ZNetPeerInfoPatch
    {
        private static void Postfix()
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
            {
                return;
            }

            bool playerReady = Plugin.Session.IsLocalPlayerReady;
            bool worldContextValid = WorldContext.TryGetCurrentWorldId(out _);
            Plugin.DebugLog($"Client world info received through RPC_PeerInfo. Player ready: {playerReady}. World context valid: {worldContextValid}.");

            if (!playerReady || !worldContextValid)
            {
                return;
            }

            Plugin.DebugLog("Snapshot retry triggered after RPC_PeerInfo.");
            Plugin.Network.RequestSnapshot();
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class ZNetDestroyPatch
    {
        private static void Prefix()
        {
            Plugin.ResetSessionState(save: true);
        }
    }
}
