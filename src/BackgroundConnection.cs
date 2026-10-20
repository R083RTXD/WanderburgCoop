using System.Diagnostics;
using System.Threading;

namespace WanderburgCoopTest;

public sealed class ConnectionInfo
{
    public static readonly ConnectionInfo Idle = new(false, false, "Bereit", 0, 0, 0, 0, default, false);
    public bool Active { get; }
    public bool Connected { get; }
    public string Status { get; }
    public int Received { get; }
    public int Sent { get; }
    public int SnapshotsReceived { get; }
    public int SessionSeed { get; }
    public PlayerSnapshot RemoteSnapshot { get; }
    public bool HasFreshSnapshot { get; }
    public string Error { get; }

    public ConnectionInfo(bool active, bool connected, string status, int received,
        int sent, int snapshotsReceived, int sessionSeed, PlayerSnapshot remoteSnapshot, bool hasFreshSnapshot, string error = null)
    {
        Active = active;
        Connected = connected;
        Status = status;
        Received = received;
        Sent = sent;
        SnapshotsReceived = snapshotsReceived;
        SessionSeed = sessionSeed;
        RemoteSnapshot = remoteSnapshot;
        HasFreshSnapshot = hasFreshSnapshot;
        Error = error;
    }
}

// Only this managed worker touches the socket. Unity publishes plain data and
// reads an immutable view. Loading scenes, Time.timeScale and game focus cannot
// stop this heartbeat. No Unity API is ever called from this thread.
public sealed class BackgroundConnection : IDisposable
{
    private readonly Thread worker;
    private readonly object snapshotGate = new();
    private PlayerSnapshot localSnapshot;
    private long snapshotPublishedAt;
    private ConnectionInfo info = ConnectionInfo.Idle;
    private int command = -1;
    private int stopping;
    public ConnectionInfo Info => Volatile.Read(ref info);

    public BackgroundConnection()
    {
        worker = new Thread(Run) { IsBackground = true, Name = "Wanderburg Coop Network" };
        worker.Start();
    }

    public void StartHost() => Request(1);
    public void JoinLocal() => Request(2);
    public void Disconnect() => Request(0);

    private void Request(int value)
    {
        if (Volatile.Read(ref stopping) != 0) return;
        Interlocked.Exchange(ref command, value);
    }

    public void PublishPlayer(PlayerSnapshot snapshot)
    {
        lock (snapshotGate)
        {
            localSnapshot = snapshot;
            snapshotPublishedAt = Stopwatch.GetTimestamp();
        }
    }

    private void Run()
    {
        using var session = new LocalSession();
        try
        {
            while (Volatile.Read(ref stopping) == 0)
            {
                switch (Interlocked.Exchange(ref command, -1))
                {
                    case 0: session.Disconnect(); break;
                    case 1: session.StartHost(); break;
                    case 2: session.JoinLocal(); break;
                }
                lock (snapshotGate)
                {
                    var snapshot = localSnapshot;
                    // While Unity is loading or not updating, maintain transport
                    // but don't advertise a frozen player as a live moving entity.
                    if ((Stopwatch.GetTimestamp() - snapshotPublishedAt) / (double)Stopwatch.Frequency > 0.75)
                        snapshot = snapshot with { InRun = false };
                    session.LocalSnapshot = snapshot;
                }
                session.Tick();
                Volatile.Write(ref info, new ConnectionInfo(session.Active, session.Connected, session.Status,
                    session.Received, session.Sent, session.SnapshotsReceived, session.SessionSeed,
                    session.RemoteSnapshot, session.HasFreshSnapshot));
                Thread.Sleep(10);
            }
            session.Disconnect();
            Volatile.Write(ref info, new ConnectionInfo(false, false, "Getrennt", 0, 0, 0, 0, default, false));
        }
        catch (Exception ex)
        {
            Volatile.Write(ref info, new ConnectionInfo(false, false, "Netzwerk-Worker fehlgeschlagen - siehe Log", 0, 0, 0, 0, default, false, ex.ToString()));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref stopping, 1) != 0) return;
        if (!worker.Join(1000)) throw new TimeoutException("Network worker did not shut down within one second.");
    }
}
