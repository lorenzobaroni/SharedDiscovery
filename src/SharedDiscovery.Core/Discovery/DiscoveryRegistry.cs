using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedDiscovery.Core.Discovery
{
    public sealed class DiscoveryRegistry
    {
        private readonly HashSet<string> _discoveries;

        public DiscoveryRegistry()
        {
            _discoveries = new HashSet<string>(StringComparer.Ordinal);
        }

        public int Count => _discoveries.Count;

        public bool Add(string itemId)
        {
            if (!DiscoveryValidator.IsValidItemId(itemId))
            {
                return false;
            }

            return _discoveries.Add(itemId);
        }

        public bool Contains(string itemId)
        {
            return DiscoveryValidator.IsValidItemId(itemId) && _discoveries.Contains(itemId);
        }

        public void ReplaceWith(IEnumerable<string> itemIds)
        {
            _discoveries.Clear();

            foreach (string itemId in itemIds ?? Enumerable.Empty<string>())
            {
                Add(itemId);
            }
        }

        public void Clear()
        {
            _discoveries.Clear();
        }

        public IReadOnlyList<string> Snapshot()
        {
            return _discoveries.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
    }
}
