using BepInEx.Configuration;

namespace SharedDiscovery.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; } = null!;
        public static ConfigEntry<bool> DebugLogging { get; private set; } = null!;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable shared item discoveries for the current world.");
            DebugLogging = config.Bind("General", "DebugLogging", false, "Enable verbose SharedDiscovery debug logs.");
        }
    }
}
