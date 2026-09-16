using System.Globalization;
using SharedDiscovery.Core.Discovery;

namespace SharedDiscovery.Discovery
{
    internal static class WorldContext
    {
        public static bool TryGetCurrentWorldId(out string worldId)
        {
            worldId = string.Empty;

            ZNet znet = ZNet.instance;
            if (znet == null)
            {
                return false;
            }

            World world = znet.GetWorld();
            if (world == null)
            {
                return false;
            }

            long uid = znet.GetWorldUID();
            if (uid <= 0)
            {
                return false;
            }

            string candidate = uid.ToString(CultureInfo.InvariantCulture);
            if (!DiscoveryValidator.IsValidItemId(candidate))
            {
                return false;
            }

            worldId = candidate;
            return true;
        }
    }
}
