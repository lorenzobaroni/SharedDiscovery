using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using SharedDiscovery.Config;
using SharedDiscovery.Core.Discovery;
using UnityEngine;

namespace SharedDiscovery.Discovery
{
    internal sealed class VanillaDiscoveryService
    {
        private readonly Queue<string> _pending = new Queue<string>();
        private readonly HashSet<string> _pendingSet = new HashSet<string>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _attempts = new Dictionary<string, int>(System.StringComparer.Ordinal);
        private bool _processing;
        private int _sessionGeneration;

        public static bool IsApplyingSharedDiscovery { get; private set; }

        public static void ResetApplyingSharedDiscoveryGuard()
        {
            IsApplyingSharedDiscovery = false;
        }

        public void ClearPending()
        {
            _pending.Clear();
            _pendingSet.Clear();
            _attempts.Clear();
            _processing = false;
            _sessionGeneration++;
        }

        public void ApplyOrQueue(string itemId)
        {
            if (!ModConfig.Enabled.Value || !DiscoveryValidator.IsValidItemId(itemId))
            {
                return;
            }

            if (PlayerAlreadyKnows(itemId))
            {
                return;
            }

            if (_pendingSet.Add(itemId))
            {
                _pending.Enqueue(itemId);
                Plugin.DebugLog($"Queued remote discovery: {itemId}");
            }

            if (!_processing && Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ProcessPending(_sessionGeneration));
            }
        }

        public bool PlayerAlreadyKnows(string itemId)
        {
            Player player = Player.m_localPlayer;
            ItemDrop item = FindItemDrop(itemId);
            return player != null && item != null && player.IsMaterialKnown(item.m_itemData.m_shared.m_name);
        }

        public void ApplySnapshot(IEnumerable<string> itemIds)
        {
            foreach (string itemId in itemIds)
            {
                ApplyOrQueue(itemId);
            }
        }

        private IEnumerator ProcessPending(int generation)
        {
            _processing = true;

            while (generation == _sessionGeneration && _pending.Count > 0)
            {
                string itemId = _pending.Dequeue();
                _pendingSet.Remove(itemId);

                Plugin.DebugLog($"Applying pending remote discovery: {itemId}");
                if (!TryApply(itemId))
                {
                    if (!Plugin.Session.CanApplyRemoteDiscoveries || Player.m_localPlayer == null || MessageHud.instance == null)
                    {
                        Plugin.DebugLog($"Deferred remote discovery '{itemId}': final player not ready.");
                    }

                    int attempts = _attempts.TryGetValue(itemId, out int current) ? current + 1 : 1;
                    _attempts[itemId] = attempts;

                    if (attempts >= 30)
                    {
                        Plugin.Log.LogWarning($"Shared discovery item prefab was not available after retries: {itemId}");
                        _attempts.Remove(itemId);
                        continue;
                    }

                    _pending.Enqueue(itemId);
                    _pendingSet.Add(itemId);
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    _attempts.Remove(itemId);
                    yield return null;
                }
            }

            if (generation == _sessionGeneration)
            {
                _processing = false;
            }
        }

        private static bool TryApply(string itemId)
        {
            Player player = Player.m_localPlayer;
            if (player == null || ObjectDB.instance == null || !Plugin.Session.IsLocalPlayerReady || MessageHud.instance == null)
            {
                return false;
            }

            ItemDrop item = FindItemDrop(itemId);
            if (item == null)
            {
                return false;
            }

            string materialName = item.m_itemData.m_shared.m_name;
            if (player.IsMaterialKnown(materialName))
            {
                return true;
            }

            try
            {
                IsApplyingSharedDiscovery = true;
                player.AddKnownItem(item.m_itemData);
                Plugin.DebugLog($"Applied remote discovery through Player.AddKnownItem: {itemId}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to refresh vanilla knowledge after remote AddKnownItem '{itemId}': {ex}");
                return false;
            }
            finally
            {
                IsApplyingSharedDiscovery = false;
            }

            return true;
        }

        private static ItemDrop FindItemDrop(string itemId)
        {
            if (ObjectDB.instance == null)
            {
                return null!;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(itemId);
            if (prefab == null)
            {
                return null!;
            }

            return prefab.GetComponent<ItemDrop>();
        }
    }
}
