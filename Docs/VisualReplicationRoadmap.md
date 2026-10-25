# Visual replication roadmap / Plan zur Effektübertragung

**Planned work, NOT implemented in 0.6.18. / Geplante Arbeit, NICHT in 0.6.18 enthalten.**

Latest user test 2026-10-25: spider visuals work. Normal-castle dust/chimney
smoke strength differs. General/DJ musical notes are absent remotely.
Letzter Nutzertest: Spinnendarstellung funktioniert; Stärke von Radstaub und
Schornsteinqualm abweichend; grüne DJ-Musiknoten fehlen auf der Gegenseite.

## Owner-driven visual state / Darstellung durch den Besitzer

The owner of each castle should publish actual cosmetic effect activity and
emission state. Each receiving peer resolves local game artwork and renders it
on the matching castle/module anchor. This applies symmetrically to host and
client castles. It does not stream the screen or clone gameplay controllers.

Der Besitzer jeder Burg soll den tatsächlichen optischen Effektzustand senden.
Die Gegenseite nutzt ihre eigenen Spielassets am passenden Burg-/Modul-Anker.
Das gilt für beide Richtungen, nicht nur vom Host zum Client.

## Work sequence / Reihenfolge

1. Inventory runtime cosmetic emitters and authored parameters for normal/spider
   castles, commander, modules and resident variant assets, without unlock changes.
   Capture actual particle counts/emission while moving, stationary and boosting.
   Laufende Effekte erfassen; echte Ausgabe statt pauschaler Ersatzraten messen.
2. Synchronize stable effect identities, active/emitting flags, source rate and
   attachment state using bounded managed messages. Unity stays on main thread;
   no native references or game assets cross the network. Remove guessed source
   intensity where measured state is available. Synchronize both directions.
3. Reproduce the General/DJ green musical-note effect, chimney smoke, ground dust,
   nitro and module cosmetic effects using verified source components/parameters.
   DJ-Noten, Qualm, Staub, Nitro und Modul-Effekte anhand tatsächlicher Zustände.
4. Rebuild new module meshes from stable module identifiers and attachment slots;
   keep old model until replacement is ready. No new gameplay controllers, damage,
   projectile or loot logic on the remote visual representation.
5. Validate start/stop, simultaneous effects, upgrades, respawn, scene load and
   disconnect; limit live particles/bandwidth, clean up owned copies. Test known
   castles first. Locked models remain unverified until legitimately tested.

## Acceptance / Abnahme

- Source and remote smoke/dust strength, position, lifetime and movement agree.
- Musical notes appear on the correct commander and follow its real activation.
- New module appearance and purely visual effects appear on the other peer.
- No duplicated attacks, sound loops, physics, save writes or unlock changes.
- No regressions in spider poses/footfalls, normal trails or disconnect safety.
- Publish a new version only after user test; retain exact tested artifacts.

Exact per-particle replication may be necessary for effects whose random timing
or birth parameters cannot be reproduced from emitter state. Decide after capture,
not by copying every native component. Enemies/combat synchronization remains a
separate next phase. Health bars follow shared damage handling.

Eine vollständige Partikelübertragung ist eine mögliche Erweiterung, falls die
gemessenen Emitterdaten nicht ausreichen. Nicht sämtliche nativen Komponenten
kopieren. Gemeinsame Gegner/Kämpfe folgen als eigener Schritt; Lebensbalken später.
