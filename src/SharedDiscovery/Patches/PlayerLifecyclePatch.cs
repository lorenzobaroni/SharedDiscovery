using HarmonyLib;
using SharedDiscovery.Config;
using SharedDiscovery.Discovery;

namespace SharedDiscovery.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class PlayerSpawnedPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!ModConfig.Enabled.Value || __instance == null || __instance != Player.m_localPlayer)
            {
                return;
            }

            Plugin.Network.TryRegister();
            Plugin.Session.CaptureInitialRestoredItems(__instance);

            if (__instance.InIntro())
            {
                Plugin.DebugLog("Local player spawned in intro; waiting for Valkyrie.DropPlayer before requesting snapshot.");
                return;
            }

            MarkPlayerReady(__instance);
        }

        internal static void MarkPlayerReady(Player player)
        {
            if (Plugin.Session.MarkLocalPlayerReady(player))
            {
                Plugin.Network.RequestSnapshot();
            }
        }
    }
}
