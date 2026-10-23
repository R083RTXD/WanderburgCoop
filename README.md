# Wanderburg Multiplayer / Co-op Mod — WIP Position & Visual Tests

**EXPERIMENTAL TEST — NOT PLAYABLE MULTIPLAYER CO-OP.**

**TEST / IN ENTWICKLUNG — NOCH KEIN SPIELBARER MEHRSPIELER-KOOP.**

[Deutsche Anleitung](README.de.md) · [Version archive](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT license](LICENSE)

Unofficial mod by **R083RTXD**. Latest user-tested version: **0.6.9** (2026-10-23).
Targets Wanderburg **0.9.14b**, Windows x64, Unity 6000.0.63f1 and BepInEx
6.0.0-be.789 Unity IL2CPP. Later untested work is not included.

## Verified in two instances

- Two games on the **same Windows PC** connect via UDP `127.0.0.1:28714`.
- Position, heading, castle meshes and wheel/spider-leg poses are exchanged.
  Normal and spider castles tested; other locked models remain unverified.
- Animations work after 0.6.3; 0.6.7 stopped sinking remote legs.
- Castles stay visible during nitro; **nitro VFX are not replicated**.
- After module installation, the known castle stays visible. **New module meshes
  are not synchronized**; `Burg (Ausbau offen)` means its layout may be outdated.
- Background networking survives loading stalls. Either peer survives closing
  the other in the user's test. F8 temporarily releases the cursor.
- Host seed is set before MapGenerator.Awake if connected before loading.
  Identical whole-world terrain/collision state has not been verified.

## Known failures and missing gameplay

**0.6.9 footprints are NOT correct.** Original static artwork loads, but spider
marks appear too close to the body and accumulate. The native short-lived
dust/growth animation is not reproduced. Normal wheel tracks are too long/dotted.
These are cosmetic experiments, not accurate synchronized effects.

Unknown initial appearances can still use the cyan marker. Retaining a known
castle after upgrades is a degraded visual fallback, not full module replication.
Partial matching animation-node counts are expected for that fallback.

Enemies, attacks, weapons, damage, health, loot, XP, upgrades and progression
still run independently. There is **no shared authoritative world**.
Internet/LAN discovery, invite codes, NAT traversal and Steam lobbies are absent.
This is a loopback prototype for one PC, not a playable cooperative combat mod.

**Identified game-save write paths are blocked. Test-run progress is NOT saved.
Remove the plugin before normal play. Back up saves before testing.**
No unlock changes are made. Game assets and dependencies are not distributed.

## Installation

1. Install matching BepInEx 6 Unity IL2CPP x64 and start the game once to generate
   interop assemblies. Install the game and loader separately.
2. Download [the preserved 0.6.9 test ZIP](versions/packages/WanderburgCoop-0.6.9-footprint-art.zip).
3. Close all games. Copy only `WanderburgCoopTest.dll` into `BepInEx/plugins/`,
   replacing the old DLL. Install only one version.
4. Start both games normally; **both must show 0.6.9**.
   Do not use invisible `-batchmode -nographics` startup checks.
5. Choose **Host erstellen** in one main menu and **Lokal beitreten** in the
   other. Wait for connection before loading the same biome.
6. Observe the remote castle. `Posen` counts received animation frames;
   `Animation: Knoten` shows rig matches. Footprints remain experimental.
7. F8 releases the cursor. The limitations above are known, not a finished co-op mode.

## Build

Install .NET 10 SDK. Plugin targets .NET 6; managed tests target .NET 10.
Building needs BepInEx-generated interop assemblies from your own game install.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.9 compiled without warnings/errors and passed **121 managed checks**.
Those checks do not validate native rendering, collision or full gameplay.
The build script creates an installation ZIP under `artifacts/`.

## History and license

[The archive](versions/README.md) preserves installation packages, available
source snapshots, real test outcomes and SHA256 checksums. Missing historical
sources are explicitly marked; Git history is not fabricated. Documentation
inside old ZIPs is historical and can contain superseded claims.

Original mod code: MIT. [Third-party notices](THIRD_PARTY_NOTICES.md).
No game binaries, extracted artwork, generated interop assemblies, saves,
private logs or crash dumps are included.
