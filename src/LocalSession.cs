using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace WanderburgCoopTest;

// Local two-peer transport. Player snapshots are presentation-only: receiving
// one never changes any game entity, opponent, health, inventory or progression.
public sealed class LocalSession : IDisposable
{
    private const int Port = 28714;
    private const string Protocol = "WB06";
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private UdpClient socket;
    private IPEndPoint peer;
    private string token = "";
    private bool host;
    private double lastReceive;
    private double lastSend = -1;
    private double started;
    private double lastStateSend = -1;
    private double lastStateReceive = double.NegativeInfinity;
    private long nextSequence;
    private long remoteSequence = -1;
    public bool Connected { get; private set; }
    public bool Active => socket != null;
    public string Status { get; private set; } = "Bereit";
    public int Received { get; private set; }
    public int Sent { get; private set; }
    public int SnapshotsReceived { get; private set; }
    public int SessionSeed { get; private set; }
    public PlayerSnapshot LocalSnapshot { get; set; }
    public PlayerSnapshot RemoteSnapshot { get; private set; }
    public bool HasFreshSnapshot => Connected && clock.Elapsed.TotalSeconds - lastStateReceive < 1.5;

    public void StartHost() => Start(true);
    public void JoinLocal() => Start(false);

    private void Start(bool asHost)
    {
        Stop();
        host = asHost;
        token = asHost ? "" : Guid.NewGuid().ToString("N");
        SessionSeed = asHost ? RandomNumberGenerator.GetInt32(1, int.MaxValue) : 0;
        try
        {
            socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, asHost ? Port : 0));
            // A datagram sent before the other instance binds its port can cause
            // Winsock to report an ICMP port-unreachable as WSAECONNRESET on a
            // later receive. UDP has no connection to reset: our heartbeat owns
            // liveness. Disable this notification for this socket only.
            if (OperatingSystem.IsWindows())
                socket.Client.IOControl(unchecked((int)0x9800000C), new byte[4], null);
            socket.Client.Blocking = false;
            peer = asHost ? null : new IPEndPoint(IPAddress.Loopback, Port);
            started = clock.Elapsed.TotalSeconds;
            Status = asHost ? "Host wartet auf zweiten Spieler (lokal)" : "Verbinde mit 127.0.0.1...";
        }
        catch (SocketException ex)
        {
            Stop();
            Status = $"Start fehlgeschlagen ({ex.SocketErrorCode}). Host bereits offen?";
        }
    }

    public void Tick()
    {
        if (socket == null) return;
        try
        {
            // A strict per-frame budget prevents packet traffic from blocking Unity.
            for (int i = 0; i < 32 && socket.Available > 0; i++)
            {
                IPEndPoint sender = new(IPAddress.Any, 0);
                byte[] bytes = socket.Receive(ref sender);
                if (!IPAddress.IsLoopback(sender.Address) || bytes.Length > 256) continue;
                string[] parts = Encoding.ASCII.GetString(bytes).Split('|');
                if (parts.Length < 3 || parts[0] != Protocol || !Guid.TryParseExact(parts[2], "N", out _)) continue;
                if (host && parts.Length == 3 && parts[1] == "HELLO" && peer == null)
                {
                    peer = sender;
                    token = parts[2];
                }
                if (peer == null || !peer.Equals(sender) || parts[2] != token) continue;
                if (parts[1] == "STATE")
                {
                    if (!Connected || !PlayerSnapshot.TryRead(parts, out long sequence, out var snapshot) ||
                        sequence <= remoteSequence) continue;
                    RemoteSnapshot = snapshot;
                    remoteSequence = sequence;
                    lastStateReceive = lastReceive = clock.Elapsed.TotalSeconds;
                    SnapshotsReceived++;
                    Received++;
                    continue;
                }
                if (parts[1] == "BYE" && parts.Length == 3)
                {
                    Stop();
                    Status = "Gegenstelle hat getrennt";
                    return;
                }
                bool valid;
                if (host)
                {
                    valid = parts.Length == 3 && parts[1] is "HELLO" or "PING";
                }
                else
                {
                    int seed = 0;
                    valid = parts.Length == 4 && parts[1] == "PONG" &&
                        int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out seed) && seed > 0;
                    if (valid) SessionSeed = seed;
                }
                if (!valid) continue;
                Received++;
                lastReceive = clock.Elapsed.TotalSeconds;
                Connected = true;
                Status = host ? "Verbunden: Host + 1 Client" : "Verbunden mit lokalem Host";
                if (host) Send("PONG", SessionSeed.ToString(CultureInfo.InvariantCulture));
            }
            double now = clock.Elapsed.TotalSeconds;
            if (Connected && now - lastReceive > 5 || !host && !Connected && now - started > 8)
            {
                Stop();
                Status = "Zeitueberschreitung. Host/Client erneut verbinden.";
                return;
            }
            if (!host && now - lastSend >= 0.5)
            {
                Send(Connected ? "PING" : "HELLO");
                lastSend = now;
            }
            if (Connected && now - lastStateSend >= 0.05 && LocalSnapshot.IsValid)
            {
                Send("STATE", LocalSnapshot.ToPayload(nextSequence++));
                lastStateSend = now;
            }
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode == SocketError.WouldBlock) return;
            Stop();
            Status = $"Netzwerkfehler: {ex.SocketErrorCode}";
        }
    }

    private void Send(string kind, string payload = null)
    {
        if (socket == null || peer == null) return;
        byte[] bytes = Encoding.ASCII.GetBytes($"{Protocol}|{kind}|{token}" +
            (payload == null ? "" : "|" + payload));
        socket.Send(bytes, bytes.Length, peer);
        Sent++;
    }

    public void Disconnect()
    {
        try { Send("BYE"); } catch (SocketException) { }
        Stop();
        Status = "Getrennt";
    }

    private void Stop()
    {
        socket?.Dispose();
        socket = null;
        peer = null;
        Connected = false;
        Received = 0;
        Sent = 0;
        lastSend = -1;
        lastStateSend = -1;
        lastStateReceive = double.NegativeInfinity;
        nextSequence = 0;
        remoteSequence = -1;
        SnapshotsReceived = 0;
        SessionSeed = 0;
        RemoteSnapshot = default;
    }

    public void Dispose() => Disconnect();
}
