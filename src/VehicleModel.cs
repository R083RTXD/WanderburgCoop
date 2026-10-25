using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Construct a render-only representation. Never Instantiate a VP/VM prefab:
// even temporarily disabled game components can execute Awake/OnEnable.
internal sealed class VehicleModel : IDisposable
{
    private readonly List<Mesh> bakedMeshes = new();
    private readonly Dictionary<int, Transform> transforms = new();
    private readonly Dictionary<uint, Transform> poseTargets = new();
    internal readonly record struct GroundContact(Transform Transform, bool Foot, Renderer[] Soles = null);
    internal readonly List<GroundContact> GroundContacts = new();
    internal GameObject FootprintPrefab { get; private set; }
    private readonly RemoteDrivingEffects drivingEffects = new();
    internal bool HasNativeWheelTrails => drivingEffects.HasTrails;
    internal void UpdateDrivingEffects(bool fresh) => drivingEffects.Update(visualRoot, fresh);
    private bool poseInitialized;
    public int AnimatedNodeCount => poseTargets.Count;
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

    internal readonly record struct PoseSource(uint Id, Transform Transform, bool VehicleRelative,
        bool OwnerFrame = false, Renderer[] Soles = null);

    private static List<GroundContact> GetGroundContacts(VP vehicle)
    {
        var result = new List<GroundContact>();
        var seen = new HashSet<int>();
        var feedback = vehicle.df;
        if (!feedback) return result;
        var parts = GetParts(vehicle);
        void Add(Transform t, bool foot, Renderer[] soles = null)
        {
            if (t && result.Count < 16 && seen.Add(t.GetInstanceID())) result.Add(new(t, foot, soles));
        }
        void AddFoot(DitzelGames.FastIK.FastIKFabric leg)
        {
            if (!leg || !leg.Target) return;
            // The game's IK endpoint has a ~5-unit graphical offset. Use only
            // that detached leg's renderers to measure its visible lower edge;
            // never use the body's bounds or relax terrain collision checks.
            var soles = parts.Where(p => p.Category != "body" && IsUnder(leg.transform, p.Boundary))
                .Select(p => p.Renderer).ToArray();
            Add(leg.Target, true, soles);
        }
        if (feedback.spc && feedback.spc.myLegs != null)
            foreach (var leg in feedback.spc.myLegs) AddFoot(leg);
        if (feedback.spc2 && feedback.spc2.myLegs != null)
            foreach (var leg in feedback.spc2.myLegs) AddFoot(leg);
        if (result.Count == 0)
        {
            if (feedback.leftSideWheels != null) foreach (var t in feedback.leftSideWheels) Add(t, false);
            if (feedback.rightSideWheels != null) foreach (var t in feedback.rightSideWheels) Add(t, false);
        }
        return result;
    }

    internal static List<PoseSource> GetPoseSources(VP vehicle)
    {
        var result = new List<PoseSource>
        {
            new(VehicleIdentity.VisualRootId, vehicle.transform, false, true)
        };
        var seen = new HashSet<int>();
        var parts = GetParts(vehicle);
        foreach (var part in parts)
        {
            AddChain(part.Renderer.transform, part.Boundary, part.Category);
            var skinned = part.Renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned == null) continue;
            foreach (var bone in skinned.bones) if (bone) AddChain(bone, part.Boundary, part.Category);
            if (skinned.rootBone) AddChain(skinned.rootBone, part.Boundary, part.Category);
        }
        // IK targets can live outside the render hierarchy. Sample them in the
        // vehicle frame under dedicated IDs; do not treat the solver bone tip
        // (observed five units above terrain) as a planted foot contact.
        var contacts = GetGroundContacts(vehicle);
        // Tracks may register driving VFX without any wheel-contact transforms.
        // Only explicit vehicle effect references are considered; no fake feet.
        if (contacts.All(c => !c.Foot))
            foreach (var effect in RemoteDrivingEffects.Sources(vehicle)) AddChain(effect, vehicle.transform, "body");
        for (int i = 0; i < contacts.Count; i++)
            result.Add(new PoseSource(VehicleIdentity.FromGroundContact(i, contacts[i].Foot),
                contacts[i].Transform, true, Soles: contacts[i].Soles));
        return result;

        void AddChain(Transform transform, Transform boundary, string category)
        {
            if (!IsUnder(transform, boundary)) return;
            for (var node = transform; node && node != vehicle.transform; node = node.parent)
            {
                if (seen.Add(node.GetInstanceID()))
                {
                    var path = new List<string>();
                    for (var p = node; p && p != boundary; p = p.parent)
                        path.Add(p.name + "#" + p.GetSiblingIndex());
                    uint id = VehicleIdentity.FromModuleId("pose:" + category + "/" + string.Join("/", path));
                    result.Add(new PoseSource(id, node, node == boundary));
                }
                if (node == boundary) break;
            }
        }
    }

    private static bool IsUnder(Transform transform, Transform boundary)
    {
        for (var node = transform; node; node = node.parent) if (node == boundary) return true;
        return false;
    }

    public int ApplyPose(VisualPoseFrame frame)
    {
        if (frame == null) return 0;
        float blend = poseInitialized ? 1 - Mathf.Exp(-25 * Time.unscaledDeltaTime) : 1;
        int applied = 0;
        foreach (var pose in frame.Nodes)
        {
            if (!poseTargets.TryGetValue(pose.Id, out var target) || !target) continue;
            target.localPosition = Vector3.Lerp(target.localPosition, new Vector3(pose.X, pose.Y, pose.Z), blend);
            target.localRotation = Quaternion.Slerp(target.localRotation,
                new Quaternion(pose.Qx, pose.Qy, pose.Qz, pose.Qw).normalized, blend);
            target.localScale = Vector3.Lerp(target.localScale, new Vector3(pose.Sx, pose.Sy, pose.Sz), blend);
            applied++;
        }
        if (!poseInitialized) Log.LogInfo($"PID {Environment.ProcessId}: first animation pose matched {applied}/{frame.Nodes.Count} nodes.");
        poseInitialized = true;
        return applied;
    }

    private static List<VisualPart> GetParts(VP vehicle)
    {
        // Nitro is a transient VFX branch, not a different vehicle skin. Its
        // active mesh renderers must not change identity or the animation rig.
        var boostGroup = vehicle.df ? vehicle.df.boostVFXgroup : null;
        Transform boostRoot = boostGroup ? boostGroup.transform : null;
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
                    (!boostRoot || !IsUnder(filter.transform, boostRoot)) &&
                    seen.Add(renderer.GetInstanceID()))
                    result.Add(new VisualPart(renderer, filter.sharedMesh, root, category));
            }
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.enabled && renderer.sharedMesh && IsVisible(renderer.transform, root) &&
                    (!boostRoot || !IsUnder(renderer.transform, boostRoot)) &&
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
        var feedback = source.df;
        FootprintPrefab = feedback && feedback.spc2 ? feedback.spc2.stepEffectPrefab :
            feedback && feedback.spc ? feedback.spc.stepEffectPrefab : null;
        if (!FootprintPrefab && feedback && feedback.spc) FootprintPrefab = feedback.spc.stepEffectPrefab;

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
            var skinned = renderer.TryCast<SkinnedMeshRenderer>();
            bool canSkin = skinned != null && skinned.bones.Length > 0 &&
                skinned.bones.All(b => b && IsUnder(b, part.Boundary)) &&
                (!skinned.rootBone || IsUnder(skinned.rootBone, part.Boundary));
            Renderer meshRenderer;
            if (canSkin)
            {
                var copy = target.gameObject.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = skinned.sharedMesh;
                var bones = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform>(skinned.bones.Length);
                for (int i = 0; i < bones.Length; i++)
                    bones[i] = CopyTransform(skinned.bones[i], source.transform, part.Boundary);
                copy.bones = bones;
                if (skinned.rootBone) copy.rootBone = CopyTransform(skinned.rootBone, source.transform, part.Boundary);
                copy.localBounds = skinned.localBounds;
                copy.updateWhenOffscreen = true;
                meshRenderer = copy;
            }
            else
            {
                var meshFilter = target.gameObject.AddComponent<MeshFilter>();
                if (skinned != null)
                {
                    var mesh = new Mesh();
                    bakedMeshes.Add(mesh);
                    skinned.BakeMesh(mesh);
                    meshFilter.sharedMesh = mesh;
                }
                else meshFilter.sharedMesh = part.Mesh;
                meshRenderer = target.gameObject.AddComponent<MeshRenderer>();
            }
            meshRenderer.sharedMaterials = renderer.sharedMaterials;
            meshRenderer.shadowCastingMode = renderer.shadowCastingMode;
            meshRenderer.receiveShadows = renderer.receiveShadows;
            count++;
            if (part.Category != "body") legCount++;
        }
        GroundContacts.Clear();
        var sourceContacts = GetGroundContacts(source);
        for (int i = 0; i < sourceContacts.Count; i++)
        {
            var contact = sourceContacts[i];
            var anchor = new GameObject("GroundContact_" + i) { layer = 2 };
            anchor.transform.SetParent(visualRoot, false);
            anchor.transform.localPosition = source.transform.InverseTransformPoint(contact.Transform.position);
            GroundContacts.Add(new(anchor.transform, contact.Foot));
        }
        if (sourceContacts.All(c => !c.Foot))
            drivingEffects.Build(source, t => CopyTransform(t, source.transform, source.transform));
        poseTargets.Clear();
        foreach (var node in GetPoseSources(source))
            if (transforms.TryGetValue(node.Transform.GetInstanceID(), out var target)) poseTargets[node.Id] = target;
        for (int i = 0; i < GroundContacts.Count; i++)
            poseTargets[VehicleIdentity.FromGroundContact(i, GroundContacts[i].Foot)] = GroundContacts[i].Transform;
        transforms.Clear();
        if (count == 0)
        {
            Object.Destroy(visual);
            return false;
        }
        Log.LogInfo($"PID {Environment.ProcessId}: visual vehicle {remote.VehicleId:X8}, appearance {remote.AppearanceId:X8}, {count} meshes ({legCount} detached leg meshes), {poseTargets.Count} animation nodes from {source.name}, live={source.gameObject.scene.IsValid()}");
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
        drivingEffects.Dispose();
        foreach (var mesh in bakedMeshes) if (mesh) Object.Destroy(mesh);
        bakedMeshes.Clear();
        transforms.Clear();
        poseTargets.Clear();
        GroundContacts.Clear();
        poseInitialized = false;
        FootprintPrefab = null;
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
