# Wanderburg Multiplayer / Co-op Mod — WIP Positions- und Darstellungstests

**TEST / IN ENTWICKLUNG — NOCH KEIN SPIELBARER MEHRSPIELER-KOOP.**

[English guide](README.md) · [Entwicklungsarchiv](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT-Lizenz](LICENSE)

Inoffizieller Mod von **R083RTXD**. Zuletzt vom Nutzer getestet: **0.6.18**,
25.10.2026. Für Wanderburg **0.9.14b**, Windows x64, Unity 6000.0.63f1 und
BepInEx 6.0.0-be.789 Unity IL2CPP. Spiel und Loader separat installieren.

## Aktueller Teststand

- Zwei Instanzen auf demselben Windows-PC verbinden sich über UDP
  `127.0.0.1:28714`. Position, Richtung, Burgmodelle und Rad-/Beinposen werden übertragen.
- Der Nutzer bestätigt in 0.6.18 funktionierende Spinnenabdrücke, Staub und Animation.
- Radspuren, Radstaub und Schornsteinqualm der normalen Burg sind sichtbar.
  Die Staub-/Qualmstärke weicht vom Original ab; das bleibt ein bekannter Fehler.
- Die grünen Musiknoten des Generals/DJs **fehlen auf der Gegenseite**.
- Originale Partikelassets werden lokal verwendet. Dies überträgt ausgewählte
  Effekte, nicht sämtliche Partikel, Animationen oder fertige Bildframes.
- Nach Schließen von Host oder Client bleibt die andere Instanz im Nutzertest an.
  Hintergrundnetzwerk übersteht Ladepausen; F8 gibt vorübergehend die Maus frei.
- Bei Verbindung vor dem Laden wird der Host-Seed vor MapGenerator.Awake gesetzt.
  Vollständig identische Gelände-/Kollisions- und Weltdaten sind nicht bestätigt.
- Normale Burg und Spinnenburg getestet; gesperrte Varianten bleiben **UNGETESTET**.
  Dafür werden keine Freischaltungen oder Spielstände verändert.

## Bekannte Grenzen und nächste Arbeit

Neue Modulmodelle werden **noch nicht synchronisiert**. Nach Ausbau bleibt die
bekannte Burg sichtbar und kann `Burg (Ausbau offen)` anzeigen. Bei unbekanntem
Anfangsmodell kann die türkise Testfigur erscheinen. Beim Nitro bleibt die Burg
sichtbar, seine Effekte fehlen jedoch teilweise. Kettentextur-Bewegung und
besondere Bein-Schritteffekte sind noch offen.

Als Nächstes: tatsächliche Effektauslösung der Besitzerburg, DJ-/Generalseffekte,
Staub-/Qualmstärke und Modulmodelle übertragen. Der
[Plan zur Effektübertragung](Docs/VisualReplicationRoadmap.md) beschreibt offene
Arbeit; sie ist **in 0.6.18 noch nicht implementiert**.

Gegner, Waffen, Angriffe, Schaden, Lebenspunkte, Beute, XP, Ausbauten und Fortschritt
laufen weiterhin getrennt. Es gibt **keine gemeinsam autoritative Welt**.
Internet-/LAN-Suche, Einladungscodes, NAT-Verbindung und Steam-Lobbys fehlen.
Dies ist ein Loopback-Prototyp, noch kein gemeinsamer Kampfmodus.

**Gefundene Spielstand-Schreibmethoden sind blockiert. Test-Fortschritt wird NICHT
gespeichert. Vor normalem Spielen den Mod entfernen und Spielstände sichern.**

## Installation und lokaler Test

1. Passendes BepInEx 6 Unity IL2CPP x64 installieren und das Spiel einmal starten,
   damit Interop-Dateien entstehen.
2. [Test-ZIP 0.6.18](versions/packages/WanderburgCoop-0.6.18-smoke-dust.zip) herunterladen.
3. Alle Spiele schließen. Nur `WanderburgCoopTest.dll` nach `BepInEx/plugins/`
   kopieren und die alte DLL ersetzen. Nur eine Mod-Version installieren.
4. Beide Spiele normal starten; **beide müssen 0.6.18 anzeigen**.
5. Im Hauptmenü **Host erstellen** und im zweiten Spiel **Lokal beitreten**.
   Verbindung abwarten, danach dieselbe Landschaft laden.
6. Burgmodelle, Räder/Beine, Abdrücke und Staub vergleichen. `Posen` zählt
   empfangene Animationsframes; `Animation: Knoten` zeigt passende Knoten.
7. F8 gibt die Maus frei. Qualm und Generalseffekte mit den bekannten Fehlern
   oben vergleichen. Keine unsichtbaren batchmode/nographics-Tests.

## Selbst bauen und Prüfstand

.NET 10 SDK installieren. Mod-Ziel: .NET 6; verwaltete Prüfungen: .NET 10.
Benötigt werden die Interop-Assemblies der eigenen Spielinstallation.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.18 kompiliert ohne Warnungen/Fehler; **133 verwaltete Prüfungen** bestanden.
Dies bestätigt weder exakte native Darstellung noch vollständiges Gameplay oder
jede Burgvariante. SHA256 der vom Nutzer getesteten installierten DLL:
`1037499D66D4308E7EFA8653DC0BE87DD1C72CF04FA6F9360147D9D8FAA46476`.

## Entwicklung und Lizenz

Das [Archiv](versions/README.md) enthält Originalpakete einschließlich
fehlgeschlagener Versuche, vorhandene Quellcode-Snapshots und Prüfsummen.
Anleitungen in alten ZIPs können überholt sein; aktuelle Ergebnisse stehen hier.
Fehlende historische Quellen werden nicht als erfundene Git-Historie rekonstruiert.

Eigener Mod-Code: MIT. [Drittanbieterhinweise](THIRD_PARTY_NOTICES.md).
Keine Spieldateien, extrahierten Grafiken, Interop-Assemblies, Spielstände,
privaten Logs oder Crashdumps enthalten.
