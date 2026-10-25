# Wanderburg Multiplayer / Co-op Mod — WIP Position & Visual Tests

**EXPERIMENTAL TEST — NOT PLAYABLE MULTIPLAYER CO-OP.**

[Deutsche Anleitung](README.de.md) · [Development archive](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT license](LICENSE)

Unofficial mod by **R083RTXD**. Latest user-tested snapshot: **0.6.18**, 2026-10-25.
For Wanderburg **0.9.14b**, Windows x64, Unity 6000.0.63f1 and
BepInEx 6.0.0-be.789 Unity IL2CPP. Install game and loader separately.

## Current test results

- Two instances on the same Windows PC connect through UDP `127.0.0.1:28714`.
  Positions, heading, castle meshes and wheel/spider-leg poses are exchanged.
- The user reports spider footprints, dust and animation working in 0.6.18.
- Normal castle wheel trails, dust and chimney smoke are visible, but dust/smoke
  strength differs from the source castle. This remains a known visual defect.
- The commander/DJ's green musical-note effect is **missing on the remote castle**.
- Original particle assets are reused locally. This is selected-effect replay,
  not a complete stream of every particle, animation or rendered frame.
- Either peer stays running after the other closes in user testing. Background
  networking survives loading stalls; F8 temporarily releases the cursor.
- Host seed is set before MapGenerator.Awake when connected before world loading.
  Complete terrain/collision/world-state equality is not verified.
- Normal and spider castles tested. Locked variants remain **UNTESTED**.
  No unlocks, saves or progression are changed to test other castles.

## Known limitations and next work

New module meshes are **not synchronized**. The known castle is retained after
an upgrade and may show `Burg (Ausbau offen)`. Unknown initial appearances may
still use the cyan test marker. Nitro keeps the castle visible, but its effects
are not fully replicated. Track texture scrolling and special leg footsteps
remain pending.

Next visual work: synchronize actual owner-side emission/activity, commander/DJ
effects, smoke/dust intensity and module appearance. See the
[visual replication roadmap](Docs/VisualReplicationRoadmap.md). This work is
**not implemented in 0.6.18**; the roadmap is not a feature-completion claim.

Enemies, weapons, attacks, damage, health, loot, XP, upgrades and progression
still run independently. There is **no shared authoritative world**.
Internet/LAN discovery, invite codes, NAT traversal and Steam lobbies are absent.
This is a loopback prototype, not playable cooperative combat.

**Identified game-save write paths are blocked. Test-run progress is NOT saved.
Remove the plugin before normal play. Back up saves before testing.**

## Installation and local test

1. Install matching BepInEx 6 Unity IL2CPP x64; start your game once to generate interop files.
2. Download [0.6.18 test ZIP](versions/packages/WanderburgCoop-0.6.18-smoke-dust.zip).
3. Close all games. Copy only `WanderburgCoopTest.dll` to `BepInEx/plugins/`,
   replacing the old DLL. Install only one mod version.
4. Start both games normally; **both must show 0.6.18**.
5. In the main menus choose **Host erstellen** and **Lokal beitreten**.
   Wait for connection before loading the same biome.
6. Compare remote models, wheels/legs, footprints and dust. `Posen` counts
   received animation frames; `Animation: Knoten` shows matched rig nodes.
7. F8 releases the cursor. Compare smoke and commander effects against the known
   issues above. Do not use invisible batchmode/nographics tests.

## Build and validation

Install .NET 10 SDK. Plugin targets .NET 6; managed checks target .NET 10.
Use BepInEx-generated interop assemblies from your own installed game.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.18 compiled with zero warnings/errors and passed **133 managed checks**.
These checks do not establish native visual fidelity, full gameplay or support
for every castle. The installed DLL tested by the user has SHA256:
`1037499D66D4308E7EFA8653DC0BE87DD1C72CF04FA6F9360147D9D8FAA46476`.

## History and license

The [archive](versions/README.md) preserves original packages, including failed
experiments, available source snapshots and checksums. Historical ZIP guides
contain superseded claims; use this README for current results. Missing source
snapshots are not reconstructed as invented Git history.

Original mod code: MIT. [Third-party notices](THIRD_PARTY_NOTICES.md).
No game binaries, extracted artwork, generated interop assemblies, saves,
private logs or crash dumps are distributed.
