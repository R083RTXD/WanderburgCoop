using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Native particle-only subtree replay. Never instantiate the full step prefab.
internal sealed class RemoteStepDust : IDisposable
{
    private const int Capacity = 64;
    private readonly record struct Template(GameObject Source, Vector3 Position, Quaternion Rotation, Vector3 Scale);
    private readonly List<Template> templates = new();
    private readonly GameObject[] bursts = new GameObject[Capacity];
    private readonly ParticleSystem[][] systems = new ParticleSystem[Capacity][];
    private readonly float[] born = new float[Capacity];
    private readonly DustRateGate rate = new();
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Dust");
    public int Emitted { get; private set; }
    public string ArtworkStatus => templates.Count > 0 ? "Original-Partikel" : "Original-Partikel fehlen";

    public void Configure(GameObject prefab)
    {
        Clear();
        templates.Clear();
        if (!prefab) return;
        foreach (var particle in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            bool nested = false;
            for (var p = particle.transform.parent; p; p = p.parent)
            {
                if (p.GetComponent<ParticleSystem>()) nested = true;
                if (p == prefab.transform) break;
            }
            if (nested) continue;
            bool safe = true;
            foreach (var component in particle.GetComponentsInChildren<Component>(true))
            {
                if (component && (component.TryCast<Transform>() != null ||
                    component.TryCast<ParticleSystem>() != null ||
                    component.TryCast<ParticleSystemRenderer>() != null)) continue;
                safe = false;
                Log.LogWarning($"Rejecting particle subtree {particle.name}: non-particle or missing component.");
                break;
            }
            if (!safe) continue;
            var matrix = prefab.transform.worldToLocalMatrix * particle.transform.localToWorldMatrix;
            templates.Add(new(particle.gameObject, matrix.GetColumn(3), matrix.rotation, matrix.lossyScale));
            Log.LogInfo($"PID {Environment.ProcessId}: native particle template {particle.name}; authored settings retained.");
            if (templates.Count == 4) break;
        }
        Log.LogInfo($"Native step particle templates={templates.Count}; no module setters or mesh approximation.");
    }

    public void Emit(Transform parent, Vector3 point, Vector3 normal, float now)
    {
        if (templates.Count == 0) return;
        int slot = Array.FindIndex(bursts, b => !b);
        if (!rate.TryTake(now)) return;
        if (slot < 0)
        {
            // Show every new accepted footfall; retire the oldest cosmetic burst.
            slot = Array.IndexOf(born, born.Min());
            bursts[slot].SetActive(false);
            Object.Destroy(bursts[slot]);
        }
        var root = new GameObject("Coop_NativeStepParticles") { layer = 2 };
        bursts[slot] = root; // Own partially constructed bursts for exception cleanup.
        root.SetActive(false);
        root.transform.SetParent(parent, false);
        root.transform.position = point + normal * 0.025f;
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        foreach (var template in templates)
        {
            // Inactive parent prevents premature play-on-awake during cloning.
            var child = Object.Instantiate(template.Source, root.transform, false);
            child.transform.localPosition = template.Position;
            child.transform.localRotation = template.Rotation;
            child.transform.localScale = template.Scale;
            foreach (var t in child.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            child.SetActive(true);
        }
        systems[slot] = root.GetComponentsInChildren<ParticleSystem>(true).ToArray();
        born[slot] = now;
        root.SetActive(true);
        foreach (var system in systems[slot])
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(false);
        }
        Emitted++;
    }

    public void Tick(float now)
    {
        for (int i = 0; i < bursts.Length; i++)
        {
            if (!bursts[i]) continue;
            bool alive = now - born[i] < 0.1f;
            foreach (var system in systems[i] ?? Array.Empty<ParticleSystem>())
                if (system && system.IsAlive(false)) alive = true;
            // Bound even unexpectedly looping or externally linked presets.
            if (alive && now - born[i] < 6) continue;
            Object.Destroy(bursts[i]);
            bursts[i] = null;
            systems[i] = null;
        }
    }

    public void Clear()
    {
        foreach (var burst in bursts) if (burst) Object.Destroy(burst);
        Array.Clear(bursts, 0, bursts.Length);
        Array.Clear(systems, 0, systems.Length);
        rate.Reset();
        Emitted = 0;
    }

    public void Dispose() { Clear(); templates.Clear(); }
}
