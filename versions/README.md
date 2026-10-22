# Development archive / Entwicklungsarchiv

**TEST / WORK IN PROGRESS — position tests only.**

**TEST / IN ENTWICKLUNG — bisher nur Positionstests.**

These are the original preserved development packages, including failed experiments.
They are not stable releases. Install only one DLL version at a time. On 2026-10-22
the user confirmed that either peer survives closing the other with 0.4.6.
Vehicle rendering was tested: 0.5.1 has the correct castle body, but spider legs
were missing. Normal castle wheels are present. 0.5.2 adds detached leg objects;
the user confirmed the correct spider body and visible legs on 2026-10-22.
Animations are still missing. This remains a position/visual test, not playable co-op.
The original documentation inside older ZIPs may contain superseded conclusions.

Dies sind die erhaltenen Originalpakete der Entwicklung einschließlich fehlgeschlagener
Versuche. Es sind keine stabilen Releases. Immer nur eine DLL-Version installieren.
Am 22.10.2026 hat der Nutzer das getrennte Beenden mit 0.4.6 in beiden Richtungen bestätigt.
Der richtige Burgkörper wurde mit 0.5.1 bestätigt, Spinnenbeine fehlten jedoch.
Die normale Burg samt Rädern ist sichtbar. 0.5.2 ergänzt ausgelagerte Beinobjekte;
der Nutzer hat den richtigen Körper und sichtbare Beine am 22.10.2026 bestätigt.
Animationen fehlen weiterhin. Es bleibt ein Positions-/Darstellungstest ohne spielbaren Koop.
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
| 0.5.1 | Correct body matching / Richtige Körperzuordnung | [ZIP](packages/WanderburgCoop-0.5.1-body-matching-test.zip) | [ZIP](source/WanderburgCoop-source-0.5.1.zip) |
| 0.5.2 | Detached spider legs / Ausgelagerte Spinnenbeine | [ZIP](packages/WanderburgCoop-0.5.2-spider-leg-test.zip) | [ZIP](source/WanderburgCoop-source-0.5.2.zip) |

Both instances must use 0.5.2 for its WB06 appearance matching. Vehicle meshes/skin
states are cosmetic; combat, weapons, enemies and progression remain independent.
Both guides include the exact validation limits and local setup instructions.

Beide Instanzen benötigen für den WB06-Aussehensabgleich 0.5.2. Fahrzeugmeshes und
Ausbaustufen sind rein optisch; Kämpfe, Waffen, Gegner und Fortschritt laufen getrennt.
Beide Anleitungen beschreiben Prüfstand und Einrichtung.

Package integrity: [SHA256SUMS.txt](SHA256SUMS.txt).

The current editable source is in [../src](../src). Source ZIPs are preserved
snapshots, not reconstructed Git commit history. Original mod code is MIT licensed.
Game files and dependencies are not included.

Der aktuelle bearbeitbare Quellcode steht unter [../src](../src). Die Quellcode-ZIPs
sind erhaltene Snapshots und keine nachträglich erfundene Git-Historie.
Eigener Mod-Code steht unter MIT. Spieldateien und Abhängigkeiten sind nicht enthalten.
