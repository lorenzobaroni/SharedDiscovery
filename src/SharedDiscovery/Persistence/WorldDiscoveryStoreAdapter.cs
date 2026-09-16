using System.Collections.Generic;
using System.IO;
using BepInEx;
using SharedDiscovery.Core.Persistence;

namespace SharedDiscovery.Persistence
{
    internal sealed class WorldDiscoveryStoreAdapter
    {
        private readonly WorldDiscoveryStore _store;

        public WorldDiscoveryStoreAdapter()
        {
            string worldsDirectory = Path.Combine(Paths.ConfigPath, "SharedDiscovery", "worlds");
            _store = new WorldDiscoveryStore(worldsDirectory, Plugin.Log.LogInfo, Plugin.Log.LogWarning);
        }

        public IReadOnlyList<string> Load(string worldId)
        {
            return _store.Load(worldId);
        }

        public bool Save(string worldId, IEnumerable<string> discoveries)
        {
            return _store.Save(worldId, discoveries);
        }
    }
}
