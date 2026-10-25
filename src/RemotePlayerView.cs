using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// A collision-free marker, not a clone of VM: cloning the player would duplicate
// singleton setup, input, weapons, collection and save side effects.
internal sealed class RemotePlayerView : IDisposable
{
    private GameObject root;
    private Material material;
    private int scene = int.MinValue;
    private bool initialized;
    private VehicleModel vehicleModel = new();
    private readonly RemoteGroundMarks groundMarks = new();
    private bool marksErrorLogged;
    private uint vehicleId;
    private uint appearanceId;
    private ulong skinMask;
    private bool hasVehicleModel;
    private float nextModelAttempt;
    private float nextPoseDiagnostic;
    private int poseDiagnostics;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Pose");
    public bool Visible { get; private set; }
    public string AnimationStatus { get; private set; } = "warte";
    public string GroundMarksStatus => marksErrorLogged ? "Fehler - siehe Log" : groundMarks.Status;
    private bool upgradePending;
    public string Representation => hasVehicleModel ? upgradePending ? "Burg (Ausbau offen)" : "Fahrzeugmodell" : "Testfigur (Modell fehlt)";

    public void Update(ConnectionInfo session, PlayerSnapshot local)
    {
        UpdateView(session, local);
        vehicleModel.UpdateDrivingEffects(Visible && hasVehicleModel && session.RemotePose != null);
        if (!marksErrorLogged)
        {
            try
            {
                groundMarks.Update(Visible && hasVehicleModel && root ? root.transform : null,
                    vehicleModel.GroundContacts, session.RemotePose != null, vehicleModel.FootprintPrefab,
                    vehicleModel.HasNativeWheelTrails);
            }
            catch (Exception ex)
            {
                groundMarks.Dispose();
                marksErrorLogged = true;
                Log.LogError($"Ground marks disabled; vehicle presentation retained: {ex}");
            }
        }
        // Temporary, bounded receiver probe: distinguish transport freshness,
        // presentation guards and node matching without per-frame log spam.
        if (session.PoseFramesReceived > 0 && poseDiagnostics < 6 && Time.unscaledTime >= nextPoseDiagnostic)
        {
            nextPoseDiagnostic = Time.unscaledTime + 5;
            poseDiagnostics++;
            var pose = session.RemotePose;
            var remote = session.RemoteSnapshot;
            Log.LogInfo($"PID {Environment.ProcessId}: received={session.PoseFramesReceived}, animation={AnimationStatus}, visible={Visible}, model={hasVehicleModel}, targets={vehicleModel.AnimatedNodeCount}, freshState={session.HasFreshSnapshot}, run={local.InRun}/{remote.InRun}, scene={local.Scene}/{remote.Scene}, pose={(pose == null ? "null" : $"{pose.Nodes.Count} nodes, scene={pose.Scene}, vehicle={pose.VehicleId:X8}, appearance={pose.AppearanceId:X8}")}, remoteVehicle={remote.VehicleId:X8}, remoteAppearance={remote.AppearanceId:X8}");
        }
    }

    private void UpdateView(ConnectionInfo session, PlayerSnapshot local)
    {
        var remote = session.RemoteSnapshot;
        bool show = TestSaveGuard.Ready && session.HasFreshSnapshot && local.InRun && remote.InRun && local.Scene == remote.Scene;
        if (!show)
        {
            AnimationStatus = "Figur ausgeblendet";
            if (root) root.SetActive(false);
            Visible = initialized = false;
            return;
        }
        if (!root || scene != local.Scene || vehicleId != remote.VehicleId)
        {
            ClearView();
            Create(local.Scene, remote);
        }
        else if ((skinMask != remote.SkinMask || appearanceId != remote.AppearanceId || !hasVehicleModel) &&
            Time.unscaledTime >= nextModelAttempt)
            TryRefresh(remote);
        upgradePending = hasVehicleModel && (skinMask != remote.SkinMask || appearanceId != remote.AppearanceId);
        Vector3 position = new(remote.X, remote.Y + (hasVehicleModel ? 0 : 2), remote.Z);
        Quaternion rotation = Quaternion.Euler(0, remote.Yaw, 0);
        if (!initialized || Vector3.Distance(root.transform.position, position) > 40)
        {
            root.transform.SetPositionAndRotation(position, rotation);
            initialized = true;
        }
        else
        {
            float blend = 1 - Mathf.Exp(-15 * Time.unscaledDeltaTime);
            root.transform.position = Vector3.Lerp(root.transform.position, position, blend);
            root.transform.rotation = Quaternion.Slerp(root.transform.rotation, rotation, blend);
        }
        root.SetActive(true);
        var pose = session.RemotePose;
        AnimationStatus = !hasVehicleModel ? "kein Modell" : pose == null ? "keine frische Pose" : "Kennung passt nicht";
        if (hasVehicleModel && pose != null && pose.AppearanceId == remote.AppearanceId &&
            pose.VehicleId == remote.VehicleId && pose.Scene == remote.Scene)
            AnimationStatus = $"{(upgradePending ? "Basis " : "")}Knoten {vehicleModel.ApplyPose(pose)}/{pose.Nodes.Count}";
        Visible = true;
    }

    private void TryRefresh(PlayerSnapshot remote)
    {
        nextModelAttempt = Time.unscaledTime + 2;
        var candidateRoot = new GameObject("WanderburgCoop_Replacement_VisualOnly");
        candidateRoot.SetActive(false);
        SceneManager.MoveGameObjectToScene(candidateRoot, SceneManager.GetActiveScene());
        var candidateModel = new VehicleModel();
        try
        {
            if (!candidateModel.TryBuild(candidateRoot, remote)) return;
            // Build first. A failed exact-appearance lookup must not destroy a
            // known castle merely because a remote module was added.
            if (root) root.SetActive(false);
            if (root) Object.Destroy(root);
            if (material) Object.Destroy(material);
            material = null;
            vehicleModel.Dispose();
            groundMarks.Clear();
            vehicleModel = candidateModel;
            root = candidateRoot;
            candidateModel = null; candidateRoot = null;
            appearanceId = remote.AppearanceId; skinMask = remote.SkinMask;
            hasVehicleModel = true;
            initialized = false;
        }
        finally
        {
            candidateModel?.Dispose();
            if (candidateRoot) Object.Destroy(candidateRoot);
        }
    }

    private void Create(int sceneId, PlayerSnapshot remote)
    {
        scene = sceneId;
        root = new GameObject("WanderburgCoop_RemotePlayer_TestMarker");
        root.SetActive(false);
        // A plain GameObject is scene-owned. Keep it out of the persistent plugin scene.
        SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
        vehicleId = remote.VehicleId;
        appearanceId = remote.AppearanceId;
        skinMask = remote.SkinMask;
        nextModelAttempt = Time.unscaledTime + 2;
        hasVehicleModel = vehicleModel.TryBuild(root, remote);
        if (hasVehicleModel) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        if (shader)
        {
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.1f, 0.9f, 1, 1));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.1f, 0.9f, 1, 1));
        }
        CreatePart("Body", new Vector3(0, 0, 0), new Vector3(3, 3, 4));
        CreatePart("Direction", new Vector3(0, 0.5f, 3), new Vector3(1, 1, 2));
    }

    private void CreatePart(string name, Vector3 offset, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = "Coop_" + name;
        part.layer = 2; // Ignore Raycast, so game aiming/selection cannot hit the marker.
        var collider = part.GetComponent<Collider>();
        if (collider)
        {
            collider.enabled = false;
            Object.Destroy(collider);
        }
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = offset;
        part.transform.localScale = scale;
        if (material) part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void ClearView()
    {
        groundMarks.Clear();
        if (root) root.SetActive(false);
        if (root) Object.Destroy(root);
        if (material) Object.Destroy(material);
        root = null;
        material = null;
        initialized = Visible = false;
        scene = int.MinValue;
        hasVehicleModel = false;
        upgradePending = false;
        vehicleModel.ClearMeshes();
    }

    public void Dispose()
    {
        ClearView();
        groundMarks.Dispose();
        vehicleModel.Dispose();
    }
}
