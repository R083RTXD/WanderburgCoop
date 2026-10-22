# Development archive / Entwicklungsarchiv

**TEST / WORK IN PROGRESS — position tests only.**

**TEST / IN ENTWICKLUNG — bisher nur Positionstests.**

These are the original preserved development packages, including failed experiments.
They are not stable releases. Install only one DLL version at a time. On 2026-10-22
the user confirmed that either peer survives closing the other with 0.4.6.
Vehicle rendering in 0.5.0 is implemented but still awaits an in-game check.
The original documentation inside older ZIPs may contain superseded conclusions.

Dies sind die erhaltenen Originalpakete der Entwicklung einschließlich fehlgeschlagener
Versuche. Es sind keine stabilen Releases. Immer nur eine DLL-Version installieren.
Am 22.10.2026 hat der Nutzer das getrennte Beenden mit 0.4.6 in beiden Richtungen bestätigt.
Die Fahrzeugdarstellung in 0.5.0 ist implementiert und wartet noch auf einen Spieltest.
Anleitungen in älteren ZIPs können inzwischen widerlegte Aussagen enthalten.

| Version | Development step / Entwicklungsschritt | Original package / Originalpaket | Source snapshot / Quellcode |
| --- | --- | --- | --- |
| 0.1.0 | Loader startup / Loader-Start | Not preserved / Nicht erhalten | See changelog / Siehe Changelog |
| 0.2.0 | Local connection / Lokale Verbindung | Not preserved / Nicht erhalten | See changelog / Siehe Changelog |
| 0.3.0 | Position and cyan marker / Position und türkise Figur | Not preserved / Nicht erhalten | See changelog / Siehe Changelog |
| 0.3.1 | Background network and focus / Hintergrundnetzwerk und Fokus | [ZIP](packages/WanderburgCoop-0.3.1-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.3.1.zip) |
| 0.3.2 | Cursor re-entry / Maus-Rückkehr | [ZIP](packages/WanderburgCoop-0.3.2-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.3.2.zip) |
| 0.4.0 | Shared seed / Gemeinsamer Seed | [ZIP](packages/WanderburgCoop-0.4.0-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.0.zip) |
| 0.4.1 | First input gate / Erste Eingabesperre | [ZIP](packages/WanderburgCoop-0.4.1-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.1.zip) |
| 0.4.2 | Stale input attempt / Versuch gegen alte Eingaben | [ZIP](packages/WanderburgCoop-0.4.2-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.2.zip) |
| 0.4.3 | Button-release handling / Maustasten-Freigabe | [ZIP](packages/WanderburgCoop-0.4.3-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.3.zip) |
| 0.4.4 | Timed peer-loss guard / Zeitliche Trennungssperre | [ZIP](packages/WanderburgCoop-0.4.4-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.4.zip) |
| 0.4.5 | Input state machine / Eingabe-Zustandsregel | [ZIP](packages/WanderburgCoop-0.4.5-local-prototype.zip) | [ZIP](source/WanderburgCoop-source-0.4.5.zip) |
| 0.4.6 | Native-crash investigation / Untersuchung des nativen Absturzes | [ZIP](packages/WanderburgCoop-0.4.6-investigation.zip) | [ZIP](source/WanderburgCoop-source-0.4.6.zip) |
| 0.5.0 | Static vehicle visual test / Statischer Fahrzeug-Darstellungstest | [ZIP](packages/WanderburgCoop-0.5.0-vehicle-visual-test.zip) | [ZIP](source/WanderburgCoop-source-0.5.0.zip) |

Both instances must use 0.5.0 for its new WB05 protocol. Vehicle meshes/skin
states are cosmetic; combat, weapons, enemies and progression remain independent.
Both guides include the exact validation limits and local setup instructions.

Beide Instanzen benötigen für das neue WB05-Protokoll 0.5.0. Fahrzeugmeshes und
Ausbaustufen sind rein optisch; Kämpfe, Waffen, Gegner und Fortschritt laufen getrennt.
Beide Anleitungen beschreiben Prüfstand und Einrichtung.

Package integrity: [SHA256SUMS.txt](SHA256SUMS.txt).

The current editable source is in [../src](../src). Source ZIPs are preserved
snapshots, not reconstructed Git commit history. Original mod code is MIT licensed.
Game files and dependencies are not included.

Der aktuelle bearbeitbare Quellcode steht unter [../src](../src). Die Quellcode-ZIPs
sind erhaltene Snapshots und keine nachträglich erfundene Git-Historie.
Eigener Mod-Code steht unter MIT. Spieldateien und Abhängigkeiten sind nicht enthalten.
