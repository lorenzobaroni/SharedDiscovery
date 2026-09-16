namespace SharedDiscovery.Core.Discovery
{
    public static class DiscoveryOriginPolicy
    {
        public static bool ShouldApplyToHost(long sender, bool isNewDiscovery)
        {
            return sender > 0L && isNewDiscovery;
        }
    }
}
