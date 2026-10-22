# Changelog

> **TEST / WORK IN PROGRESS — position tests only.**
> **TEST / IN ENTWICKLUNG — bisher nur Positionstests.**
>
> Historical entries describe attempted changes, not proven runtime fixes.
> Versions 0.4.0–0.4.5 did not resolve the client crash after host exit.
> Windows crash dumps confirmed a native client failure; earlier explanations
> based on shared Player.log output were unreliable. On 2026-10-22 the user
> confirmed that 0.4.6 keeps either surviving peer running after the other closes.
>
> Ältere Einträge dokumentieren Versuche, keine bestätigten Laufzeitbehebungen.
> 0.4.0–0.4.5 haben den Client-Absturz nach Host-Ende nicht behoben.
> Windows-Crashdumps bestätigen einen nativen Client-Absturz. Frühere Aussagen
> aus dem gemeinsamen Player.log waren nicht zuverlässig. Am 22.10.2026 hat der
> Nutzer das getrennte Beenden in beiden Richtungen mit 0.4.6 bestätigt.

## 0.5.2 — 2026-10-22 — detached spider leg test

- User confirmed correct spider body in 0.5.1 but missing legs; normal castle
  body and wheels are visible. Spider controllers maintain detached leg objects.
- Include owned SpiderLegController.legObjs and SpiderLegController2.myLegsObjParents
  in the appearance fingerprint and render-only copy, with renderer deduplication.
- Rebase detached leg roots from world space into the vehicle's visual frame.
- Preserve body selection, focus protection and static-only rendering. No leg
  controllers, IK scripts, damage, sound or physics components are cloned.
- WB06 packet layout unchanged, but both instances must use 0.5.2 for matching
  appearance fingerprints.
- Manual result on 2026-10-22: user confirmed the correct spider castle body
  and visible legs; the two-instance visual test works. Animation remains absent.
- Manueller Test am 22.10.2026: richtiger Spinnenburgkörper und sichtbare Beine
  vom Nutzer bestätigt. Darstellungstest funktioniert; Animationen fehlen noch.

## 0.5.1 — 2026-10-22 — castle body matching test

- 0.5.0 was observed to show the default castle body with correct spider legs
  even when both players selected the spider castle. Both logs used PV_Spider_0.
- Add a stable appearance fingerprint of visible mesh branches and materials.
- Prefer the live local vehicle when its appearance matches the peer exactly;
  otherwise require an appearance-matched resident vehicle or keep the marker.
- Stop applying the live skin-index mask to a potentially different prefab layout.
- Use WB06 so old clients without body identity cannot silently select a model.
- 67 managed checks pass; the original two-spider-castle reproduction still
  requires an in-game check. Legs remain static; this does not add animation.

## 0.5.0 — 2026-10-22 — vehicle visual test

- Extend protocol to WB05 with stable vehicle identity and a 64-bit skin-state mask.
- Render remote vehicles from resident game meshes/materials using empty visual
  objects only; no prefab instantiation, gameplay scripts, physics or audio.
- Rebuild the visual on vehicle/skin changes; retain the cyan marker as fallback.
- Sample position from the vehicle's visual origin and retain movement smoothing.
- Preserve the 0.4.6 focus behavior, which the user confirmed survives closing
  either peer. Visible vehicle rendering still requires an in-game test.
- Weapons, wheel animation, attached modules, enemies, combat and progression
  remain unsynchronized. Static vehicle appearance only, not playable co-op.

## 0.4.6 — investigation build

- Keep focus-loss protection enabled after a network session ends.
- Log every GM.CloseApplication request with process ID and policy result.
- Native Windows crash evidence invalidates earlier claims that the surviving
  client only exited normally. The underlying native crash remains under investigation.

## 0.4.5 — 2026-10-20

- Replace the peer-loss timer with a deterministic input state machine.
- After peer loss, require a clean focused frame followed by a new local press
  before the surviving process may close.
- Reject delayed inherited button releases regardless of their delay.

## 0.4.4 — 2026-10-20

- Suppress the close path for 1.5 seconds only in the surviving peer after a
  connected session reports peer loss.
- Keep the host's deliberate quit input unaffected.
- Add regression coverage for fresh inherited input during peer-loss grace.

## 0.4.3 — 2026-10-20

- Accept a fresh mouse-button release as the game's legitimate quit-button
  confirmation while retaining the focus-transfer and stale-input guards.
- Use the same confirmation definition when arming the clean-focus gate.

## 0.4.2 — 2026-10-20

- Do not retain close permission from an earlier mouse or keyboard input.
- Require the confirmation input to still be fresh when `CloseApplication` runs.
- Add a regression check for stale focused input after peer shutdown.

## 0.4.1 — 2026-10-20

- Require a fresh confirmation input from the focused process before allowing
  the game's `CloseApplication` path.
- Prevent automatic Windows focus transfer after a peer exits from reusing the
  departing peer's mouse click in the surviving instance.
- Preserve the shared world-seed protocol and position test from 0.4.0.

## 0.4.0 — 2026-10-20

- Add a host-authoritative world seed to local protocol `WB04`.
- Apply that seed before `MapGenerator.Awake` on both connected instances.
- Display the negotiated seed in the in-game test panel.
- Prevent an unfocused instance from processing another window's quit-button click.
- Require connection in the main menu before loading the biome for this test.
- Keep enemies, combat, loot and progression independent for now.

## 0.3.2 — 2026-10-20

- Make F8 cursor release an explicit temporary window-switching mode.
- Stop writing cursor state while the game window is unfocused.
- Automatically restore the previous cursor state when the game regains focus.
- Add focus-transition logging for manual crash verification.

## 0.3.1 — 2026-10-19

- Move socket ownership and heartbeat processing to a managed background thread.
- Keep connections alive during simulated Unity loading/focus stalls.
- Stop advertising frozen Unity samples as live players until sampling resumes.
- Re-assert background execution after the game's boot background override ends.
- Bypass focus-specific auto-pause decisions while the local test session is active.
- Add F8 cursor-release toggle; keep the toggle useful after failed joins.
- Disable Windows UDP port-unreachable reset notifications on the local socket.
- Add regression coverage for late hosts and a 6.5-second main-thread stall.
- Keep test save protection enabled. In-game focus/cursor retesting is pending.

## 0.3.0

- Two-way player position/yaw snapshots and cyan remote marker.
- Snapshot validation, sequence ordering, freshness and scene/run presence.
- Development-only SaveLoad write guard.

## 0.2.0

- Local host/client UI, UDP handshake, heartbeat and disconnect.
- Two real game instances connected successfully in manual testing.

## 0.1.0

- Minimal BepInEx IL2CPP startup plugin.
