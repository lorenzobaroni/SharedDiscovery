using System.Collections.Generic;

namespace SharedDiscovery.Discovery
{
    internal sealed class DiscoverySessionState
    {
        private readonly HashSet<string> _restoredItemIds = new HashSet<string>(System.StringComparer.Ordinal);
        private Player? _readyPlayer;

        public bool AcceptLocalDiscoveries { get; private set; }
        public bool IsLocalPlayerReady { get; private set; }
        public bool CanApplyRemoteDiscoveries { get; private set; }

        public void CaptureInitialRestoredItems(Player player)
        {
            _restoredItemIds.Clear();

            if (player == null || player.GetInventory() == null)
            {
                return;
            }

            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                string? itemId = VanillaItemIdentifier.GetPrefabItemId(item);
                if (itemId == null || !_restoredItemIds.Add(itemId))
                {
                    continue;
                }

                Plugin.DebugLog($"Initial restored item baseline: {itemId}");
            }
        }

        public bool MarkLocalPlayerReady(Player player)
        {
            if (player == null || player != Player.m_localPlayer)
            {
                return false;
            }

            if (IsLocalPlayerReady && _readyPlayer == player)
            {
                return false;
            }

            _readyPlayer = player;
            IsLocalPlayerReady = true;
            CanApplyRemoteDiscoveries = true;
            AcceptLocalDiscoveries = true;
            Plugin.DebugLog("Local player is ready for remote discovery application.");
            return true;
        }

        public bool IsRestoredItem(string itemId)
        {
            return _restoredItemIds.Contains(itemId);
        }

        public void Reset()
        {
            AcceptLocalDiscoveries = false;
            IsLocalPlayerReady = false;
            CanApplyRemoteDiscoveries = false;
            _readyPlayer = null;
            _restoredItemIds.Clear();
            VanillaDiscoveryService.ResetApplyingSharedDiscoveryGuard();
        }
    }
}
