# SharedDiscovery Testing

## Automated Tests

Run:

```powershell
dotnet run --project .\tests\SharedDiscovery.Tests\SharedDiscovery.Tests.csproj
```

Covered without Unity runtime:

- Registry duplicate handling
- Persistence save/load
- World isolation
- Snapshot filtering/sorting
- Empty snapshot
- Snapshot duplicates
- Corrupt JSON fallback
- Invalid message validation

Before runtime testing with r2modman, copy `Environment.props.example` to `Environment.props` and set:

```xml
<VALHEIM_INSTALL>Steam Valheim folder</VALHEIM_INSTALL>
<BEPINEX_PATH>r2modman profile BepInEx folder</BEPINEX_PATH>
<MOD_DEPLOYPATH>r2modman profile BepInEx\plugins\SharedDiscovery</MOD_DEPLOYPATH>
```

Then run:

```powershell
.\build.ps1 -Configuration Release -CopyToProfile
```

The first runtime test must verify whether applying remote discoveries with `Player.AddKnownItem(ItemDrop.ItemData)` produces the exact vanilla notification queue in the current Valheim build.

## Manual Runtime Tests

### Test 1 - Online Discovery

1. Install the same SharedDiscovery build on the server/host and Player A and Player B.
2. Start World A.
3. Have Player A collect Flint for the first time.
4. Expected:
   - Player A discovers Flint normally.
   - Player B receives Flint through the vanilla discovery UI.
   - Valheim unlocks only the recipes it would normally unlock.

### Test 2 - Reverse Discovery

1. Keep Player A and Player B online.
2. Have Player B discover a new item.
3. Expected: Player A receives the same vanilla discovery flow.

### Test 3 - Duplicate

1. Player A already knows Copper.
2. Player B discovers Copper.
3. Expected: Player A does not get an incorrect duplicate notification.

### Test 4 - Offline Player

1. Player A and Player B are online.
2. Player C is offline.
3. Player A discovers Iron.
4. Player C joins later.
5. Expected: Player C receives Iron automatically if not already known.

### Test 5 - Persistence

1. Discover Copper in World A.
2. Stop the server normally.
3. Start the same world again.
4. Have a new or missing-knowledge player join.
5. Expected: Copper is still part of the world's SharedDiscovery state.

### Test 6 - Different World

1. World A has Copper saved.
2. Start World B.
3. Expected: Copper is not imported into World B automatically.

### Test 7 - Veteran Character

1. Use a character that already knows Iron, Silver, and BlackMetal.
2. Join a new world.
3. Expected: the server does not import that old character knowledge into the new world's global discoveries.

### Test 8 - Local Host

1. Player A hosts with Start Server from the client.
2. Player B connects.
3. Test a discovery.
4. Expected: no duplicate persistence, duplicate broadcasts, or repeated notifications.

### Test 9 - Dedicated Server

1. Install SharedDiscovery on a dedicated server and two clients.
2. Start the dedicated server.
3. Test online and late-join discovery flows.
4. Expected: synchronization works without relying on `Player.m_localPlayer` server-side.

### Test 10 - Many Discoveries

1. Let an offline player miss at least 20 discoveries.
2. Reconnect that player.
3. Expected:
   - Missing discoveries are applied.
   - No crash occurs.
   - No network loop occurs.
   - Valheim's vanilla notification flow controls the display order.
