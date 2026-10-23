using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WanderburgCoopTest;

static void Check(bool valid, string description)
{
    if (!valid)
    {
        Console.Error.WriteLine("FAIL: " + description);
        Environment.Exit(1); // Expected test failures must not open Windows crash dialogs.
    }
    Console.WriteLine("PASS: " + description);
}

static void Pump(LocalSession host, LocalSession client, Func<bool> done, double timeout = 2)
{
    var timer = Stopwatch.StartNew();
    while (!done() && timer.Elapsed.TotalSeconds < timeout)
    {
        host.Tick();
        client.Tick();
        Thread.Sleep(5);
    }
}

var closeGate = new FocusCloseGate();
var soleHeight = new SoleHeight();
Check(soleHeight.OrFallback(5.185f) == 5.185f,
    "Missing sole geometry preserves the source target rather than fabricating terrain height");
soleHeight.Include(1.2f); soleHeight.Include(0.12f); soleHeight.Include(3);
Check(soleHeight.OrFallback(5.185f) == 0.12f,
    "Visible leg lower edge replaces the observed elevated native IK reference height");
soleHeight.Include(float.NaN); soleHeight.Include(float.NegativeInfinity);
Check(soleHeight.OrFallback(5.185f) == 0.12f,
    "Nonfinite renderer bounds do not corrupt the sole contact height");
var originSnapshot = new PlayerSnapshot(true, 4, 10, 0.4f, 20, 90, 1, 0, 1);
var ownerPosition = new System.Numerics.Vector3(10, 2, 20);
var yawFrame = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, MathF.PI / 2);
var ownerRotation = yawFrame * System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, 0.5f);
var ownerScale = new System.Numerics.Vector3(2, 3, 4);
var sourceFoot = new System.Numerics.Vector3(5, 0, 12);
var localFoot = System.Numerics.Vector3.Transform(sourceFoot - ownerPosition,
    System.Numerics.Quaternion.Inverse(ownerRotation)) / ownerScale;
var rootPose = VisualRootPose.Capture(originSnapshot, ownerPosition, ownerRotation, ownerScale);
var rootPosition = new System.Numerics.Vector3(rootPose.X, rootPose.Y, rootPose.Z);
var rootRotation = new System.Numerics.Quaternion(rootPose.Qx, rootPose.Qy, rootPose.Qz, rootPose.Qw);
var snapshotOrigin = new System.Numerics.Vector3(originSnapshot.X, originSnapshot.Y, originSnapshot.Z);
var restoredFoot = snapshotOrigin + System.Numerics.Vector3.Transform(rootPosition +
    System.Numerics.Vector3.Transform(localFoot * ownerScale, rootRotation), yawFrame);
Check(System.Numerics.Vector3.Distance(sourceFoot, restoredFoot) < 0.0001f,
    "Tilted and vertically offset owner frame restores the original world-space foot contact");
var oldFoot = snapshotOrigin + System.Numerics.Vector3.Transform(localFoot * ownerScale, yawFrame);
Check(System.Numerics.Vector3.Distance(sourceFoot, oldFoot) > 1,
    "Yaw-only presentation reproduces a ground-contact error for the same valid source pose");
Check(rootPose.IsValid && rootPose.Id != VehicleIdentity.FromGroundContact(0, true),
    "Full visual root is a valid bounded pose with a separate contact-independent identity");
var neutralRoot = VisualRootPose.Capture(originSnapshot, snapshotOrigin, yawFrame, System.Numerics.Vector3.One);
Check(Math.Abs(neutralRoot.Y) < 0.0001f && Math.Abs(neutralRoot.Qw - 1) < 0.0001f,
    "A yaw-only unchanged origin retains the previous neutral visual placement");
Check(Enumerable.Range(0, 16).Select(i => VehicleIdentity.FromGroundContact(i, true))
    .Concat(Enumerable.Range(0, 16).Select(i => VehicleIdentity.FromGroundContact(i, false))).Distinct().Count() == 32,
    "Foot and wheel contact anchors have distinct bounded identities");
Check(VehicleIdentity.FromGroundContact(-1, true) == 0 && VehicleIdentity.FromGroundContact(16, false) == 0,
    "Out-of-range ground contact indices are rejected");
var footGate = new GroundMarkGate();
Check(footGate.Sample(0, 0, true, true, true), "Moving foot stamps its first ground contact");
Check(!footGate.Sample(0, 0, true, true, true), "Planted foot does not stamp repeatedly");
Check(!footGate.Sample(0, 0, false, true, true), "Airborne foot never leaves ground marks");
Check(footGate.Sample(0.1f, 0, true, true, true), "Landing re-arms the footprint gate");
Check(!footGate.Sample(0.1f, 0, true, false, true), "Stationary vehicle cannot emit footprints");
Check(footGate.Sample(0.1f, 0, true, true, true), "Foot contact resumes after movement restarts");
Check(!footGate.Sample(1.0f, 0, true, true, true), "Larger footprint graphics do not create dense sub-unit stamp fields");
Check(footGate.Sample(2.7f, 0, true, true, true), "A long grounded stride can still place a spaced footprint");
var wheelGate = new GroundMarkGate();
Check(!wheelGate.Sample(0, 0, true, true, false), "New wheel sample starts spacing without a spawn burst");
Check(!wheelGate.Sample(0.2f, 0, true, true, false), "Wheel marks respect minimum distance");
Check(wheelGate.Sample(0.4f, 0, true, true, false), "Moving wheel creates a spaced track segment");
Check(!wheelGate.Sample(100, 0, true, true, false), "Teleport cannot draw a trail across the world");
Check(wheelGate.Sample(100.4f, 0, true, true, false), "Wheel tracks recover after a teleport");
wheelGate.Reset();
Check(!wheelGate.Sample(101, 0, true, true, false), "Reset clears old contact positions on model changes");
Check(!wheelGate.Sample(float.NaN, 0, true, true, false), "Invalid contact coordinates cannot create marks");
closeGate.OnFocusChanged(false);
closeGate.Observe(false, true, true, true);
Check(!closeGate.TryConsumeClosePermission(false, true), "Unfocused peer rejects another window's confirmation input");
closeGate.OnFocusChanged(true);
closeGate.Observe(true, true, true, true);
Check(!closeGate.TryConsumeClosePermission(true, true), "Confirmation input during automatic focus transfer cannot close the surviving peer");
closeGate.Observe(true, false, false, false);
closeGate.Observe(true, true, true, true);
Check(!closeGate.TryConsumeClosePermission(true, false), "An earlier focused click cannot authorize a later close");
closeGate.OnPeerLost();
closeGate.Observe(true, true, false, false);
Check(!closeGate.TryConsumeClosePermission(true, true), "Delayed inherited release cannot close the surviving peer");
closeGate.Observe(true, false, false, false);
closeGate.Observe(true, true, true, true);
Check(closeGate.TryConsumeClosePermission(true, true), "New post-loss confirmation input may close the focused instance");

var poseNodes = Enumerable.Range(1, 35).Select(i =>
    new VisualNodePose((uint)i, i * 0.1f, 1, 2, 0, 0, 0, 1, 1, 1, 1)).ToArray();
var poseFrame = new VisualPoseFrame(2, 77, 88, poseNodes);
// Exact first rejected transform in both 0.6.2 runtime logs.
var observedSpiderNode = new VisualNodePose(0xC2B895B9, 0, 0, -0.18937616f,
    -0.7071068f, 0, 0, 0.7071067f, 100, 199.16663f, 100);
Check(observedSpiderNode.IsValid,
    "Observed spider import/stretch scale is a valid local transform, not a corrupt pose");
var observedFrame = new VisualPoseFrame(2, 77, 88, new[] { observedSpiderNode });
var observedAssembler = new VisualPoseAssembler();
var observedPacket = ("WB07|POSE|" + Guid.Empty.ToString("N") + "|" + observedFrame.ToPayloads(1).Single()).Split('|');
Check(observedAssembler.Accept(observedPacket, out var observedRoundtrip) && observedRoundtrip.Nodes[0] == observedSpiderNode,
    "Exact logged spider transform survives sender validation and receiver assembly");
foreach (float invalidScale in new[] { 1000.01f, -1000.01f, float.NaN, float.PositiveInfinity })
    Check(!(observedSpiderNode with { Sy = invalidScale }).IsValid,
        "Scale validation still rejects unsafe values: " + invalidScale);
var posePackets = poseFrame.ToPayloads(5).Select(p => ("WB07|POSE|" + Guid.Empty.ToString("N") + "|" + p).Split('|')).ToArray();
Check(posePackets.Length == 3 && posePackets.All(p => string.Join("|", p).Length <= 1200),
    "Rig poses split into bounded datagrams without IP fragmentation");
var assembler = new VisualPoseAssembler();
Check(!assembler.Accept(posePackets[2], out _), "Last chunk alone cannot publish a partial skeleton");
Check(!assembler.Accept(posePackets[0], out _), "Incomplete animation remains unpublished");
Check(assembler.Accept(posePackets[1], out var completePose) && completePose.Nodes.SequenceEqual(poseNodes),
    "Out-of-order pose chunks assemble into the original complete rig");
Check(!assembler.Accept(posePackets[0], out _), "Completed animation frame cannot be replayed");
var lostPackets = poseFrame.ToPayloads(6).Select(p => ("WB07|POSE|" + Guid.Empty.ToString("N") + "|" + p).Split('|')).ToArray();
assembler.Accept(lostPackets[0], out _);
var recoveredPackets = poseFrame.ToPayloads(7).Select(p => ("WB07|POSE|" + Guid.Empty.ToString("N") + "|" + p).Split('|')).ToArray();
bool recovered = false;
foreach (var p in recoveredPackets) recovered = assembler.Accept(p, out _) || recovered;
Check(recovered && !assembler.Accept(lostPackets[1], out _), "A complete new pose recovers from a lost frame and rejects its late chunks");
var corruptedPose = (string[])recoveredPackets[0].Clone();
corruptedPose[3] = "8";
corruptedPose[9] = "not-base64";
Check(!assembler.Accept(corruptedPose, out _), "Malformed animation bytes are rejected");
corruptedPose[9] = recoveredPackets[0][9];
corruptedPose[8] = "99999";
Check(!assembler.Accept(corruptedPose, out _), "Unbounded animation chunk counts are rejected");
Check(!new VisualPoseFrame(2, 77, 88, poseNodes.Append(poseNodes[0])).IsValid,
    "Duplicate rig node identities are rejected");
Check(new VisualPoseFrame(2, 77, 88, poseNodes.Append(poseNodes[0])).InvalidReason == "duplicate node identities",
    "Sender diagnostics distinguish duplicate rig IDs from invalid transforms");
Check(new VisualPoseFrame(2, 77, 88, new[] { poseNodes[0] with { Sx = 1000.01f } }).InvalidReason.Contains("scale="),
    "Rejected transform diagnostics include the actual scale and node identity");
Check(poseFrame.InvalidReason == null, "Valid poses have no rejection diagnostic");
Check(!new VisualPoseFrame(2, 77, 88, new[] { poseNodes[0] with { Qw = 0 } }).IsValid,
    "Zero quaternion cannot reach a Unity animation transform");
Check(!new VisualPoseFrame(2, 77, 88, new[] { poseNodes[0] with { X = float.NaN } }).IsValid,
    "Nonfinite rig coordinates are rejected");
Check(!new VisualPoseFrame(2, 77, 88, Enumerable.Range(1, 257).Select(i => poseNodes[0] with { Id = (uint)i })).IsValid,
    "Oversized rigs are rejected rather than partially animated");
assembler.Reset();
bool resetPose = false;
foreach (var p in posePackets) resetPose = assembler.Accept(p, out _) || resetPose;
Check(resetPose, "Animation sequence history resets for a new session");

using var host = new LocalSession();
using var client = new LocalSession();
// A late host is normal while the other game instance is loading.
client.JoinLocal();
var lateHostWait = Stopwatch.StartNew();
while (client.Active && lateHostWait.Elapsed.TotalSeconds < 0.8)
{
    client.Tick();
    Thread.Sleep(10);
}
Check(client.Active && !client.Connected, "Client waits for late host instead of dying on UDP ConnectionReset: " + client.Status);
client.Disconnect();
host.StartHost();
Check(host.Active, "Local host opens");
using (var conflictingHost = new LocalSession())
{
    conflictingHost.StartHost();
    Check(!conflictingHost.Active, "Second host cannot take occupied port");
}
using (var malformed = new UdpClient())
{
    var bytes = Encoding.ASCII.GetBytes("WB07|HELLO|not-a-token");
    malformed.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, 28714));
    Thread.Sleep(20);
    host.Tick();
    Check(!host.Connected, "Malformed handshake is ignored");
}
client.JoinLocal();
Pump(host, client, () => host.Connected && client.Connected);
Check(host.Connected && client.Connected, "Both peers complete handshake");
Check(host.SessionSeed > 0 && client.SessionSeed == host.SessionSeed,
    "Host-authoritative world seed reaches the client");
host.LocalSnapshot = new PlayerSnapshot(true, 2, 100.25f, 0, -53.5f, 179,
    VehicleIdentity.FromModuleId("castle_A"), 1, 101);
client.LocalSnapshot = new PlayerSnapshot(true, 2, -23.5f, 0.2f, 12.25f, 359.5f,
    VehicleIdentity.FromModuleId("castle_B"), 2, 202);
Pump(host, client, () => host.RemoteSnapshot == client.LocalSnapshot && client.RemoteSnapshot == host.LocalSnapshot);
Check(host.RemoteSnapshot == client.LocalSnapshot && client.RemoteSnapshot == host.LocalSnapshot,
    "Player position, rotation and scene travel both ways");
Check(host.HasFreshSnapshot && client.HasFreshSnapshot, "New snapshots are fresh");
var livePose = new VisualPoseFrame(host.LocalSnapshot.Scene, host.LocalSnapshot.VehicleId,
    host.LocalSnapshot.AppearanceId, poseNodes);
host.LocalPose = livePose;
Pump(host, client, () => client.HasFreshPose && client.RemotePose != null);
Check(client.RemotePose?.Nodes.SequenceEqual(poseNodes) == true && client.PoseFramesReceived > 0,
    "A complete animation rig travels between real socket peers");
host.LocalPose = new VisualPoseFrame(2, host.LocalSnapshot.VehicleId, 999, poseNodes);
int previousPoseCount = client.PoseFramesReceived;
var mismatchWait = Stopwatch.StartNew();
while (mismatchWait.Elapsed.TotalSeconds < 0.15) { host.Tick(); client.Tick(); Thread.Sleep(5); }
Check(client.PoseFramesReceived == previousPoseCount, "Wrong-appearance pose is not sent onto the player's current rig");
host.LocalSnapshot = host.LocalSnapshot with { SkinMask = 4, AppearanceId = 303 };
Pump(host, client, () => client.RemoteSnapshot.SkinMask == 4 && client.RemoteSnapshot.AppearanceId == 303);
Check(client.RemoteSnapshot.VehicleId == host.LocalSnapshot.VehicleId && client.RemoteSnapshot.SkinMask == 4 &&
    client.RemoteSnapshot.AppearanceId == 303,
    "Vehicle upgrade appearance reaches the other peer without reconnecting");
client.LocalSnapshot = new PlayerSnapshot(false, 0, 0, 0, 0, 0);
Pump(host, client, () => !host.RemoteSnapshot.InRun);
Check(!host.RemoteSnapshot.InRun, "Leaving the run clears remote presence");
Pump(host, client, () => host.Received - host.SnapshotsReceived >= 3 && client.Received - client.SnapshotsReceived >= 3);
Check(host.Received - host.SnapshotsReceived >= 3 && client.Received - client.SnapshotsReceived >= 3,
    "Heartbeat packets travel both ways independently of snapshots");
using (var outsider = new LocalSession())
{
    outsider.JoinLocal();
    Pump(host, outsider, () => outsider.Connected, 0.7);
    Check(!outsider.Connected && host.Connected, "Occupied session rejects another peer");
}
client.Disconnect();
Pump(host, client, () => !host.Active);
Check(!host.Active && !host.Connected, "Disconnect releases host session");
Check(!host.HasFreshSnapshot && host.SnapshotsReceived == 0, "Disconnect clears remote player data");
Check(host.RemotePose == null && client.RemotePose == null, "Disconnect clears both remote animation rigs");
host.StartHost();
client.JoinLocal();
Pump(host, client, () => host.Connected && client.Connected);
Check(host.Connected && client.Connected, "Reconnect works after teardown");
Check(host.SnapshotsReceived < 2, "Reconnect starts a fresh snapshot stream");
// Do not tick the client: this simulates a stopped game or a lost peer.
var wait = Stopwatch.StartNew();
bool freshnessChecked = false;
while (host.Active && wait.Elapsed.TotalSeconds < 6)
{
    host.Tick();
    if (!freshnessChecked && wait.Elapsed.TotalSeconds > 1.6)
    {
        Check(!host.HasFreshSnapshot, "Silent snapshot stream expires before connection timeout");
        freshnessChecked = true;
    }
    Thread.Sleep(10);
}
Check(!host.Active, "Silent peer times out and releases socket");
Check(!host.HasFreshSnapshot, "Disconnected peer cannot leave a visible stale marker");

Check(VehicleIdentity.FromModuleId("") == 0, "Missing vehicle identity uses marker fallback");
Check(VehicleIdentity.FromModuleId("castle_A") != VehicleIdentity.FromModuleId("castle_B"),
    "Different vehicle module IDs select different visuals");
var spider = new[] { "body/spider", "legs/spider" };
var mixed = new[] { "body/normal", "legs/spider" };
Check(VehicleIdentity.FromAppearance(spider) != VehicleIdentity.FromAppearance(mixed),
    "Spider legs alone cannot identify a different castle body as the same appearance");
Check(VehicleIdentity.FromAppearance(spider) == VehicleIdentity.FromAppearance(spider.Reverse()),
    "Renderer enumeration order cannot change appearance identity");
Check(VehicleIdentity.FromAppearance(Array.Empty<string>()) == 0,
    "Missing render geometry cannot claim a matching appearance");
var state = new PlayerSnapshot(true, 3, -12.5f, 0.25f, 50.5f, 359.9f, uint.MaxValue, ulong.MaxValue, uint.MaxValue);
var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
string[] fields = ("WB07|STATE|" + Guid.NewGuid().ToString("N") + "|" + state.ToPayload(42)).Split('|');
Check(PlayerSnapshot.TryRead(fields, out long sequence, out var parsed) && sequence == 42 && parsed == state,
    "Wire format roundtrips under German decimal culture");
System.Globalization.CultureInfo.CurrentCulture = previousCulture;
foreach (int fieldIndex in new[] { 10, 11, 12 })
{
    var invalidIdentity = (string[])fields.Clone();
    invalidIdentity[fieldIndex] = "-1";
    Check(!PlayerSnapshot.TryRead(invalidIdentity, out _, out _), "Negative visual identity rejected: " + fieldIndex);
    invalidIdentity[fieldIndex] = "18446744073709551616";
    Check(!PlayerSnapshot.TryRead(invalidIdentity, out _, out _), "Overflowing visual identity rejected: " + fieldIndex);
}
Check(!PlayerSnapshot.TryRead(fields.Take(10).ToArray(), out _, out _),
    "Old snapshot without vehicle identity cannot enter the new session");
Check(!PlayerSnapshot.TryRead(fields.Take(12).ToArray(), out _, out _),
    "Old vehicle-only snapshot cannot select a body without appearance identity");
foreach (string bad in new[] { "NaN", "Infinity", "1000001", "-1000001", "garbage" })
{
    var invalid = (string[])fields.Clone();
    invalid[6] = bad;
    Check(!PlayerSnapshot.TryRead(invalid, out _, out _), "Reject invalid coordinate: " + bad);
}
foreach (string bad in new[] { "-1", "360", "NaN" })
{
    var invalid = (string[])fields.Clone();
    invalid[9] = bad;
    Check(!PlayerSnapshot.TryRead(invalid, out _, out _), "Reject invalid yaw: " + bad);
}
fields[4] = "2";
Check(!PlayerSnapshot.TryRead(fields, out _, out _), "Reject invalid presence flag");

// Exercise receive-order and identity checks with a real socket acting as a peer.
host.StartHost();
using (var rawPeer = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
{
    string token = Guid.NewGuid().ToString("N");
    void SendRaw(string message)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(message);
        rawPeer.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, 28714));
        Thread.Sleep(15);
        host.Tick();
    }
    SendRaw("WB06|HELLO|" + token);
    Check(!host.Connected, "Old protocol cannot join the new version");
    SendRaw("WB07|HELLO|" + token);
    Check(host.Connected, "Raw test peer completes handshake");
    SendRaw("WB07|STATE|" + token + "|" + state.ToPayload(10));
    Check(host.RemoteSnapshot == state, "Valid snapshot accepted from selected peer");
    var moved = state with { X = 50 };
    SendRaw("WB07|STATE|" + token + "|" + moved.ToPayload(9));
    Check(host.RemoteSnapshot == state && host.SnapshotsReceived == 1, "Out-of-order snapshot cannot move marker backwards");
    SendRaw("WB07|STATE|" + token + "|" + moved.ToPayload(10));
    Check(host.RemoteSnapshot == state && host.SnapshotsReceived == 1, "Duplicate sequence is ignored");
    SendRaw("WB07|STATE|" + Guid.NewGuid().ToString("N") + "|" + moved.ToPayload(11));
    Check(host.RemoteSnapshot == state, "Wrong-session snapshot ignored");
    var invalid = state with { X = float.NaN };
    SendRaw("WB07|STATE|" + token + "|" + invalid.ToPayload(11));
    Check(host.RemoteSnapshot == state, "Nonfinite network coordinates never enter presentation state");
    SendRaw("WB07|STATE|" + token + "|" + moved.ToPayload(11));
    Check(host.RemoteSnapshot == moved, "Next valid snapshot applies after malformed packet");
    using var stranger = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
    byte[] foreign = Encoding.ASCII.GetBytes("WB07|STATE|" + token + "|" + state.ToPayload(12));
    stranger.Send(foreign, foreign.Length, new IPEndPoint(IPAddress.Loopback, 28714));
    Thread.Sleep(15);
    host.Tick();
    Check(host.RemoteSnapshot == moved, "Different endpoint cannot inject snapshots with copied token");
    SendRaw("WB07|BYE|" + token);
}
static void WaitUntil(Func<bool> done, double timeout = 3)
{
    var timer = Stopwatch.StartNew();
    while (!done() && timer.Elapsed.TotalSeconds < timeout) Thread.Sleep(10);
}

using (var backgroundHost = new BackgroundConnection())
using (var backgroundClient = new BackgroundConnection())
{
    backgroundClient.JoinLocal();
    Thread.Sleep(900);
    Check(backgroundClient.Info.Active && !backgroundClient.Info.Connected && backgroundClient.Info.Error == null,
        "Background client survives absent host without ConnectionReset");
    backgroundHost.StartHost();
    WaitUntil(() => backgroundHost.Info.Connected && backgroundClient.Info.Connected);
    Check(backgroundHost.Info.Connected && backgroundClient.Info.Connected, "Background peers connect when host starts later");
    Check(backgroundHost.Info.SessionSeed > 0 && backgroundClient.Info.SessionSeed == backgroundHost.Info.SessionSeed,
        "Background connection publishes the shared world seed");
    backgroundHost.PublishPlayer(state);
    backgroundClient.PublishPlayer(state with { X = 25 });
    backgroundHost.PublishPose(new VisualPoseFrame(state.Scene, state.VehicleId, state.AppearanceId, poseNodes));
    WaitUntil(() => backgroundHost.Info.RemoteSnapshot.InRun && backgroundClient.Info.RemoteSnapshot.InRun);
    Check(backgroundHost.Info.RemoteSnapshot.X == 25 && backgroundClient.Info.RemoteSnapshot == state,
        "Main-thread snapshot samples reach background transport");
    WaitUntil(() => backgroundClient.Info.RemotePose != null);
    Check(backgroundClient.Info.RemotePose?.Nodes.SequenceEqual(poseNodes) == true,
        "Plain animation data crosses the background-worker boundary without Unity references");
    var spiderNodes = Enumerable.Range(1, 139).Select(i => poseNodes[0] with { Id = (uint)i }).ToArray();
    spiderNodes[8] = observedSpiderNode;
    int posesBeforeSpider = backgroundClient.Info.PoseFramesReceived;
    int spiderSamples = 0;
    int missingSpiderSamples = 0;
    var spiderTimer = Stopwatch.StartNew();
    while (spiderTimer.Elapsed.TotalSeconds < 1.2)
    {
        backgroundHost.PublishPlayer(state);
        backgroundClient.PublishPlayer(state with { X = 25 });
        spiderNodes[0] = spiderNodes[0] with { X = (float)spiderTimer.Elapsed.TotalSeconds };
        backgroundHost.PublishPose(new VisualPoseFrame(state.Scene, state.VehicleId, state.AppearanceId, spiderNodes));
        var sample = backgroundClient.Info;
        if (spiderTimer.Elapsed.TotalSeconds > 0.2)
        {
            spiderSamples++;
            if (sample.RemotePose?.Nodes.Count != 139) missingSpiderSamples++;
        }
        Thread.Sleep(16);
    }
    Check(backgroundClient.Info.PoseFramesReceived > posesBeforeSpider + 10,
        "A live 139-node spider rig assembles repeatedly alongside position updates");
    Check(spiderSamples > 10 && missingSpiderSamples == 0,
        "Rising spider pose counts retain a fresh complete rig at presentation sampling times");
    Check(backgroundClient.Info.RemotePose?.Nodes[0].X > 0.8f,
        "Changing spider transforms survive immutable publication and worker handoff");
    int receivedBefore = backgroundHost.Info.Received;
    // Reproduce a Unity loading/focus stall longer than the old 5-second timeout.
    // No main-thread calls to either transport occur during this interval.
    Thread.Sleep(6500);
    Check(backgroundHost.Info.Connected && backgroundClient.Info.Connected,
        "Both connections survive 6.5 seconds without game Update");
    Check(backgroundHost.Info.Received > receivedBefore + 10,
        "Worker keeps exchanging packets while game thread is stalled");
    Check(!backgroundHost.Info.RemoteSnapshot.InRun && !backgroundClient.Info.RemoteSnapshot.InRun,
        "Frozen Unity samples hide markers without disconnecting transport");
    Check(backgroundClient.Info.RemotePose == null, "Stalled Unity sampling expires remote animation instead of keeping a stale live rig");
    backgroundHost.PublishPlayer(state);
    backgroundClient.PublishPlayer(state with { X = 30 });
    WaitUntil(() => backgroundHost.Info.RemoteSnapshot.InRun && backgroundHost.Info.RemoteSnapshot.X == 30);
    Check(backgroundHost.Info.RemoteSnapshot.InRun && backgroundHost.Info.RemoteSnapshot.X == 30,
        "Position stream resumes after loading without reconnect");
    backgroundClient.Disconnect();
    WaitUntil(() => !backgroundHost.Info.Active && !backgroundClient.Info.Active);
    Check(!backgroundHost.Info.Active && !backgroundClient.Info.Active,
        "Background disconnect tears down both sessions");
    backgroundHost.StartHost();
    backgroundClient.JoinLocal();
    WaitUntil(() => backgroundHost.Info.Connected && backgroundClient.Info.Connected);
    Check(backgroundHost.Info.Connected && backgroundClient.Info.Connected,
        "Background workers support a second session after disconnect");
}
using (var releaseCheck = new LocalSession())
{
    releaseCheck.StartHost();
    Check(releaseCheck.Active, "Disposing background workers releases the UDP port");
}
Console.WriteLine("All local transport checks passed. Unity focus/cursor still need an in-game check.");
