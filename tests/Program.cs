using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WanderburgCoopTest;

static void Check(bool valid, string description)
{
    if (!valid) throw new Exception(description);
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
    var bytes = Encoding.ASCII.GetBytes("WB06|HELLO|not-a-token");
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
string[] fields = ("WB06|STATE|" + Guid.NewGuid().ToString("N") + "|" + state.ToPayload(42)).Split('|');
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
    SendRaw("WB05|HELLO|" + token);
    Check(!host.Connected, "Old protocol cannot join the new version");
    SendRaw("WB06|HELLO|" + token);
    Check(host.Connected, "Raw test peer completes handshake");
    SendRaw("WB06|STATE|" + token + "|" + state.ToPayload(10));
    Check(host.RemoteSnapshot == state, "Valid snapshot accepted from selected peer");
    var moved = state with { X = 50 };
    SendRaw("WB06|STATE|" + token + "|" + moved.ToPayload(9));
    Check(host.RemoteSnapshot == state && host.SnapshotsReceived == 1, "Out-of-order snapshot cannot move marker backwards");
    SendRaw("WB06|STATE|" + token + "|" + moved.ToPayload(10));
    Check(host.RemoteSnapshot == state && host.SnapshotsReceived == 1, "Duplicate sequence is ignored");
    SendRaw("WB06|STATE|" + Guid.NewGuid().ToString("N") + "|" + moved.ToPayload(11));
    Check(host.RemoteSnapshot == state, "Wrong-session snapshot ignored");
    var invalid = state with { X = float.NaN };
    SendRaw("WB06|STATE|" + token + "|" + invalid.ToPayload(11));
    Check(host.RemoteSnapshot == state, "Nonfinite network coordinates never enter presentation state");
    SendRaw("WB06|STATE|" + token + "|" + moved.ToPayload(11));
    Check(host.RemoteSnapshot == moved, "Next valid snapshot applies after malformed packet");
    using var stranger = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
    byte[] foreign = Encoding.ASCII.GetBytes("WB06|STATE|" + token + "|" + state.ToPayload(12));
    stranger.Send(foreign, foreign.Length, new IPEndPoint(IPAddress.Loopback, 28714));
    Thread.Sleep(15);
    host.Tick();
    Check(host.RemoteSnapshot == moved, "Different endpoint cannot inject snapshots with copied token");
    SendRaw("WB06|BYE|" + token);
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
    WaitUntil(() => backgroundHost.Info.RemoteSnapshot.InRun && backgroundClient.Info.RemoteSnapshot.InRun);
    Check(backgroundHost.Info.RemoteSnapshot.X == 25 && backgroundClient.Info.RemoteSnapshot == state,
        "Main-thread snapshot samples reach background transport");
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
