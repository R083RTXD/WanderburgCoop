# WanderburgCoop

**TEST / WORK IN PROGRESS — position tests only. Not playable multiplayer co-op.**

**TEST / IN ENTWICKLUNG — bisher nur Positionstests. Noch kein spielbarer Mehrspieler-Koop.**

[Deutsche Anleitung](README.de.md) · [Version archive / Versionsarchiv](versions/README.md) · [Development history](CHANGELOG.md) · [MIT license](LICENSE)

An unofficial, experimental BepInEx mod for Wanderburg by **R083RTXD**.
This repository shares the development process, including unsuccessful experiments.
Current investigation build: **0.4.6**, targeting Wanderburg **0.9.14b**, Windows x64,
Unity 6000.0.63f1 and BepInEx 6.0.0-be.789 (Unity IL2CPP).

## What currently works

- Two instances on the **same Windows PC** connect using UDP `127.0.0.1:28714`.
- Player position and heading are exchanged and displayed as a small cyan test marker.
- Position movement was verified manually in both game instances.
- A host-provided map seed is sent to both instances; matching seeds were verified manually.
- Networking runs on a background worker so Unity loading stalls do not stop heartbeats.
- F8 temporarily releases the cursor for switching windows.

The matching seed does **not** mean both players share one authoritative world.
Real castle/vehicle replication, synchronized enemies, combat, damage, loot,
XP, upgrades and shared progression are **not implemented**.
Internet connections, LAN discovery, invitation codes, NAT traversal and Steam
lobbies are **not implemented**. Current tests use loopback on one computer.

## Known problems and test status

**The client can crash shortly after the host closes. This is unresolved.**
Windows recorded a native `GameAssembly.dll` crash. Earlier attempts to explain
it as an inherited quit-button click were not confirmed; the shared Unity log
could not reliably identify which process called quit.

Build 0.4.6 keeps focus-loss protection active after disconnect and logs every
`GM.CloseApplication` request with its process ID. This is an investigation
change awaiting an in-game result, **not a confirmed fix**.

The 0.4.6 plugin compiled with zero errors and warnings. The managed transport
and policy suite most recently passed 53 checks on 0.4.5. Those checks do not
validate the native game runtime or prove the disconnect crash is fixed.

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
4. Start two game instances normally. The current panel should show **0.4.6**.
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
