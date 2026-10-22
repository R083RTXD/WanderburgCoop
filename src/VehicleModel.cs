using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Construct a render-only representation. Never Instantiate a VP/VM prefab:
// even temporarily disabled game components can execute Awake/OnEnable.
internal sealed class VehicleModel : IDisposable
{
    private readonly List<Mesh> bakedMeshes = new();
    private readonly Dictionary<int, Transform> transforms = new();
    private Transform visualRoot;
    private VP cachedSource;
    private float nextScan;
    private int catalogScene = int.MinValue;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Visual");

    public static uint GetIdentity(VP vehicle) => !vehicle ? 0 :
        VehicleIdentity.FromModuleId(string.IsNullOrEmpty(vehicle.moduleID)
            ? "prefab:" + vehicle.name.Replace("(Clone)", "").Trim()
            : vehicle.moduleID);

    public static uint GetAppearance(VP vehicle)
    {
        if (!vehicle) return 0;
        var parts = new List<string>();
        foreach (var part in GetParts(vehicle))
            parts.Add(DescribePart(part.Renderer, part.Mesh, part.Boundary, part.Category));
        return VehicleIdentity.FromAppearance(parts);
    }

    private readonly record struct VisualPart(Renderer Renderer, Mesh Mesh, Transform Boundary, string Category);

    private static List<VisualPart> GetParts(VP vehicle)
    {
        var roots = new List<(Transform root, string category)> { (vehicle.transform, "body") };
        var feedback = vehicle.df;
        if (feedback)
        {
            var spider = feedback.spc;
            if (spider && spider.legObjs != null)
                for (int i = 0; i < spider.legObjs.Count; i++)
                    if (spider.legObjs[i]) roots.Add((spider.legObjs[i].transform, "leg1/" + i));
            var spider2 = feedback.spc2;
            if (spider2 && spider2.myLegsObjParents != null)
                for (int i = 0; i < spider2.myLegsObjParents.Count; i++)
                    if (spider2.myLegsObjParents[i]) roots.Add((spider2.myLegsObjParents[i].transform, "leg2/" + i));
        }
        var result = new List<VisualPart>();
        var seen = new HashSet<int>();
        foreach (var (root, category) in roots)
        {
            if (!root || !root.gameObject.activeSelf) continue;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer && renderer.enabled && filter.sharedMesh && IsVisible(filter.transform, root) &&
                    seen.Add(renderer.GetInstanceID()))
                    result.Add(new VisualPart(renderer, filter.sharedMesh, root, category));
            }
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.enabled && renderer.sharedMesh && IsVisible(renderer.transform, root) &&
                    seen.Add(renderer.GetInstanceID()))
                    result.Add(new VisualPart(renderer, renderer.sharedMesh, root, category));
        }
        return result;
    }

    private static string DescribePart(Renderer renderer, Mesh mesh, Transform boundary, string category)
    {
        var path = new List<string>();
        for (var node = renderer.transform; node && node != boundary; node = node.parent)
            path.Add(node.name);
        var materials = new List<string>();
        foreach (var material in renderer.sharedMaterials)
            materials.Add(material ? material.name.Replace(" (Instance)", "") : "none");
        return category + "/" + string.Join("/", path) + "|" + mesh.name + "|" + mesh.vertexCount + "|" +
            mesh.subMeshCount + "|" + string.Join(";", materials);
    }

    private static bool Matches(VP source, PlayerSnapshot remote) => source &&
        remote.AppearanceId != 0 && GetIdentity(source) == remote.VehicleId &&
        GetAppearance(source) == remote.AppearanceId;

    public bool TryBuild(GameObject destination, PlayerSnapshot remote)
    {
        if (remote.VehicleId == 0 || remote.AppearanceId == 0) return false;
        if (catalogScene != remote.Scene)
        {
            cachedSource = null;
            catalogScene = remote.Scene;
            nextScan = 0;
        }
        // A live same-appearance vehicle contains the body actually selected by
        // the player. A resident prefab may still contain its default body.
        var local = GM.gm && GM.gm.vm ? GM.gm.vm.vp : null;
        VP source = Matches(local, remote) ? local : Matches(cachedSource, remote) ? cachedSource : null;
        if (!source)
        {
            if (Time.unscaledTime < nextScan) return false;
            nextScan = Time.unscaledTime + 2;
            // Includes unloaded-from-scene but resident prefab assets. No
            // instantiation, unlock changes, or calls into vehicle gameplay.
            foreach (var candidate in Resources.FindObjectsOfTypeAll<VP>())
            {
                if (!candidate) continue;
                if (!Matches(candidate, remote)) continue;
                source = candidate;
                break;
            }
            if (!source) return false;
        }
        cachedSource = source;

        var sourceScale = source.transform.lossyScale;
        if (Math.Abs(sourceScale.x) < 0.000001f || Math.Abs(sourceScale.y) < 0.000001f ||
            Math.Abs(sourceScale.z) < 0.000001f) return false;

        var visual = new GameObject("RemoteVehicle_VisualOnly");
        visual.transform.SetParent(destination.transform, false);
        visual.transform.localScale = sourceScale;
        visualRoot = visual.transform;
        transforms.Clear();
        transforms[source.transform.GetInstanceID()] = visual.transform;
        int count = 0;
        int legCount = 0;
        foreach (var part in GetParts(source))
        {
            var renderer = part.Renderer;
            Transform target = CopyTransform(renderer.transform, source.transform, part.Boundary);
            var meshFilter = target.gameObject.AddComponent<MeshFilter>();
            var skinned = renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                var mesh = new Mesh();
                bakedMeshes.Add(mesh);
                skinned.BakeMesh(mesh);
                meshFilter.sharedMesh = mesh;
            }
            else meshFilter.sharedMesh = part.Mesh;
            var meshRenderer = target.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = renderer.sharedMaterials;
            meshRenderer.shadowCastingMode = renderer.shadowCastingMode;
            meshRenderer.receiveShadows = renderer.receiveShadows;
            count++;
            if (part.Category != "body") legCount++;
        }
        transforms.Clear();
        if (count == 0)
        {
            Object.Destroy(visual);
            return false;
        }
        Log.LogInfo($"PID {Environment.ProcessId}: visual vehicle {remote.VehicleId:X8}, appearance {remote.AppearanceId:X8}, {count} meshes ({legCount} detached leg meshes) from {source.name}, live={source.gameObject.scene.IsValid()}");
        return true;
    }

    private Transform CopyTransform(Transform source, Transform vehicle, Transform boundary)
    {
        int id = source.GetInstanceID();
        if (transforms.TryGetValue(id, out var result)) return result;
        // Detached leg roots live in world space, not below VP. Rebase their
        // world pose into the vehicle frame rather than copying world parents.
        if (source == boundary)
        {
            var anchor = new GameObject("DetachedLeg_" + source.name) { layer = 2 };
            result = anchor.transform;
            result.SetParent(visualRoot, false);
            result.localPosition = vehicle.InverseTransformPoint(source.position);
            result.localRotation = Quaternion.Inverse(vehicle.rotation) * source.rotation;
            var scale = source.lossyScale;
            var ownerScale = vehicle.lossyScale;
            result.localScale = new Vector3(scale.x / ownerScale.x, scale.y / ownerScale.y, scale.z / ownerScale.z);
            transforms[id] = result;
            return result;
        }
        var parent = CopyTransform(source.parent, vehicle, boundary);
        var node = new GameObject(source.name) { layer = 2 }; // Ignore Raycast.
        result = node.transform;
        result.SetParent(parent, false);
        result.localPosition = source.localPosition;
        result.localRotation = source.localRotation;
        result.localScale = source.localScale;
        transforms[id] = result;
        return result;
    }

    private static bool IsVisible(Transform transform, Transform boundary)
    {
        // Inspect actual render branches, not a skin-index mask applied to a
        // different prefab. Prefab assets have activeInHierarchy=false.
        for (var node = transform; node && node != boundary; node = node.parent)
        {
            if (!node.gameObject.activeSelf) return false;
        }
        return true;
    }

    public void ClearMeshes()
    {
        foreach (var mesh in bakedMeshes) if (mesh) Object.Destroy(mesh);
        bakedMeshes.Clear();
        transforms.Clear();
        visualRoot = null;
    }

    public void Dispose()
    {
        ClearMeshes();
        cachedSource = null;
        catalogScene = int.MinValue;
        nextScan = 0;
    }
}
