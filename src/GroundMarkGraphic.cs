using UnityEngine;
using UnityEngine.Rendering;

namespace WanderburgCoopTest;

// Cache static mesh/material data only. Never Instantiate the effect prefab:
// its GroundEffect/particle/audio/gameplay scripts must not execute remotely.
internal sealed class GroundMarkGraphic
{
    private readonly record struct Part(Mesh Mesh, Material[] Materials, Vector3 Position, Quaternion Rotation, Vector3 Scale);
    private Part[] parts = Array.Empty<Part>();
    private Quaternion rotation;
    private Vector3 scale;
    public GameObject Source { get; private set; }
    public bool Available => parts.Length > 0;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op GroundGraphic");

    public void Configure(GameObject prefab)
    {
        Source = prefab;
        parts = Array.Empty<Part>();
        if (!prefab) return;
        rotation = prefab.transform.localRotation;
        scale = prefab.transform.localScale;
        if (scale.sqrMagnitude < 0.000000000001f)
        {
            Log.LogWarning($"Footprint prefab {prefab.name} has degenerate initial scale; using fallback ring instead of invisible geometry.");
            return;
        }
        var found = new List<Part>();
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!renderer || !renderer.enabled || !filter.sharedMesh) continue;
            bool active = true;
            for (var node = filter.transform; node && node != prefab.transform; node = node.parent)
                if (!node.gameObject.activeSelf) { active = false; break; }
            if (!active) continue;
            var matrix = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            found.Add(new(filter.sharedMesh, renderer.sharedMaterials, matrix.GetColumn(3), matrix.rotation, matrix.lossyScale));
            if (found.Count == 4) break; // Bound both pooling cost and draw calls.
        }
        parts = found.ToArray();
        Log.LogInfo($"PID {Environment.ProcessId}: footprint prefab={prefab.name}, static mesh parts={parts.Length}, root scale={scale}; graphics only, no prefab instantiation.");
    }

    public GameObject Build(Transform parent)
    {
        if (!Available) return null;
        var graphic = new GameObject("Coop_NativeFootprint_GraphicsOnly") { layer = 2 };
        graphic.SetActive(false);
        graphic.transform.SetParent(parent, false);
        graphic.transform.localRotation = rotation;
        graphic.transform.localScale = scale;
        foreach (var part in parts)
        {
            var node = new GameObject("Coop_FootprintMesh") { layer = 2 };
            node.transform.SetParent(graphic.transform, false);
            node.transform.localPosition = part.Position;
            node.transform.localRotation = part.Rotation;
            node.transform.localScale = part.Scale;
            node.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = part.Materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return graphic;
    }
}
