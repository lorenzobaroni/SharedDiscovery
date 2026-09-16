using HarmonyLib;

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

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    internal static class ZNetDestroyPatch
    {
        private static void Prefix()
        {
            Plugin.ResetSessionState(save: true);
        }
    }
}
