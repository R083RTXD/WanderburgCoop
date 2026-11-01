# Wanderburg Multiplayer / Co-op Mod — WIP Positions- und Darstellungstests

**EXPERIMENTELLER ENTWICKLUNGSSTAND — NOCH KEIN SPIELBARER MULTIPLAYER-KOOP.**

[English](README.md) · [Versionsarchiv](versions/README.md) · [Changelog](CHANGELOG.md) · [MIT](LICENSE)

Inoffizieller Mod von **R083RTXD**. Aktueller Quellstand: **0.6.23**, 01.11.2026.
Wanderburg0.9.14b, Windows x64, Unity6000.0.63f1, BepInEx6.0.0-be.789 IL2CPP.
Spiel und Loader separat installieren. Keine Spielassets/Interop-Dateien enthalten.

## Vor dem Test lesen

**0.6.22 stürzt beim Start ab. Nicht für normale Tests verwenden.**
0.6.23 entfernt den verdächtigen nativen Partikel-Starttest und den experimentellen
NativeArray-Zugriff. **Erfolgreicher Spielstart mit0.6.23 ist NOCH NICHT BESTÄTIGT.**
Dieser Entwicklungsstand wird auf ausdrücklichen Nutzerwunsch veröffentlicht.

Die exakte Partikelübertragung ist DEAKTIVIERT. DJ-Musiknoten fehlen weiterhin
auf der Gegenseite; Staub-/Qualmstärke können abweichen. Bisherige Ersatzdarstellung,
Godmode und neuer Randmarker bleiben. Der Randmarker ist optisch noch ungeprüft.
Falls0.6.23 weiter beim Start abstürzt, auf das erhaltene [0.6.20-Godmode-Paket](versions/packages/WanderburgCoop-0.6.20-test-godmode.zip)
zurückgehen und erst den Startfehler klären.

## Tatsächliche Testergebnisse

- Zwei Instanzen auf demselben PC verbinden sich über UDP127.0.0.1:28714.
  Burgmodelle, Richtung, Position und Rad-/Spinnenbeinposen werden übertragen.
- Spinnenabdrücke/-staub mit0.6.18 vom Nutzer bestätigt. Native Radspuren sichtbar;
  normale Staub-/Qualmstärke abweichend. Gesperrte Burgen bleiben UNGETESTET.
- Godmode in0.6.20 unter Beschuss in beiden Logs bestätigt; Host zuletzt161
  blockierte Treffer. F9 oder Modschalter, je Instanz separat. Beim Programmstart
  AUS, nur lokale Burg, keine Wiederbelebung eines bereits verlorenen Runs.
- Getrenntes Beenden hält die andere Instanz im bisherigen Test am Leben.
  F8 gibt die Maus frei.
-0.6.19-Partikelübertragung FEHLGESCHLAGEN: native Array-Methoden werfen Method
  unstripping failed. Bestandene verwaltete Prüfungen bestätigen diese API nicht.
-0.6.22 führte den Startabsturz ein. Log endet vor dem Starttest-Ergebnis; ein
  vollständiger Crash-Stack zur exakten nativen Fehlerstelle fehlt.

## Noch offen

Neue Modulmodelle nach Ausbau, exakte DJ-/Generalseffekte, vollständige Nitro-
Effekte, Kettentextur-Bewegung und besondere Schritteffekte fehlen weiterhin.
Burg (Ausbau offen) behält lediglich das bekannte Modell. Bei unbekanntem Aussehen
kann die türkise Testfigur erscheinen.

Gegner, Waffen, Schaden, Lebenspunkte, Beute, XP und Fortschritt laufen getrennt.
Keine gemeinsam autoritative Kampf-/Spielwelt. Host-Seed wird bei vorheriger
Verbindung vor dem Laden gesetzt; identisches Gelände/Kollision nicht bestätigt.
Internet-/LAN-Suche, Einladungscodes, NAT-Verbindung und Steam-Lobbys fehlen.

**Gefundene Spielstand-Schreibpfade sind blockiert. Testfortschritt wird NICHT
gespeichert. Für normales Spielen Mod entfernen und Spielstände sichern.
Keine Freischaltungen werden verändert.**

## Lokal einrichten

1. Passendes BepInEx6 Unity IL2CPP x64 installieren; einmal für Interop starten.
2. Zum UNBESTÄTIGTEN Startkorrektur-Test [Entwicklungs-ZIP0.6.23](versions/packages/WanderburgCoop-0.6.23-startup-fix.zip) laden.
3. Alle Spiele schließen. BepInEx/plugins/WanderburgCoopTest.dll ersetzen; nur eine Version.
4. Normal starten. Beide Testinstanzen müssen dieselbe Version anzeigen.
5. Im Hauptmenü Host erstellen / Lokal beitreten; vor dem Laden gleicher Landschaft warten.
6. Godmode bei Bedarf einschalten. Andere Burg aus dem Bild fahren: türkisfarbener
   Burgpfeil soll am Rand erscheinen und bei Rückkehr/Trennung verschwinden.
7. Exakte Effektanzeige meldet Partikelstream deaktiviert. Ersatzqualm ist keine
   exakte Übertragung. Nutzer übernimmt sämtliche Fenstersteuerung und Spieltests.

## Bauen und Prüfstand

.NET10 SDK; Mod net6, verwaltete Prüfungen net10. Eigene Interop-Dateien erforderlich.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

0.6.23 kompiliert ohne Warnungen/Fehler. Verwaltete Prüfungen betreffen Protokoll,
Godmode-Regeln und Randgeometrie; sie bestätigen weder Spielstart noch Darstellung.
[Checkliste für die nächste Sitzung](Docs/NextSession.md) enthält den offenen Test.
Der [Effektplan](Docs/VisualReplicationRoadmap.md) beschreibt weitere Arbeit.

Eigener Mod-Code MIT. [Drittanbieterhinweise](THIRD_PARTY_NOTICES.md).
Anleitungen alter ZIPs können überholt sein. Fehlgeschlagene Versionen bleiben
zur Entwicklungsdokumentation erhalten, nicht als Stabilitätsempfehlung.
