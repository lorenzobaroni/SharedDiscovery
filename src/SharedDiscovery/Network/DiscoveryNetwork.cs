using System.Collections.Generic;
using SharedDiscovery.Core.Discovery;
using SharedDiscovery.Discovery;

namespace SharedDiscovery.Network
{
    internal sealed class DiscoveryNetwork
    {
        private readonly DiscoveryService _discovery;
        private readonly VanillaDiscoveryService _vanillaDiscovery;
        private bool _registered;
        private int _sessionGeneration;

        public DiscoveryNetwork(DiscoveryService discovery, VanillaDiscoveryService vanillaDiscovery)
        {
            _discovery = discovery;
            _vanillaDiscovery = vanillaDiscovery;
        }

        public void TryRegister()
        {
            if (_registered || ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.Register<string>(DiscoveryMessages.SubmitDiscovery, RPC_SubmitDiscovery);
            ZRoutedRpc.instance.Register<string>(DiscoveryMessages.ApplyDiscovery, RPC_ApplyDiscovery);
            ZRoutedRpc.instance.Register(DiscoveryMessages.RequestSnapshot, RPC_RequestSnapshot);
            ZRoutedRpc.instance.Register<ZPackage>(DiscoveryMessages.Snapshot, RPC_Snapshot);

            _registered = true;
            Plugin.DebugLog($"Registered SharedDiscovery routed RPC handlers. Session network generation: {_sessionGeneration}.");
        }

        public void ResetSession()
        {
            _registered = false;
            _sessionGeneration++;
            Plugin.DebugLog($"Session network generation: {_sessionGeneration}.");
        }

        public void SubmitLocalDiscovery(string itemId)
        {
            if (!DiscoveryValidator.IsValidItemId(itemId) || ZRoutedRpc.instance == null)
            {
                return;
            }

            bool isServer = ZNet.instance != null && ZNet.instance.IsServer();
            if (isServer)
            {
                Plugin.DebugLog($"Sending discovery '{itemId}' to server.");
                if (_discovery.TryAddServerDiscovery(itemId))
                {
                    BroadcastDiscovery(itemId);
                }

                return;
            }

            Plugin.DebugLog($"Sending discovery '{itemId}' to server.");
            ZRoutedRpc.instance.InvokeRoutedRPC(DiscoveryMessages.SubmitDiscovery, new object[] { itemId });
        }

        public void RequestSnapshot()
        {
            if (ZRoutedRpc.instance == null || !WorldContext.TryGetCurrentWorldId(out string worldId))
            {
                return;
            }

            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                Plugin.DebugLog($"Snapshot request handled locally for world UID {worldId}.");
                SendSnapshot(0L);
                return;
            }

            Plugin.DebugLog($"Snapshot request sent for world UID {worldId}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(DiscoveryMessages.RequestSnapshot, new object[] { });
        }

        public void SendSnapshot(long peerId)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            if (!_discovery.EnsureWorldLoaded())
            {
                return;
            }

            ZPackage package = WriteSnapshot(_discovery.Snapshot);
            Plugin.DebugLog($"Sending snapshot with {_discovery.Snapshot.Count} discovery ID(s).");
            if (peerId == 0L)
            {
                RPC_Snapshot(0L, new ZPackage(package.GetArray()));
            }
            else
            {
                Plugin.DebugLog($"Snapshot response sent to peer {peerId}.");
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, DiscoveryMessages.Snapshot, new object[] { package });
            }
        }

        private void RPC_SubmitDiscovery(long sender, string itemId)
        {
            Plugin.DebugLog($"Client discovery '{itemId}' received from peer {sender}.");
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            if (!IsValidClientSender(sender))
            {
                Plugin.Log.LogWarning($"Rejected discovery from unknown peer {sender}.");
                return;
            }

            if (!DiscoveryValidator.IsValidItemId(itemId))
            {
                Plugin.Log.LogWarning($"Rejected invalid discovery from peer {sender}.");
                return;
            }

            if (!VanillaItemExists(itemId))
            {
                Plugin.Log.LogWarning($"Rejected discovery for missing item prefab from peer {sender}: {itemId}");
                return;
            }

            if (_discovery.TryAddServerDiscovery(itemId))
            {
                if (DiscoveryOriginPolicy.ShouldApplyToHost(sender, isNewDiscovery: true))
                {
                    Plugin.DebugLog($"Applying client-originated discovery '{itemId}' to host local player.");
                    _vanillaDiscovery.ApplyOrQueue(itemId);
                }

                BroadcastDiscovery(itemId);
            }
        }

        private void RPC_ApplyDiscovery(long sender, string itemId)
        {
            ReceiveRemoteDiscovery(itemId, live: true);
        }

        private void RPC_RequestSnapshot(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }

            if (!IsValidClientSender(sender))
            {
                Plugin.Log.LogWarning($"Rejected snapshot request from unknown peer {sender}.");
                return;
            }

            Plugin.DebugLog("Snapshot request received from client.");
            SendSnapshot(sender);
        }

        private void RPC_Snapshot(long sender, ZPackage package)
        {
            List<string> itemIds = ReadSnapshot(package);
            Plugin.DebugLog($"Snapshot received with {itemIds.Count} discovery ID(s).");

            foreach (string itemId in itemIds)
            {
                ReceiveRemoteDiscovery(itemId, live: false);
            }
        }

        private void ReceiveRemoteDiscovery(string itemId, bool live)
        {
            if (!DiscoveryValidator.IsValidItemId(itemId))
            {
                Plugin.Log.LogWarning($"Rejected invalid discovery {(live ? "broadcast" : "snapshot")}.");
                return;
            }

            if (live)
            {
                Plugin.DebugLog($"Live discovery RPC received: {itemId}");
            }

            _vanillaDiscovery.ApplyOrQueue(itemId);
        }

        private void BroadcastDiscovery(string itemId)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            List<ZNetPeer> remotePeers = new List<ZNetPeer>();
            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer != null && !peer.m_server && peer.m_uid != 0L)
                {
                    remotePeers.Add(peer);
                }
            }

            Plugin.DebugLog($"Broadcasting discovery '{itemId}' to {remotePeers.Count} remote peer(s).");
            foreach (ZNetPeer peer in remotePeers)
            {
                Plugin.DebugLog($"Sending live discovery '{itemId}' to peer routed ID {peer.m_uid}.");
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, DiscoveryMessages.ApplyDiscovery, new object[] { itemId });
            }
        }

        private static ZPackage WriteSnapshot(IReadOnlyList<string> itemIds)
        {
            ZPackage package = new ZPackage();
            package.Write(itemIds.Count);

            foreach (string itemId in itemIds)
            {
                package.Write(itemId);
            }

            return package;
        }

        private static List<string> ReadSnapshot(ZPackage package)
        {
            List<string> itemIds = new List<string>();
            int count;
            try
            {
                count = package.ReadInt();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Rejected unreadable shared discovery snapshot: {ex.Message}");
                return itemIds;
            }

            if (count < 0 || count > 4096)
            {
                Plugin.Log.LogWarning($"Rejected shared discovery snapshot with invalid size: {count}");
                return itemIds;
            }

            for (int i = 0; i < count; i++)
            {
                string itemId;
                try
                {
                    itemId = package.ReadString();
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"Stopped reading malformed shared discovery snapshot: {ex.Message}");
                    break;
                }

                if (DiscoveryValidator.IsValidItemId(itemId))
                {
                    itemIds.Add(itemId);
                }
            }

            return itemIds;
        }

        private static bool IsValidClientSender(long sender)
        {
            if (sender == 0L || ZRoutedRpc.instance == null)
            {
                return false;
            }

            return ZNet.instance != null && ZNet.instance.GetPeer(sender) != null;
        }

        private static bool VanillaItemExists(string itemId)
        {
            return ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(itemId) != null;
        }
    }
}
