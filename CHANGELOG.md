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

## 0.6.9 — 2026-10-23 — footprint artwork test (tested, known defects)

- Latest user-tested publication: artwork loads but spider marks are misplaced
  toward the body, pile up and do not reproduce native short-lived dust/growth
  animation. Normal wheel tracks are too long/dotted. Not a stable footprint fix.
- Neue Module fehlen weiterhin; bekannte Burg bleibt bei Ausbau sichtbar.
  Spinnen-Abdrücke versetzt, Ablauf unpassend, Radspuren zu lang/gepunktet.
- Later unfinished experiments are excluded from this publication.

- 0.6.8 user confirms visible footprints and castle retention after upgrades;
  new modules still omitted as documented. Screenshot shows many tiny dark
  placeholder triangles. Foot contact generation works; artwork needs improvement.
- Prefer up to four static mesh/material parts from the installed castle's
  stepEffectPrefab. Construct plain graphics-only objects, never Instantiate
  the prefab or copy GroundEffect/particle/audio/collision/gameplay components.
  Native assets stay in the user's installed game and are not redistributed.
- Preserve internal graphics transforms/materials. Unsupported particle-only or
  zero-scale templates use a larger brown irregular ring, not black triangles.
- Increase grounded-stride spacing from0.85 to2.5 units; retain landing triggers,
  <=256 pooled marks and lifetime14s. Panel says Originalgrafik or Ersatzring.
- Native artwork, proportions and material behavior still need a visual test;
  this does not claim replication of the full animated native step effect.
- Abdruckgrafik-Test: Original-Meshdaten rein optisch, sonst größerer Ersatzring.

## 0.6.8 — 2026-10-23 — retain castle through missing upgrades / sole test (tested)

- User confirms footprints appear (small dark test graphics) and upgraded castles
  remain visible without new modules. Both reported fixes verified manually.

- User confirms 0.6.7 stops legs sinking. Keep its full owner-frame correction.
- New report: installing a module makes the remote castle become a cyan marker.
  Logs show new appearance hashes; no exact matching assembled local vehicle
  exists for many remote module combinations. Previous code destroyed the known
  model before checking whether a replacement could be built.
- Build an inactive replacement first, only swap on success. Keep the last known
  vehicle in the same scene/vehicle identity when an appearance lookup fails;
  retry at most every two seconds. Show 'Ausbau offen' and animate intersecting
  known node IDs. This is a degraded visual fallback, NOT full module-layout sync.
- Native IK foot targets are already at Y~5.19 on the sender against floor Y0.
  Use the lower render bounds of the matching detached leg for contact height,
  retaining target X/Z; do not include body geometry or relax terrain tolerance.
  Bounds accuracy and footprint visibility await a native test.
- 119 managed checks pass; native upgrade retention and footprints unverified.
- Burg bei fehlender Modulkombination behalten; neue Module noch nicht vollständig
  sichtbar synchronisiert. Sohlen-Test statt erhöhter IK-Zielhöhe.

## 0.6.7 — 2026-10-23 — full visual owner frame (tested, partial success)

- User confirms remote legs no longer sink. Footprints still absent and module
  installation exposes exact-appearance fallback limitation.

- User reports no visible footprints and occasional sinking remote legs in
  0.6.6. Logs show native grass Y=0, received contact around Y=5, and occasional
  stamps on non-floor objects; target replacement alone did not resolve it.
- Correct an independently reproducible coordinate gap: local bones and
  detached roots were sampled in full VP coordinates, but the receiver omitted
  VP origin offset, pitch/roll and source rotation relative to snapshot yaw.
- Add a dedicated root-pose node, carrying the full sampled VP TRS relative to
  snapshot position/yaw. Apply it to visualRoot before child poses. Existing
  body/bone/contact IDs remain intact. Do not clamp legs to terrain or loosen
  ground tolerance. Source origin/contact logs help validate the hypothesis.
- Pure-math regression reproduces old yaw-only foot error and verifies world
  contact reconstruction with tilt, vertical offset and nonuniform scale.
- Native sinking/footprint retest pending; no confirmed full-world height sync.
- Fahrzeugursprung vollständig übertragen; sichtbarer Höhen-/Spurentest offen.

## 0.6.6 — 2026-10-23 — separate IK contact anchors (tested, failed)

- User reports still no visible tracks, occasional underground remote legs.
  Screenshot four contacts, eight/zero stamps, accepted ground count zero.

- 0.6.5 ground probes hit BaseGround_Grass at Y=0 but reject Bone.003_end
  at Y=4.92..6.38. The selected solver bone is not a reliable planted contact.
- Use native IK Target transforms for spider contacts, with dedicated stable
  contact IDs and vehicle-relative poses. Targets may live outside mesh/bone
  hierarchy, so create separate invisible render-side contact anchors.
- Preserve all existing render-bone mappings. Wheels also use separate contact
  anchors. No arbitrary height-limit increase or guessed floor-plane fallback.
- Ground footprint placement and landing timing still need user verification.
- Fusskontakt-Test: IK-Ziele statt des zu hoch liegenden Knochen-Endes.

## 0.6.5 — 2026-10-23 — boost identity filtering / ground probe (tested)

- User confirms vehicle remains visible during nitro. Nitro VFX are not yet
  replicated. Footprints still fail; ground diagnostics identify high bone tip.

- User reports remote castle becomes the cyan fallback during nitro. Logs show
  repeated appearance F39A71E7/33A1696F transitions and 143/151 rig nodes.
- Exclude renderers under DriveFeedback.boostVFXgroup from model appearance,
  render cloning and pose-source enumeration. A transient VFX branch must not
  select another castle body. Native boost retest still required.
- Screenshot confirms four contact points but zero stamps, while 143/143 pose
  nodes apply. Add bounded moving-contact ground probes (four logs, five seconds
  apart) for hit object/layer, dynamic body, height delta and native ground plane.
  Show accepted ground contacts in the panel. No speculative ground fallback.
- Other castle models remain unverified: user has not unlocked them. No unlock,
  save or progression changes; no promise of universal vehicle/effect support.
- Boost-Zweig aus der Modellkennung entfernt; Bodendiagnose für die fehlenden
  Abdrücke ergänzt. Keine bestätigte Spurenbehebung, erneuter Spieltest nötig.

## 0.6.4 — 2026-10-23 — cosmetic ground-mark prototype (tested, failed)

- User reports no spider footprints. Screenshot: four contacts, zero stamps,
  both peers apply all 143 pose nodes. Also reports nitro fallback regression.

- Derive bounded cosmetic marks locally from the received animated rig, without
  additional network packets. Use native wheel/IK-foot references to locate
  matching render-only contacts. Include contact ancestors in pose sampling.
- Draw approximate dark wheel segments and claw-shaped footprints on nearby
  non-trigger, non-rigidbody ground. They are simplified mod visuals, not the
  game's exact native particle/decal presets. Unsupported contacts stay silent.
- Distance/contact gating prevents stationary or airborne stamping; reset on
  stale poses, teleport, model/scene change. Marks shrink and expire after 14s.
- At most 16 contact probes per 80ms and 256 reused mark objects. Shared meshes
  and material are released at teardown. No physics, sound, gameplay or damage
  components are instantiated. Ground-effect errors do not disable animation.
- Show contact/stamp counts in the panel. Native visual testing pending.
- Vereinfachte Rad-/Krallenspuren als rein optischer Test; kein Trittschaden,
  keine Original-Partikeleffekte und noch keine Gegner-Synchronisierung.

## 0.6.3 — 2026-10-23 — accept native spider rig scales (tested)

- User confirmed visible animations work after the scale correction.
  Nutzer bestätigt: Animationen funktionieren. Ground marks were still missing.

- Both 0.6.2 logs reject node C2B895B9 with scale (100,199.16663,100).
  The mod incorrectly capped local scales at 100, dropping the complete frame.
- Raise the finite absolute local-scale bound to 1000 on sender and receiver.
  Keep all other frame, identity, quaternion and packet validation unchanged.
- Reproduce the rejection with the exact logged transform before fixing; add
  wire roundtrip coverage and include it in the live 139-node worker test.
  Invalid/nonfinite scales remain rejected. Visible animation awaits user test.
- Failed managed assertions now print FAIL and exit with code 1, instead of
  throwing an unhandled exception that opens a Windows crash dialog.
- Ursache der verworfenen Posen eingegrenzt: normale Import-/Streckskalierung
  der Spinnenburg wurde als unzulässig abgelehnt. Spieltest steht noch aus.

## 0.6.2 — 2026-10-23 — pose capture diagnosis (local test)

- User corrected the earlier report: the pose counter stayed at **zero**,
  including 0.6.0. 0.6.1 screenshot shows marker fallback and no model; logs
  show the spider model was available at other times. Receiver diagnostics
  did not activate because no poses arrived.
- Show local capture status, including whether LateUpdate has ever called the
  sampler. Log bounded sender-state transitions and rejected node TRS values
  or duplicate identities. Do not relax wire validation or claim a fix.
- Nutzerkorrektur: Posen waren auch zuvor **0**. Diagnose liegt jetzt auf der
  lokalen Aufnahme; keine bestaetigte Animationsbehebung.

## 0.6.1 — 2026-10-23 — animation receiver diagnosis (local test)

- User reports rising pose counters in 0.6.0, but stationary wheels and legs.
  Logs show 139 spider rig nodes and no first-pose application message.
- Add a visible animation status and applied/received node counts, plus at most
  six receiver-state log samples per instance. No speculative gameplay or
  disconnect changes. This is a diagnostic build, not a confirmed animation fix.
- Nutzerbericht: Posen-Zähler steigt, Räder und Beine bleiben still. Neue
  Diagnosezeile und begrenzte Logs grenzen die fehlende Anwendung ein.

## 0.6.0 — 2026-10-23 — wheel and spider-leg animation test

- Sample the owning vehicle's visible transform/bone poses on the main thread
  at up to 20 Hz and apply them only to the remote render-only hierarchy.
- Include detached leg roots in vehicle-relative space and interpolate received
  local joint transforms. Copy supported skinned mesh skeletons without controllers.
- Add WB07 pose packets with bounded chunks (up to 256 nodes, under 1200 bytes
  per datagram), complete-frame assembly, validation and stale-frame rejection.
- Preserve the managed background transport; no Unity references enter its worker.
- Show received pose-frame counts in the panel; log rig size and matched nodes.
- 85 managed checks pass. Build has zero warnings/errors. Actual wheel/leg
  animation failed the user's two-instance check: wheels/legs remain static
  despite a rising pose counter. No animation fix has been confirmed.
- Both instances need 0.6.0. Unsupported rigs stay static; no IK controller,
  step damage, audio, enemy, combat or progression replication is added.

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
