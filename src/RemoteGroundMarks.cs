using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Approximate cosmetic ground marks, derived from the received render rig.
// Never instantiate native leg controllers or effect prefabs with scripts.
internal sealed class RemoteGroundMarks : IDisposable
{
    private const int MaxMarks = 256;
    private const float Lifetime = 14;
    private readonly GroundMarkGate[] gates = Enumerable.Range(0, 16).Select(_ => new GroundMarkGate()).ToArray();
    private readonly GameObject[] marks = new GameObject[MaxMarks];
    private readonly GameObject[] nativeGraphics = new GameObject[MaxMarks];
    private readonly MeshRenderer[] simpleRenderers = new MeshRenderer[MaxMarks];
    private readonly MeshFilter[] simpleFilters = new MeshFilter[MaxMarks];
    private readonly GroundMarkGraphic nativeGraphic = new();
    private readonly float[] born = new float[MaxMarks];
    private GameObject worldRoot;
    private Material material;
    private Mesh wheelMesh, footMesh;
    private int cursor;
    private bool initialized;
    private Vector3 lastPosition;
    private float lastYaw, nextSample;
    private int diagnosticSamples;
    private float nextDiagnostic;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Ground");
    public int Emitted { get; private set; }
    public string Status { get; private set; } = "warte";

    public void Update(Transform vehicle, IReadOnlyList<VehicleModel.GroundContact> contacts, bool freshPose, GameObject footprintPrefab)
    {
        if (vehicle && nativeGraphic.Source != footprintPrefab)
        {
            Clear();
            nativeGraphic.Configure(footprintPrefab);
        }
        float now = Time.unscaledTime;
        for (int i = 0; i < marks.Length; i++)
        {
            var mark = marks[i];
            if (!mark || !mark.activeSelf) continue;
            float remaining = Lifetime - (now - born[i]);
            if (remaining <= 0) mark.SetActive(false);
            else mark.transform.localScale = Vector3.one * Mathf.Clamp01(remaining / 2);
        }
        if (!vehicle || !freshPose)
        {
            Suspend();
            Status = "warte auf Pose";
            return;
        }
        if (now < nextSample) return;
        nextSample = now + 0.08f; // <=12.5 Hz; at most 16 non-trigger raycasts.
        var position = vehicle.position;
        float yaw = vehicle.eulerAngles.y;
        if (!initialized)
        {
            initialized = true;
            lastPosition = position; lastYaw = yaw;
            return;
        }
        float distance = Vector3.Distance(position, lastPosition);
        bool moving = distance > 0.025f || Math.Abs(Mathf.DeltaAngle(yaw, lastYaw)) > 0.8f;
        lastPosition = position; lastYaw = yaw;
        if (distance > 20) { Suspend(); return; }
        int grounded = 0;
        bool probe = moving && diagnosticSamples < 4 && now >= nextDiagnostic;
        if (probe) { diagnosticSamples++; nextDiagnostic = now + 5; }
        for (int i = 0; i < Math.Min(contacts.Count, gates.Length); i++)
        {
            var contact = contacts[i];
            var t = contact.Transform;
            if (!t) { gates[i].Reset(); continue; }
            var p = t.position;
            bool hasHit = Physics.Raycast(p + Vector3.up * 2, Vector3.down, out RaycastHit hit,
                8, ~(1 << 2), QueryTriggerInteraction.Ignore);
            bool ground = hasHit && hit.normal.y > 0.65f &&
                hit.collider && !hit.collider.attachedRigidbody &&
                Math.Abs(p.y - hit.point.y) < (contact.Foot ? 0.6f : 3f);
            if (ground) grounded++;
            if (probe && i == 0)
            {
                string hitInfo = hasHit && hit.collider
                    ? $"{hit.collider.name}, layer={hit.collider.gameObject.layer}, point={hit.point}, normal={hit.normal}, rigidbody={!!hit.collider.attachedRigidbody}, deltaY={p.y - hit.point.y:F3}"
                    : "none";
                var vm = GM.gm && GM.gm.vm ? GM.gm.vm : null;
                Log.LogInfo($"PID {Environment.ProcessId}: contacts={contacts.Count}, moving={moving}, foot={contact.Foot}, node={t.name}, position={p}, ground={ground}, hit={hitInfo}, nativePlane={(vm && vm.playerGroundPlaneInitialized ? vm.playerGroundPlaneY.ToString() : "uninitialized")}, stamps={Emitted}");
            }
            if (!gates[i].Sample(p.x, p.z, ground, moving, contact.Foot)) continue;
            if (EnsureResources()) Stamp(hit.point, hit.normal, vehicle.forward, contact.Foot, now);
        }
        Status = contacts.Count == 0 ? "keine Kontaktpunkte" : $"{contacts.Count} Kontakte / {Emitted} ({(nativeGraphic.Available ? "Originalgrafik" : "Ersatzring")})";
    }

    public void Suspend()
    {
        initialized = false;
        foreach (var gate in gates) gate.Reset();
    }

    private bool EnsureResources()
    {
        if (worldRoot && material && wheelMesh && footMesh) return true;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (!shader) { Status = "kein Spur-Shader"; return false; }
        if (!material)
        {
            material = new Material(shader);
            var color = new Color(0.53f, 0.36f, 0.23f, 1);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
        if (!worldRoot)
        {
            worldRoot = new GameObject("WanderburgCoop_GroundMarks_VisualOnly");
            SceneManager.MoveGameObjectToScene(worldRoot, SceneManager.GetActiveScene());
        }
        if (!wheelMesh)
            wheelMesh = MakeMesh(new[] { new Vector3(-0.18f, 0, -0.28f), new Vector3(-0.18f, 0, 0.28f),
                new Vector3(0.18f, 0, 0.28f), new Vector3(0.18f, 0, -0.28f) }, new[] { 0, 1, 2, 0, 2, 3 });
        if (!footMesh) footMesh = MakeFootRing();
        return true;
    }

    private static Mesh MakeFootRing()
    {
        const int segments = 16;
        var vertices = new Vector3[segments * 2];
        var indices = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices[i * 2] = radial * (1.65f + 0.13f * Mathf.Sin(i * 7));
            vertices[i * 2 + 1] = radial * (1.15f + 0.1f * Mathf.Sin(i * 3));
            int next = (i + 1) % segments * 2;
            int n = i * 6;
            indices[n] = i * 2; indices[n + 1] = i * 2 + 1; indices[n + 2] = next;
            indices[n + 3] = next; indices[n + 4] = i * 2 + 1; indices[n + 5] = next + 1;
        }
        return MakeMesh(vertices, indices);
    }

    private static Mesh MakeMesh(Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Stamp(Vector3 point, Vector3 normal, Vector3 forward, bool foot, float now)
    {
        int index = cursor++ % MaxMarks;
        var mark = marks[index];
        if (!mark)
        {
            mark = new GameObject("Coop_GroundMark") { layer = 2 };
            mark.SetActive(false);
            mark.transform.SetParent(worldRoot.transform, false);
            simpleFilters[index] = mark.AddComponent<MeshFilter>();
            var renderer = mark.AddComponent<MeshRenderer>();
            simpleRenderers[index] = renderer;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            marks[index] = mark;
            nativeGraphics[index] = nativeGraphic.Build(mark.transform);
        }
        bool useNative = foot && nativeGraphics[index];
        if (nativeGraphics[index]) nativeGraphics[index].SetActive(useNative);
        simpleRenderers[index].enabled = !useNative;
        simpleFilters[index].sharedMesh = foot ? footMesh : wheelMesh;
        var tangent = Vector3.ProjectOnPlane(forward, normal);
        if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.ProjectOnPlane(Vector3.forward, normal);
        mark.transform.SetPositionAndRotation(point + normal * 0.025f, Quaternion.LookRotation(tangent, normal));
        mark.transform.localScale = Vector3.one;
        born[index] = now;
        mark.SetActive(true);
        Emitted++;
    }

    public void Clear()
    {
        Suspend();
        if (worldRoot) Object.Destroy(worldRoot);
        worldRoot = null;
        Array.Clear(marks, 0, marks.Length);
        Array.Clear(nativeGraphics, 0, nativeGraphics.Length);
        Array.Clear(simpleRenderers, 0, simpleRenderers.Length);
        Array.Clear(simpleFilters, 0, simpleFilters.Length);
        cursor = Emitted = 0;
        Status = "warte";
    }

    public void Dispose()
    {
        Clear();
        if (material) Object.Destroy(material);
        if (wheelMesh) Object.Destroy(wheelMesh);
        if (footMesh) Object.Destroy(footMesh);
        material = null; wheelMesh = footMesh = null;
    }
}
