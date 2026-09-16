using System.Collections.Generic;
using SharedDiscovery.Config;
using SharedDiscovery.Core.Discovery;
using SharedDiscovery.Persistence;

namespace SharedDiscovery.Discovery
{
    internal sealed class DiscoveryService
    {
        private readonly DiscoveryRegistry _registry = new DiscoveryRegistry();
        private readonly WorldDiscoveryStoreAdapter _store;
        private string? _worldId;

        public DiscoveryService(WorldDiscoveryStoreAdapter store)
        {
            _store = store;
        }

        public IReadOnlyList<string> Snapshot => _registry.Snapshot();

        public bool IsWorldLoaded => _worldId != null;
        public bool EnsureWorldLoaded()
        {
            if (!ModConfig.Enabled.Value || ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return false;
            }

            if (!WorldContext.TryGetCurrentWorldId(out string worldId))
            {
                return false;
            }

            if (_worldId == worldId)
            {
                return true;
            }

            Plugin.DebugLog($"Loading authoritative SharedDiscovery registry for UID {worldId}.");
            _registry.ReplaceWith(_store.Load(worldId));
            _worldId = worldId;
            Plugin.Log.LogInfo($"Loaded world state '{worldId}': {_registry.Count} discoveries.");
            return true;
        }

        public void Clear()
        {
            _registry.Clear();
            _worldId = null;
        }

        public bool TryAddServerDiscovery(string itemId)
        {
            if (!ModConfig.Enabled.Value || !DiscoveryValidator.IsValidItemId(itemId) || !EnsureWorldLoaded())
            {
                return false;
            }

            if (!_registry.Add(itemId))
            {
                return false;
            }

            Plugin.DebugLog($"Server registered global discovery '{itemId}'.");
            SaveCurrentWorld();
            Plugin.Log.LogInfo($"New global discovery: {itemId}");
            return true;
        }

        public bool Contains(string itemId)
        {
            return _registry.Contains(itemId);
        }

        public void SaveCurrentWorld()
        {
            if (_worldId == null)
            {
                return;
            }

            _store.Save(_worldId, _registry.Snapshot());
            Plugin.DebugLog($"Saved world state '{_worldId}': {_registry.Count} discoveries.");
        }
    }
}
