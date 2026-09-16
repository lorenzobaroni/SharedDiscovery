using System;
using System.IO;
using System.Linq;
using SharedDiscovery.Core.Discovery;
using SharedDiscovery.Core.Network;
using SharedDiscovery.Core.Persistence;

namespace SharedDiscovery.Tests
{
    internal static class Program
    {
        private static int _passed;
        private static int _failed;

        private static int Main()
        {
            Run("Registry rejects duplicate discoveries", RegistryRejectsDuplicates);
            Run("Persistence saves and loads discoveries", PersistenceRoundTrip);
            Run("Persistence isolates worlds", PersistenceIsolatesWorlds);
            Run("Snapshot filters and sorts entries", SnapshotFiltersAndSorts);
            Run("Empty snapshot is valid", EmptySnapshotIsValid);
            Run("Snapshot removes duplicates", SnapshotRemovesDuplicates);
            Run("Corrupt persistence file loads empty", CorruptPersistenceLoadsEmpty);
            Run("Validation rejects invalid messages", ValidationRejectsInvalidMessages);

            Console.WriteLine($"{_passed} passed, {_failed} failed");
            return _failed == 0 ? 0 : 1;
        }

        private static void RegistryRejectsDuplicates()
        {
            DiscoveryRegistry registry = new DiscoveryRegistry();

            Assert(registry.Add("Copper"), "First Copper add should be accepted.");
            Assert(!registry.Add("Copper"), "Second Copper add should be rejected.");
            Assert(registry.Count == 1, "Registry should contain one discovery.");
        }

        private static void PersistenceRoundTrip()
        {
            string dir = CreateTempDirectory();
            WorldDiscoveryStore store = new WorldDiscoveryStore(dir);

            Assert(store.Save("WorldA", new[] { "Wood", "Copper", "Copper" }), "Save should succeed.");
            string[] loaded = store.Load("WorldA").ToArray();

            Assert(loaded.SequenceEqual(new[] { "Copper", "Wood" }), "Loaded discoveries should be distinct and sorted.");
        }

        private static void PersistenceIsolatesWorlds()
        {
            string dir = CreateTempDirectory();
            WorldDiscoveryStore store = new WorldDiscoveryStore(dir);

            store.Save("WorldA", new[] { "Copper" });
            store.Save("WorldB", new[] { "Iron" });

            Assert(store.Load("WorldA").SequenceEqual(new[] { "Copper" }), "WorldA should only contain Copper.");
            Assert(store.Load("WorldB").SequenceEqual(new[] { "Iron" }), "WorldB should only contain Iron.");
        }

        private static void SnapshotFiltersAndSorts()
        {
            DiscoverySnapshot snapshot = new DiscoverySnapshot(new[] { "Stone", "", "Wood", "Copper", "Wood", "Bad/Id" });

            Assert(snapshot.ItemIds.SequenceEqual(new[] { "Copper", "Stone", "Wood" }), "Snapshot should filter invalid entries and sort valid IDs.");
        }

        private static void EmptySnapshotIsValid()
        {
            DiscoverySnapshot snapshot = new DiscoverySnapshot(Array.Empty<string>());

            Assert(snapshot.ItemIds.Count == 0, "Empty snapshot should contain no entries.");
        }

        private static void SnapshotRemovesDuplicates()
        {
            DiscoverySnapshot snapshot = new DiscoverySnapshot(new[] { "Wood", "Wood", "Stone", "Stone" });

            Assert(snapshot.ItemIds.SequenceEqual(new[] { "Stone", "Wood" }), "Snapshot should remove duplicates.");
        }

        private static void CorruptPersistenceLoadsEmpty()
        {
            string dir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(dir, "WorldA.json"), "{ not json");

            WorldDiscoveryStore store = new WorldDiscoveryStore(dir);

            Assert(store.Load("WorldA").Count == 0, "Corrupt JSON should load as empty.");
        }

        private static void ValidationRejectsInvalidMessages()
        {
            Assert(!DiscoveryValidator.IsValidItemId(null), "Null should be invalid.");
            Assert(!DiscoveryValidator.IsValidItemId(""), "Empty should be invalid.");
            Assert(!DiscoveryValidator.IsValidItemId("Copper Ore"), "Spaces should be invalid.");
            Assert(!DiscoveryValidator.IsValidItemId(new string('A', DiscoveryValidator.MaxItemIdLength + 1)), "Overlong IDs should be invalid.");
            Assert(DiscoveryValidator.IsValidItemId("Copper_Ore-01.Modded"), "Stable prefab-like IDs should be valid.");
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine($"FAIL {name}: {ex.Message}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "SharedDiscovery.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
