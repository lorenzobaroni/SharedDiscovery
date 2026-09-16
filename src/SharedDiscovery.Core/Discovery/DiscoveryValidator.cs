namespace SharedDiscovery.Core.Discovery
{
    public static class DiscoveryValidator
    {
        public const int MaxItemIdLength = 128;

        public static bool IsValidItemId(string? itemId)
        {
            if (itemId == null || itemId.Trim().Length == 0)
            {
                return false;
            }

            string value = itemId;

            if (value.Length > MaxItemIdLength)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
