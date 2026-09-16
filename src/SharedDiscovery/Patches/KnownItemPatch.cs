using HarmonyLib;
using SharedDiscovery.Config;
using SharedDiscovery.Discovery;

namespace SharedDiscovery.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    internal static class KnownItemPatch
    {
        private static void Prefix(Player __instance, ItemDrop.ItemData item, ref bool __state)
        {
            __state = true;

            if (!ModConfig.Enabled.Value || __instance == null || item?.m_shared == null)
            {
                return;
            }

            __state = __instance.IsMaterialKnown(item.m_shared.m_name);
        }

        private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __state)
        {
            if (!ModConfig.Enabled.Value || __state)
            {
                return;
            }

            if (__instance == null || __instance != Player.m_localPlayer || item?.m_shared == null)
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item?.m_shared?.m_name ?? "<unknown>"}': not local player.");
                return;
            }

            if (VanillaDiscoveryService.IsApplyingSharedDiscovery)
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item.m_shared.m_name}': remote-application guard active.");
                return;
            }

            if (!__instance.IsMaterialKnown(item.m_shared.m_name))
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item.m_shared.m_name}': vanilla did not retain knowledge.");
                return;
            }

            if (!Plugin.Session.AcceptLocalDiscoveries)
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item.m_shared.m_name}': local discoveries not accepted.");
                return;
            }

            if (!Plugin.Session.IsLocalPlayerReady)
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item.m_shared.m_name}': session not ready.");
                return;
            }

            if (ZNet.instance != null && ZNet.instance.IsServer() && !Plugin.Discovery.IsWorldLoaded)
            {
                Plugin.DebugLog($"Ignoring local AddKnownItem '{item.m_shared.m_name}': no active world state.");
                return;
            }

            try
            {
                string? candidatePrefab = VanillaItemIdentifier.GetCandidatePrefabName(item);
                Plugin.DebugLog($"Discovery candidate: sharedName='{item.m_shared.m_name}' dropPrefab='{candidatePrefab ?? "<none>"}'.");

                string? itemId = VanillaItemIdentifier.GetPrefabItemId(item);
                if (itemId == null)
                {
                    Plugin.DebugLog($"Failed to resolve stable prefab ID: sharedName='{item.m_shared.m_name}' dropPrefab='{candidatePrefab ?? "<none>"}'.");
                    return;
                }

                if (Plugin.Session.IsRestoredItem(itemId))
                {
                    Plugin.DebugLog($"Ignoring restored startup item '{itemId}'.");
                    return;
                }

                Plugin.DebugLog($"{itemId} is not part of restored startup baseline; processing discovery.");
                Plugin.DebugLog($"Resolved stable prefab ID: {itemId}");
                Plugin.Network.SubmitLocalDiscovery(itemId);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"SharedDiscovery failed to process discovered item '{item.m_shared.m_name}': {ex}");
            }
        }
    }
}
