using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace WanderburgCoopTest;

public sealed class ConnectionPanel : MonoBehaviour
{
    private readonly BackgroundConnection session = new();
    private ConnectionInfo connection = ConnectionInfo.Idle;
    private static readonly ManualLogSource SessionLog = BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Connection");
    private string lastStatus = "";
    private bool previousRunInBackground;
    private readonly RemotePlayerView remoteView = new();
    private string playerStatus = "Warte auf laufenden Run";
    private bool playerErrorLogged;
    private bool viewErrorLogged;
    private PlayerSnapshot local;
    private bool wasConnected;
    private VP appearanceSource;
    private uint appearanceId;
    private float nextAppearanceSample;

    public ConnectionPanel(IntPtr pointer) : base(pointer) { }

    public void Awake()
    {
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
    }

    public void Update()
    {
        local = ReadPlayer();
        session.PublishPlayer(local);
        connection = session.Info;
        if (wasConnected && !connection.Connected) LocalTestFocus.SuppressCloseAfterPeerLoss();
        wasConnected = connection.Connected;
        SharedWorldSeed.Update(connection.Connected ? connection.SessionSeed : 0);
        // Focus hooks must remain enabled after peer loss. Restoring the game's
        // native focus-loss paths while this window is in the background can
        // enter their IL2CPP trampoline during the disconnect transition.
        LocalTestFocus.Active |= connection.Active;
        bool closeConfirmationStarted = LocalTestFocus.HasCloseConfirmationThisFrame();
        LocalTestFocus.ObserveCloseInput(Application.isFocused, closeConfirmationStarted,
            LocalTestFocus.HasCloseConfirmationPressedThisFrame(),
            LocalTestFocus.HasCloseConfirmationHeld());
        if (Application.isFocused && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            ToggleMouse();
        try
        {
            remoteView.Update(connection, local);
        }
        catch (Exception ex)
        {
            remoteView.Dispose();
            if (!viewErrorLogged)
            {
                SessionLog.LogError($"Remote marker could not be rendered: {ex}");
                viewErrorLogged = true;
            }
        }
        if (lastStatus == connection.Status) return;
        lastStatus = connection.Status;
        SessionLog.LogInfo($"PID {Environment.ProcessId}: {lastStatus}");
        if (connection.Error != null) SessionLog.LogError(connection.Error);
    }

    public void LateUpdate() => LocalTestFocus.Apply();

    private void ToggleMouse()
    {
        LocalTestFocus.FreeMouse = !LocalTestFocus.FreeMouse;
        SessionLog.LogInfo($"PID {Environment.ProcessId}: Mouse release = {LocalTestFocus.FreeMouse}");
    }

    public void OnApplicationFocus(bool focused)
    {
        LocalTestFocus.OnFocusChanged(focused);
        SessionLog.LogInfo($"PID {Environment.ProcessId}: Focus={focused}; local session active={connection.Active}; mouse release={LocalTestFocus.FreeMouse}");
    }

    private PlayerSnapshot ReadPlayer()
    {
        int scene = SceneManager.GetActiveScene().buildIndex;
        try
        {
            var gm = GM.gm;
            if (!gm || !gm.gameplayRoundStarted || gm.gameOver || gm.gameWon || !gm.vm)
            {
                playerStatus = "Lokal: kein aktiver Run";
                return new PlayerSnapshot(false, scene, 0, 0, 0, 0);
            }
            var vm = gm.vm;
            // Sample the visual origin so the remote mesh has the same height
            // and facing as the displayed vehicle, not its separate rigidbody.
            var visual = vm.vp;
            if (appearanceSource != visual || Time.unscaledTime >= nextAppearanceSample)
            {
                appearanceSource = visual;
                uint sampled = VehicleModel.GetAppearance(visual);
                if (sampled != appearanceId)
                    SessionLog.LogInfo($"PID {Environment.ProcessId}: local appearance {sampled:X8} from {(visual ? visual.name : "none")}");
                appearanceId = sampled;
                nextAppearanceSample = Time.unscaledTime + 0.5f;
            }
            Transform player = visual ? visual.transform : vm.rb ? vm.rb.transform : vm.transform;
            ulong skinMask = 0;
            if (visual && visual.myVehicleSkinElements != null)
            {
                var skins = visual.myVehicleSkinElements;
                for (int i = 0; i < Math.Min(64, skins.Length); i++)
                    if (skins[i] && skins[i].gameObject.activeSelf) skinMask |= 1UL << i;
            }
            var pos = player.position;
            var value = new PlayerSnapshot(true, scene, pos.x, pos.y, pos.z,
                Mathf.Repeat(player.eulerAngles.y, 360),
                VehicleModel.GetIdentity(visual), skinMask, appearanceId);
            if (!value.IsValid) throw new InvalidOperationException("Player transform is outside supported bounds.");
            playerStatus = $"Lokal: X {pos.x:F1} / Z {pos.z:F1}";
            return value;
        }
        catch (Exception ex)
        {
            playerStatus = "Spieler nicht lesbar - siehe BepInEx-Log";
            if (!playerErrorLogged)
            {
                SessionLog.LogError($"Player snapshot failed: {ex}");
                playerErrorLogged = true;
            }
            return new PlayerSnapshot(false, scene, 0, 0, 0, 0);
        }
    }

    public void OnGUI()
    {
        float x = Math.Max(8, Screen.width - 408);
        GUI.Box(new Rect(x, 8, 400, 376), "Wanderburg Co-op 0.5.2 - Fahrzeug-/Positionstest");
        GUI.Label(new Rect(x + 12, 38, 376, 25), connection.Status);
        GUI.Label(new Rect(x + 12, 64, 376, 25), $"Pakete: gesendet {connection.Sent} / empfangen {connection.Received}");
        if (!connection.Active)
        {
            if (GUI.Button(new Rect(x + 12, 98, 180, 32), "Host erstellen"))
            {
                LocalTestFocus.Active = true;
                session.StartHost();
            }
            if (GUI.Button(new Rect(x + 204, 98, 180, 32), "Lokal beitreten"))
            {
                LocalTestFocus.Active = true;
                session.JoinLocal();
            }
        }
        else if (GUI.Button(new Rect(x + 12, 98, 372, 32), "Verbindung trennen")) session.Disconnect();
        GUI.Label(new Rect(x + 12, 140, 376, 25), playerStatus);
        var remote = connection.RemoteSnapshot;
        string remoteStatus = !connection.HasFreshSnapshot ? "Gegenstelle: warte auf Positionsdaten" :
            !remote.InRun ? "Gegenstelle: kein aktiver Run" :
            remote.Scene != local.Scene ? "Gegenstelle: andere Spielszene" :
            $"Gegenstelle: X {remote.X:F1} / Z {remote.Z:F1}";
        GUI.Label(new Rect(x + 12, 166, 376, 25), remoteStatus);
        GUI.Label(new Rect(x + 12, 192, 376, 25), $"Positionspakete: {connection.SnapshotsReceived} | Figur: {(remoteView.Visible ? "sichtbar" : "aus")}");
        GUI.Label(new Rect(x + 12, 218, 376, 25), $"Weltsamen: {(connection.SessionSeed > 0 ? connection.SessionSeed : 0)}");
        GUI.Label(new Rect(x + 12, 244, 376, 25), "Darstellung: " + remoteView.Representation);
        GUI.Label(new Rect(x + 12, 270, 376, 25), "TESTMODUS: Run-Fortschritt wird nicht gespeichert.");
        GUI.Label(new Rect(x + 12, 296, 376, 25), "Gleicher Welt-Seed; Gegner/Kampf noch getrennt.");
        if (GUI.Button(new Rect(x + 12, 328, 372, 30),
            LocalTestFocus.FreeMouse ? "F8: Maus frei (umschalten)" : "F8: Maus freigeben")) ToggleMouse();
    }

    public void OnDestroy()
    {
        session.Dispose();
        remoteView.Dispose();
        LocalTestFocus.Active = false;
        SharedWorldSeed.Update(0);
        LocalTestFocus.RestoreCursor();
        Application.runInBackground = previousRunInBackground;
    }

    public void OnApplicationQuit() => session.Dispose();
}
