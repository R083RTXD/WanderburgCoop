# Wanderburg Multiplayer / Co-op Mod — WIP Position Tests

**TEST / WORK IN PROGRESS — position tests only. Not playable multiplayer co-op.**

**TEST / IN ENTWICKLUNG — bisher nur Positionstests. Noch kein spielbarer Mehrspieler-Koop.**

[Deutsche Anleitung](README.de.md) · [Version archive / Versionsarchiv](versions/README.md) · [Development history](CHANGELOG.md) · [MIT license](LICENSE)

An unofficial, experimental BepInEx mod for Wanderburg by **R083RTXD**.
This repository shares the development process, including unsuccessful experiments.
Current vehicle-visual test build: **0.5.0**, targeting Wanderburg **0.9.14b**, Windows x64,
Unity 6000.0.63f1 and BepInEx 6.0.0-be.789 (Unity IL2CPP).

## What currently works

- Two instances on the **same Windows PC** connect using UDP `127.0.0.1:28714`.
- Player position and heading are exchanged. Version 0.5.0 attempts to display
  the remote vehicle's meshes and active skin stages; the cyan marker remains a fallback.
- Position movement was verified manually in both game instances.
- A host-provided map seed is sent to both instances; matching seeds were verified manually.
- Networking runs on a background worker so Unity loading stalls do not stop heartbeats.
- F8 temporarily releases the cursor for switching windows.

The matching seed does **not** mean both players share one authoritative world.
Vehicle gameplay replication, synchronized enemies, combat, damage, loot,
XP, upgrades and shared progression are **not implemented**.
Internet connections, LAN discovery, invitation codes, NAT traversal and Steam
lobbies are **not implemented**. Current tests use loopback on one computer.

## Known problems and test status

On 2026-10-22 the user confirmed that with 0.4.6 either peer keeps running when
the other closes. Version 0.5.0 retains that focus protection.

**Vehicle rendering in 0.5.0 still requires an in-game check.** Only static
meshes/materials are copied. Wheels, weapon animation, attached modules, crew
and cosmetics are not separately replicated. Unknown models retain the marker.
Both peers must use 0.5.0: the WB05 visual protocol is incompatible with WB04.
The skin mask supports the first 64 entries in a vehicle's skin-element array.

The plugin compiles without errors/warnings. 61 managed transport and policy
checks pass, including vehicle identity, skin changes and old-protocol rejection.
These checks do not validate Unity rendering or native game behavior.

The development plugin blocks identified game-save write paths.
**Test-run progress is not saved. Remove the plugin before normal play.**
Old packages and their bundled documentation are historical records; their
earlier claims about crash fixes are superseded by this status.

## Installation and local position test

1. Install the matching **BepInEx 6 Unity IL2CPP x64** loader and start the game
   once to generate its interop assemblies.
2. Download a test ZIP from the [version archive](versions/README.md).
3. Close every game instance and copy only `WanderburgCoopTest.dll` into
   `BepInEx/plugins/`. Replace the existing copy; do not install multiple versions.
4. Start two game instances normally. The current panel should show **0.5.0**.
   Do not use hidden `-batchmode -nographics` startup checks.
5. In the main menu, select **Host erstellen** in one instance and
   **Lokal beitreten** in the other. Wait for the connection.
6. Enter the same biome in both instances. Connect **before** map generation
   so both can use the negotiated seed.
7. Look for the cyan test marker. F8 releases the mouse temporarily.

The game, BepInEx and its dependencies must be installed separately.
The repository contains no game binaries, extracted assets, generated game
assemblies, saves, private logs or crash dumps.

## Build from source

Install the .NET 10 SDK. The plugin targets .NET 6; the managed test harness
targets .NET 10. Compiling the plugin requires a local game installation with
BepInEx-generated interop assemblies.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
```

To run only the managed checks, without a game installation:

```powershell
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

The build script creates an installation ZIP under `artifacts/`.
Do not add game assemblies or extracted content to the repository.

## Development archive and contributions

[The archive](versions/README.md) contains all preserved installation packages
and source snapshots. Versions 0.1.0–0.3.0 are documented in the changelog;
complete original archives for those versions are unavailable.

Bug reports and code contributions are welcome. Include the game, loader and
mod versions, reproduction steps, and whether you were testing host or client.
Remove personal paths before sharing log excerpts.

## License

Original mod code: [MIT](LICENSE). Game content, trademarks and separately
installed dependencies have their own terms; see [third-party notices](THIRD_PARTY_NOTICES.md).
This is an unofficial community project, not affiliated with or endorsed by the game developers.
