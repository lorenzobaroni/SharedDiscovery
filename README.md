# SharedDiscovery

SharedDiscovery is a Valheim multiplayer mod for shared item/material discoveries per world.

When one player discovers an item or material, the discovery is sent to the server, stored as world progress, and synchronized to the other players. Each client applies the discovery through Valheim's native `Player.AddKnownItem` flow, so Valheim itself decides which recipes become available and displays the original vanilla discovery notifications.

## Features

- Shared item discoveries
- Server-authoritative progression
- Offline player sync through snapshots on join/spawn
- Per-world persistence using `ZNet.GetWorldUID()`
- Vanilla recipe logic
- Vanilla discovery notifications
- Multiplayer host and dedicated-server-oriented architecture
- r2modman and Thunderstore package structure

## Important

SharedDiscovery does not force recipes, crafting stations, boss progression, costs, or recipe visibility. It shares stable item prefab IDs and lets Valheim process recipe unlocks normally.

The mod does not import every known item from an old character when joining a new world. Only new discoveries made while connected with SharedDiscovery active, plus discoveries already saved by SharedDiscovery for that world, become shared world progress.

## Requirements

The server and every client should install the same SharedDiscovery version.

Build references required locally:

- Valheim managed assemblies
- BepInEx
- Harmony from BepInEx

SharedDiscovery uses Valheim's native `ZRoutedRpc` API.

The plugin target framework is `net462`, matching the common Valheim/BepInEx mod target. The core library also targets `net8.0` for local tests.

## Installation

For r2modman development:

1. Copy `Environment.props.example` to `Environment.props`.
2. Set `VALHEIM_INSTALL` to the Steam Valheim install folder.
3. Set `BEPINEX_PATH` to the r2modman profile's `BepInEx` folder.
4. Set `MOD_DEPLOYPATH` to the target plugin folder.
5. Run:

```powershell
.\build.ps1 -Configuration Release -CopyToProfile
```

Manual install:

```text
BepInEx/plugins/SharedDiscovery/
  SharedDiscovery.dll
  SharedDiscovery.Core.dll
```

## Build

```powershell
.\build.ps1
```

or:

```powershell
dotnet build .\SharedDiscovery.sln
```

The build script validates required local assemblies before compiling.

## Package

```powershell
.\package.ps1
```

The package ZIP is written to `dist/SharedDiscovery-0.1.0.zip`.

## Inspected Valheim APIs

The local Valheim installation inspected for this project stores the main game types in:

```text
Valheim/valheim_Data/Managed/assembly_valheim.dll
```

Relevant real APIs found:

- `Player.AddKnownItem(ItemDrop.ItemData)`
- `Player.IsMaterialKnown(string)`
- `Player.OnSpawned(bool)`
- `ObjectDB.GetItemPrefab(string)`
- `ObjectDB.GetItemPrefab(ItemDrop.ItemData.SharedData)`
- `ZNet.GetWorldUID()`
- `ZNet.GetWorldName()`
- `ZNet.IsServer()`
- `ZNet.OnWorldSaveLoaded()`
- `ZNet.OnNewConnection(ZNetPeer)`
- `ZRoutedRpc.Register<T>(string, Action<long,T>)`
- `ZRoutedRpc.Register(string, Action<long>)`
- `ZRoutedRpc.InvokeRoutedRPC(...)`
- `ZPackage.ReadInt/ReadString/Write(int)/Write(string)`

## Persistence

Server-side world state is stored under:

```text
BepInEx/config/SharedDiscovery/worlds/<WORLD_UID>.json
```

The loader tolerates missing files, empty files, duplicate entries, invalid entries, and parse/read failures without crashing the server.
