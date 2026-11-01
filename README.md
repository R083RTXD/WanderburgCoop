# Wanderburg Multiplayer / Co-op Mod — WIP Position & Visual Tests

**EXPERIMENTAL DEVELOPMENT BUILD — NOT PLAYABLE MULTIPLAYER CO-OP.**

[Deutsch](README.de.md) · [Version archive](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT](LICENSE)

Unofficial mod by **R083RTXD**. Current development source: **0.6.23**, 2026-11-01.
Wanderburg0.9.14b, Windows x64, Unity6000.0.63f1, BepInEx6.0.0-be.789 IL2CPP.
Install game and loader separately. No game assets or interop files are distributed.

## Read this before testing

**0.6.22 crashes during startup. Do not use it for ordinary testing.**
0.6.23 removes the suspected native startup particle probe and all experimental
NativeArray access. **A successful game start with0.6.23 is NOT YET CONFIRMED.**
It is published at the user's request as a clearly marked development snapshot.

Exact owner-particle streaming is DISABLED. DJ musical notes are still missing
remotely; normal smoke/dust strength may differ. The ordinary cosmetic fallback,
Godmode and new edge marker remain. The marker itself still needs a visual test.
If0.6.23 still fails to start, use the preserved [0.6.20 Godmode package](versions/packages/WanderburgCoop-0.6.20-test-godmode.zip)
and report the failure before adding further changes.

## Actual test results

- Two instances on the same PC connect through UDP127.0.0.1:28714.
  Castle meshes, heading, positions and wheel/spider-leg poses are exchanged.
- User accepted spider footprint/dust visuals in0.6.18. Native wheel trails work;
  normal smoke/dust intensity differs. Locked castles remain UNTESTED.
- Godmode was observed working under fire in0.6.20 in both logs; the host showed
  161 blocked hits. Toggle with F9 or panel, individually per process. OFF at
  program start, local castle only, no revival of already dead runs.
- Either peer survives the other closing in prior tests. F8 releases the cursor.
-0.6.19 exact particle transfer FAILED: native array overloads throw Method
  unstripping failed. Managed transport tests passing did not validate that API.
-0.6.22 introduced a native startup regression. Log stops before the probe result;
  the exact failing native instruction is not proven by a complete crash stack.

## Still missing

New module meshes after upgrades, DJ/other exact effect replication, complete
nitro visuals, track texture animation and special leg effects remain pending.
A known castle may stay visible as Burg (Ausbau offen); this is a fallback.
Unknown appearances can use the cyan test marker.

Enemies, weapons, damage, health, loot, XP and progression run independently.
There is no shared authoritative combat/world. Host seed is set before loading
when already connected; complete terrain/collision equality is unverified.
Internet/LAN discovery, invite codes, NAT traversal and Steam lobbies are absent.

**Identified save-write paths are blocked. Test progress is NOT saved.
Remove the plugin for ordinary play and back up saves. No unlock changes.**

## Local setup

1. Install matching BepInEx6 Unity IL2CPP x64 and start once to generate interop.
2. For the UNCONFIRMED startup rollback, download [0.6.23 development ZIP](versions/packages/WanderburgCoop-0.6.23-startup-fix.zip).
3. Close all games. Replace BepInEx/plugins/WanderburgCoopTest.dll; only one version.
4. Start normally. Both test instances must display the same version.
5. In main menus choose Host erstellen / Lokal beitreten; wait before loading same biome.
6. Turn Godmode on as needed. Move the teammate outside view: cyan castle arrow
   should appear at the edge and disappear when back in view or disconnected.
7. Exact FX status now says Partikelstream deaktiviert. Do not treat fallback
   smoke as exact replication. User controls all windows and gameplay tests.

## Build and validation

.NET10 SDK; plugin net6, managed checks net10. Own generated interop required.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.23 compiles without warnings/errors. The managed suite covers protocol,
Godmode policy and edge geometry; it cannot establish startup or rendering safety.
[Next-session checklist](Docs/NextSession.md) records the unresolved reproduction.
[Visual roadmap](Docs/VisualReplicationRoadmap.md) describes further work.

Original mod code MIT. [Third-party notices](THIRD_PARTY_NOTICES.md).
Historical original ZIP guides may be outdated. Archive includes failed builds
for development transparency, not endorsements of stability.
