using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharedDiscovery.Core.Discovery;

namespace SharedDiscovery.Core.Persistence
{
    public sealed class WorldDiscoveryStore
    {
        private const int CurrentVersion = 1;
        private readonly string _worldsDirectory;
        private readonly Action<string> _info;
        private readonly Action<string> _warning;

        public WorldDiscoveryStore(string worldsDirectory, Action<string>? info = null, Action<string>? warning = null)
        {
            _worldsDirectory = worldsDirectory ?? throw new ArgumentNullException(nameof(worldsDirectory));
            _info = info ?? (_ => { });
            _warning = warning ?? (_ => { });
        }

        public IReadOnlyList<string> Load(string worldId)
        {
            string path = GetPath(worldId);

            try
            {
                if (!File.Exists(path))
                {
                    _info($"No SharedDiscovery state exists yet for world '{worldId}'.");
                    return Array.Empty<string>();
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _warning($"SharedDiscovery state for world '{worldId}' is empty; starting with no discoveries.");
                    return Array.Empty<string>();
                }

                WorldDiscoveryData? data = JsonConvert.DeserializeObject<WorldDiscoveryData>(json);
                if (data?.Discoveries == null)
                {
                    _warning($"SharedDiscovery state for world '{worldId}' did not contain discoveries; starting empty.");
                    return Array.Empty<string>();
                }

                return data.Discoveries
                    .Where(DiscoveryValidator.IsValidItemId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (JsonException ex)
            {
                _warning($"Could not parse SharedDiscovery state for world '{worldId}': {ex.Message}");
                return Array.Empty<string>();
            }
            catch (IOException ex)
            {
                _warning($"Could not read SharedDiscovery state for world '{worldId}': {ex.Message}");
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException ex)
            {
                _warning($"Could not access SharedDiscovery state for world '{worldId}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public bool Save(string worldId, IEnumerable<string> discoveries)
        {
            string path = GetPath(worldId);

            try
            {
                Directory.CreateDirectory(_worldsDirectory);
                WorldDiscoveryData data = new WorldDiscoveryData
                {
                    Version = CurrentVersion,
                    Discoveries = discoveries
                        .Where(DiscoveryValidator.IsValidItemId)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToList()
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                return true;
            }
            catch (IOException ex)
            {
                _warning($"Could not save SharedDiscovery state for world '{worldId}': {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                _warning($"Could not access SharedDiscovery state for world '{worldId}': {ex.Message}");
                return false;
            }
        }

        private string GetPath(string worldId)
        {
            string safeWorldId = DiscoveryValidator.IsValidItemId(worldId) ? worldId : "unknown-world";
            return Path.Combine(_worldsDirectory, safeWorldId + ".json");
        }
    }
}
