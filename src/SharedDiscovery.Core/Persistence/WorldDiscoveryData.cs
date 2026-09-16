using System.Collections.Generic;

namespace SharedDiscovery.Core.Persistence
{
    public sealed class WorldDiscoveryData
    {
        public int Version { get; set; } = 1;

        public List<string> Discoveries { get; set; } = new List<string>();
    }
}
