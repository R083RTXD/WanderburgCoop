using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Construct a render-only representation. Never Instantiate a VP/VM prefab:
// even temporarily disabled game components can execute Awake/OnEnable.
internal sealed class VehicleModel : IDisposable
{
    private readonly List<Mesh> bakedMeshes = new();
    private readonly Dictionary<int, Transform> transforms = new();
    private readonly Dictionary<uint, VP> catalog = new();
    private float nextScan;
    private int catalogScene = int.MinValue;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Visual");

    public static uint GetIdentity(VP vehicle) => !vehicle ? 0 :
        VehicleIdentity.FromModuleId(string.IsNullOrEmpty(vehicle.moduleID)
            ? "prefab:" + vehicle.name.Replace("(Clone)", "").Trim()
            : vehicle.moduleID);

    public bool TryBuild(GameObject destination, PlayerSnapshot remote)
    {
        if (remote.VehicleId == 0) return false;
        if (catalogScene != remote.Scene)
        {
            catalog.Clear();
            catalogScene = remote.Scene;
            nextScan = 0;
        }
        if (!catalog.TryGetValue(remote.VehicleId, out var source) || !source)
        {
            if (Time.unscaledTime < nextScan) return false;
            nextScan = Time.unscaledTime + 2;
            // Includes unloaded-from-scene but resident prefab assets. No
            // instantiation, unlock changes, or calls into vehicle gameplay.
            foreach (var candidate in Resources.FindObjectsOfTypeAll<VP>())
            {
                if (!candidate) continue;
                uint key = GetIdentity(candidate);
                if (key == 0) continue;
                // Prefer the asset template to a live instance with attachments.
                if (!catalog.ContainsKey(key) || !candidate.gameObject.scene.IsValid())
                    catalog[key] = candidate;
            }
            if (!catalog.TryGetValue(remote.VehicleId, out source) || !source) return false;
        }

        var visual = new GameObject("RemoteVehicle_VisualOnly");
        visual.transform.SetParent(destination.transform, false);
        visual.transform.localScale = source.transform.lossyScale;
        transforms.Clear();
        transforms[source.transform.GetInstanceID()] = visual.transform;
        int count = 0;
        foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!renderer || !renderer.enabled || !filter.sharedMesh ||
                !IsVisible(filter.transform, source, remote.SkinMask)) continue;
            Transform target = CopyTransform(filter.transform, source.transform);
            var meshFilter = target.gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = filter.sharedMesh;
            var meshRenderer = target.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = renderer.sharedMaterials;
            meshRenderer.shadowCastingMode = renderer.shadowCastingMode;
            meshRenderer.receiveShadows = renderer.receiveShadows;
            count++;
        }
        foreach (var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!renderer.enabled || !renderer.sharedMesh ||
                !IsVisible(renderer.transform, source, remote.SkinMask)) continue;
            var mesh = new Mesh();
            bakedMeshes.Add(mesh);
            renderer.BakeMesh(mesh);
            Transform target = CopyTransform(renderer.transform, source.transform);
            var filter = target.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var copy = target.gameObject.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            count++;
        }
        transforms.Clear();
        if (count == 0)
        {
            Object.Destroy(visual);
            return false;
        }
        Log.LogInfo($"PID {Environment.ProcessId}: visual vehicle {remote.VehicleId:X8}, skin {remote.SkinMask:X16}, {count} meshes from {source.name}");
        return true;
    }

    private Transform CopyTransform(Transform source, Transform boundary)
    {
        int id = source.GetInstanceID();
        if (transforms.TryGetValue(id, out var result)) return result;
        var parent = CopyTransform(source.parent, boundary);
        var node = new GameObject(source.name) { layer = 2 }; // Ignore Raycast.
        result = node.transform;
        result.SetParent(parent, false);
        result.localPosition = source.localPosition;
        result.localRotation = source.localRotation;
        result.localScale = source.localScale;
        transforms[id] = result;
        return result;
    }

    private static bool IsVisible(Transform transform, VP source, ulong mask)
    {
        var skins = source.myVehicleSkinElements;
        // activeInHierarchy is false on prefab assets, so evaluate activeSelf
        // along the visual branch and override the skin roots with peer state.
        for (var node = transform; node && node != source.transform; node = node.parent)
        {
            bool active = node.gameObject.activeSelf;
            if (skins != null)
                for (int i = 0; i < Math.Min(64, skins.Length); i++)
                    if (skins[i] && skins[i].transform == node)
                    {
                        active = (mask & (1UL << i)) != 0;
                        break;
                    }
            if (!active) return false;
        }
        return true;
    }

    public void ClearMeshes()
    {
        foreach (var mesh in bakedMeshes) if (mesh) Object.Destroy(mesh);
        bakedMeshes.Clear();
        transforms.Clear();
    }

    public void Dispose()
    {
        ClearMeshes();
        catalog.Clear();
        catalogScene = int.MinValue;
        nextScan = 0;
    }
}
