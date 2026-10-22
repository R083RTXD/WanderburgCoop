# Wanderburg Multiplayer / Co-op Mod — WIP Position Tests

**TEST / IN ENTWICKLUNG — bisher nur Positionstests. Noch kein spielbarer Mehrspieler-Koop.**

**TEST / WORK IN PROGRESS — position tests only. Not playable multiplayer co-op.**

[English guide](README.md) · [Versionsarchiv](versions/README.md) · [Entwicklungsverlauf](CHANGELOG.md) · [MIT-Lizenz](LICENSE)

Ein inoffizieller, experimenteller BepInEx-Mod für Wanderburg von **R083RTXD**.
Das Repository zeigt die Entwicklung einschließlich fehlgeschlagener Versuche.
Aktueller Fahrzeug-Darstellungstest: **0.5.2**, für Wanderburg **0.9.14b**, Windows x64,
Unity 6000.0.63f1 und BepInEx 6.0.0-be.789 (Unity IL2CPP).

## Was bisher funktioniert

- Zwei Instanzen auf **demselben Windows-PC** verbinden sich über UDP `127.0.0.1:28714`.
- Position und Blickrichtung werden übertragen. 0.5.2 versucht, die Fahrzeugmeshes
  und aktiven Ausbaustufen des Gegenübers darzustellen; die türkise Figur bleibt als Fallback.
- Die Bewegung der Testfigur wurde in beiden Spielinstanzen manuell bestätigt.
- Der Host überträgt einen gemeinsamen Karten-Seed; gleiche Seeds wurden manuell bestätigt.
- Ein Hintergrund-Thread hält die Netzwerkverbindung auch während Unity-Ladepausen aufrecht.
- F8 gibt die Maus vorübergehend zum Fensterwechsel frei.

Ein gleicher Seed bedeutet **keine gemeinsam simulierte Welt**.
Fahrzeug-Spielmechanik, Gegner, Kämpfe, Schaden, Beute, XP, Upgrades
und gemeinsamer Fortschritt sind **noch nicht synchronisiert**.
Internetverbindungen, LAN-Suche, Einladungscodes, NAT-Durchquerung und
Steam-Lobbys sind **nicht implementiert**. Die Tests laufen lokal auf einem PC.

## Bekannte Fehler und Prüfstand

Am 22.10.2026 hat der Nutzer bestätigt: Mit 0.4.6 läuft nach dem Schließen von
Host oder Client die andere Instanz weiter. 0.5.2 behält diesen Fokusschutz bei.

**0.5.0 zeigte einen normalen Burgkörper mit Spinnenbeinen.** 0.5.2 überträgt
zusätzlich eine Kennung der sichtbaren Meshes/Materialien und verwendet nur ein
passendes laufendes oder geladenes Modell. Der richtige Körper wurde mit 0.5.1
vom Nutzer bestätigt; dort fehlten die ausgelagerten Spinnenbeine, während die
normale Burg samt Rädern sichtbar war. 0.5.2 übernimmt zusätzlich die zugehörigen
Beinobjekte. Am 22.10.2026 hat der Nutzer bestätigt: Der richtige Spinnenburgkörper
und die Beine sind jetzt sichtbar und funktionieren im Test mit zwei Instanzen.
Die Beine bleiben statisch; Animationen sind noch nicht implementiert.
Fehlt ein passendes Aussehen, bleibt die türkise Figur.
Es werden
nur statische Meshes und Materialien übernommen. Räder, Waffenanimationen,
angebaute Module, Besatzung und Dekoration werden nicht separat synchronisiert.
Für unbekannte Modelle bleibt die Testfigur sichtbar. Beide Instanzen müssen
0.5.2 für übereinstimmende Körper-/Beinkennungen nutzen. WB06 ist nicht mit
WB04/WB05 kompatibel. Die Zustandsmaske unterstützt
die ersten 64 Einträge der Ausbaustufen-Liste eines Fahrzeugs.

Der Mod kompiliert ohne Fehler/Warnungen. 67 verwaltete Netzwerk- und Regeltests
bestehen, einschließlich Modellkennung, Stufenwechsel und Abweisung alter Protokolle.
Diese Tests prüfen weder Unity-Darstellung noch die native Spielausführung.

Der Entwicklungsmod blockiert die gefundenen Spielstand-Schreibmethoden.
**Test-Fortschritt wird nicht gespeichert. Für normales Spielen den Mod entfernen.**
Alte Pakete und ihre Anleitungen bleiben als historische Stände erhalten.
Frühere Aussagen zur Absturzbehebung sind durch diesen Prüfstand überholt.

## Installation und lokaler Positionstest

1. Passendes **BepInEx 6 Unity IL2CPP x64** installieren und das Spiel einmal
   starten, damit die Interop-Dateien erzeugt werden.
2. Ein Test-ZIP aus dem [Versionsarchiv](versions/README.md) herunterladen.
3. Alle Spielinstanzen schließen und nur `WanderburgCoopTest.dll` nach
   `BepInEx/plugins/` kopieren. Alte Kopie ersetzen; keine Versionen parallel installieren.
4. Zwei Spielinstanzen normal starten. Im aktuellen Fenster muss **0.5.2** stehen.
   Keine unsichtbaren Starttests mit `-batchmode -nographics` verwenden.
5. Im Hauptmenü einmal **Host erstellen** und in der anderen Instanz
   **Lokal beitreten** wählen. Die Verbindung abwarten.
6. In beiden Instanzen dasselbe Biom starten. **Vor** dem Laden verbinden,
   damit der gemeinsame Seed vor der Kartenerzeugung feststeht.
7. Nach der türkisen Testfigur schauen. F8 gibt die Maus vorübergehend frei.

Spiel, BepInEx und Abhängigkeiten müssen separat installiert werden.
Das Repository enthält keine Spieldateien, extrahierten Assets, erzeugten
Spiel-Assemblies, Spielstände, privaten Logs oder Crashdumps.

## Selbst bauen

.NET 10 SDK installieren. Der Mod verwendet .NET 6, die reine Testsuite .NET 10.
Zum Kompilieren des Mods wird eine lokale Spielinstallation mit den von
BepInEx erzeugten Interop-Dateien benötigt.

```powershell
./build.ps1 -GameRoot 'D:/SteamLibrary/steamapps/common/Wanderburg Game'
```

Nur die verwalteten Tests ausführen, ohne Spielinstallation:

```powershell
dotnet run --project ./tests/ConnectionChecks.csproj -c Release
```

Das Buildskript erzeugt ein Installations-ZIP unter `artifacts/`.
Keine Spiel-Assemblies oder extrahierten Inhalte hochladen.

## Entwicklungsarchiv und Mitarbeit

Das [Versionsarchiv](versions/README.md) enthält alle erhaltenen Installationspakete
und Quellcode-Snapshots. 0.1.0–0.3.0 sind im Changelog dokumentiert;
vollständige Originalarchive dieser Versionen liegen nicht mehr vor.

Fehlerberichte und Codebeiträge sind willkommen. Spiel-, Loader- und Mod-Version,
die Schritte zum Nachstellen und die betroffene Rolle (Host/Client) angeben.
Persönliche Pfade vor dem Teilen von Logauszügen entfernen.

## Lizenz

Eigener Mod-Code: [MIT](LICENSE). Spielinhalte, Marken und separat installierte
Abhängigkeiten unterliegen ihren eigenen Bedingungen; siehe
[Dritthinweise](THIRD_PARTY_NOTICES.md). Das Projekt wird nicht offiziell
von den Spieleentwicklern unterstützt.
