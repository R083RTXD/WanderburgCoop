# Wanderburg Multiplayer / Co-op Mod — WIP Positions- und Darstellungstests

**TEST / IN ENTWICKLUNG — NOCH KEIN SPIELBARER MEHRSPIELER-KOOP.**

[English guide](README.md) · [Versionsarchiv](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT-Lizenz](LICENSE)

Inoffizieller Mod von **R083RTXD**. Zuletzt vom Nutzer getestet: **0.6.9**
(23.10.2026). Für Wanderburg **0.9.14b**, Windows x64, Unity 6000.0.63f1 und
BepInEx 6.0.0-be.789 Unity IL2CPP. Spätere ungetestete Arbeiten sind nicht enthalten.

## Mit zwei Instanzen bestätigt

- Verbindung auf **demselben Windows-PC** per UDP `127.0.0.1:28714`.
- Position, Richtung, Burgdarstellung und Rad-/Spinnenbeinposen werden übertragen.
  Normale Burg und Spinnenburg getestet; andere gesperrte Modelle sind ungeprüft.
- Animationen funktionieren seit 0.6.3; mit 0.6.7 sinken entfernte Beine nicht mehr ein.
- Burg bleibt beim Nitro sichtbar; **Nitro-Effekte fehlen weiterhin**.
- Nach Moduleinbau bleibt die bekannte Burg sichtbar. **Neue Modulmodelle fehlen**.
  `Burg (Ausbau offen)` bedeutet, dass das Aussehen veraltet sein kann.
- Hintergrundnetzwerk übersteht Ladepausen. Nach Schließen von Host oder Client
  bleibt die andere Instanz im Nutzertest an. F8 gibt die Maus frei.
- Host-Seed wird bei Verbindung vor dem Laden vor MapGenerator.Awake gesetzt.
  Identische gesamte Gelände-/Kollisionsdaten sind damit noch nicht bestätigt.

## Bekannte Fehler und fehlende Spielmechanik

**Spuren in 0.6.9 sind NICHT korrekt.** Originalgrafik wird geladen, aber
Spinnenabdrücke sitzen zu nah am Körper und sammeln sich an. Der kurze Staub-/
Wachstumseffekt des Originals wird nicht nachgebildet. Normale Radspuren sind
zu lang und gepunktet. Das sind rein optische Versuche.

Bei unbekanntem Anfangsmodell bleibt gegebenenfalls die türkise Figur.
Das Behalten einer bekannten Burg beim Ausbau ist ein Ersatzverfahren, keine
vollständige Modulsynchronisierung. Teilweise passende Animationsknoten sind dabei bekannt.

Gegner, Angriffe, Waffen, Schaden, Lebenspunkte, Beute, XP, Ausbauten und Fortschritt
laufen getrennt. Es gibt **keine gemeinsam autoritative Welt**.
Internet-/LAN-Suche, Einladungscodes, NAT-Verbindung und Steam-Lobbys fehlen.
Aktuell ist dies ein Loopback-Prototyp für einen PC, noch kein gemeinsamer Kampfmodus.

**Gefundene Spielstand-Schreibmethoden sind blockiert. Test-Fortschritt wird
NICHT gespeichert. Vor normalem Spielen den Mod entfernen und Spielstände sichern.**
Keine Freischaltungen werden verändert. Spielassets/Abhängigkeiten werden nicht mitgeliefert.

## Installation

1. Passendes BepInEx 6 Unity IL2CPP x64 installieren und das Spiel einmal starten,
   damit Interop-Dateien entstehen. Spiel und Loader separat installieren.
2. [Erhaltenes Test-ZIP 0.6.9](versions/packages/WanderburgCoop-0.6.9-footprint-art.zip) herunterladen.
3. Alle Spiele schließen. Nur `WanderburgCoopTest.dll` nach `BepInEx/plugins/`
   kopieren und alte DLL ersetzen. Nur eine Version installieren.
4. Beide Spiele normal starten; **beide müssen 0.6.9 anzeigen**.
   Keine unsichtbaren `-batchmode -nographics`-Starttests verwenden.
5. Im Hauptmenü einmal **Host erstellen**, im zweiten Spiel **Lokal beitreten**.
   Verbindung abwarten, danach dieselbe Landschaft laden.
6. Entfernte Burg beobachten. `Posen` zählt Animationsframes;
   `Animation: Knoten` zeigt zugeordnete Knoten. Spuren bleiben experimentell.
7. F8 gibt die Maus frei. Die obigen Grenzen sind bekannt; dies ist kein fertiger Koop.

## Selbst bauen

.NET 10 SDK installieren. Mod-Ziel: .NET 6; verwaltete Tests: .NET 10.
Kompilierung benötigt Interop-Assemblies der eigenen Spielinstallation.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.9 kompiliert ohne Warnungen/Fehler; **121 verwaltete Prüfungen** bestehen.
Sie prüfen weder native Darstellung noch Kollision oder komplette Spielmechanik.
Das Buildskript erstellt ein Installations-ZIP unter `artifacts/`.

## Entwicklung und Lizenz

[Das Archiv](versions/README.md) enthält erhaltene Installationspakete, verfügbare
Quellcode-Snapshots, tatsächliche Testergebnisse und SHA256-Prüfsummen. Fehlende
historische Quellen werden benannt; Git-Historie wird nicht erfunden. Anleitungen
in alten ZIPs sind historische Stände und können überholte Aussagen enthalten.

Eigener Mod-Code: MIT. [Drittanbieterhinweise](THIRD_PARTY_NOTICES.md).
Keine Spieldateien, extrahierten Grafiken, Interop-Assemblies, Spielstände,
privaten Logs oder Crashdumps enthalten.
