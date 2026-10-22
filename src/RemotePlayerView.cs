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
    private readonly VehicleModel vehicleModel = new();
    private uint vehicleId;
    private ulong skinMask;
    private bool hasVehicleModel;
    private float nextModelAttempt;
    public bool Visible { get; private set; }
    public string Representation => hasVehicleModel ? "Fahrzeugmodell" : "Testfigur (Modell fehlt)";

    public void Update(ConnectionInfo session, PlayerSnapshot local)
    {
        var remote = session.RemoteSnapshot;
        bool show = TestSaveGuard.Ready && session.HasFreshSnapshot && local.InRun && remote.InRun && local.Scene == remote.Scene;
        if (!show)
        {
            if (root) root.SetActive(false);
            Visible = initialized = false;
            return;
        }
        if (!root || scene != local.Scene || vehicleId != remote.VehicleId || skinMask != remote.SkinMask ||
            (!hasVehicleModel && remote.VehicleId != 0 && Time.unscaledTime >= nextModelAttempt))
        {
            ClearView();
            Create(local.Scene, remote);
        }
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
        Visible = true;
    }

    private void Create(int sceneId, PlayerSnapshot remote)
    {
        scene = sceneId;
        root = new GameObject("WanderburgCoop_RemotePlayer_TestMarker");
        root.SetActive(false);
        // A plain GameObject is scene-owned. Keep it out of the persistent plugin scene.
        SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
        vehicleId = remote.VehicleId;
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
        if (root) root.SetActive(false);
        if (root) Object.Destroy(root);
        if (material) Object.Destroy(material);
        root = null;
        material = null;
        initialized = Visible = false;
        scene = int.MinValue;
        hasVehicleModel = false;
        vehicleModel.ClearMeshes();
    }

    public void Dispose()
    {
        ClearView();
        vehicleModel.Dispose();
    }
}
