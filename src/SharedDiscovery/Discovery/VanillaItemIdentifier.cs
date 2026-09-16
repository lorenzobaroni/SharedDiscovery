using SharedDiscovery.Core.Discovery;
using UnityEngine;

namespace SharedDiscovery.Discovery
{
    internal static class VanillaItemIdentifier
    {
        public static string? GetPrefabItemId(ItemDrop.ItemData item)
        {
            if (item == null || ObjectDB.instance == null)
            {
                return null;
            }

            GameObject? prefab = item.m_dropPrefab;
            if (prefab == null && item.m_shared != null)
            {
                prefab = ObjectDB.instance.GetItemPrefab(item.m_shared);
            }

            string? itemId = GetNormalizedPrefabName(prefab);
            if (itemId == null)
            {
                return null;
            }

            GameObject registeredPrefab = ObjectDB.instance.GetItemPrefab(itemId);
            return registeredPrefab != null && registeredPrefab.GetComponent<ItemDrop>() != null ? itemId : null;
        }

        public static string? GetCandidatePrefabName(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return null;
            }

            GameObject? prefab = item.m_dropPrefab;
            if (prefab == null && item.m_shared != null && ObjectDB.instance != null)
            {
                prefab = ObjectDB.instance.GetItemPrefab(item.m_shared);
            }

            return GetNormalizedPrefabName(prefab);
        }

        private static string? GetNormalizedPrefabName(GameObject? prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            string name = StripCloneSuffix(prefab.name);

            return DiscoveryValidator.IsValidItemId(name) ? name : null;
        }

        private static string StripCloneSuffix(string name)
        {
            const string cloneSuffix = "(Clone)";
            return name.EndsWith(cloneSuffix, System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - cloneSuffix.Length)
                : name;
        }
    }
}
