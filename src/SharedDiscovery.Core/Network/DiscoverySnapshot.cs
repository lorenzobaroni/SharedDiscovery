using System.Collections.Generic;
using System.Linq;
using SharedDiscovery.Core.Discovery;

namespace SharedDiscovery.Core.Network
{
    public sealed class DiscoverySnapshot
    {
        public DiscoverySnapshot(IEnumerable<string> itemIds)
        {
            ItemIds = (itemIds ?? Enumerable.Empty<string>())
                .Where(DiscoveryValidator.IsValidItemId)
                .Distinct()
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<string> ItemIds { get; }
    }
}
