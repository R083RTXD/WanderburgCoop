using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

internal sealed class RemoteParticleView : IDisposable
{
    private sealed class Copy
    {
        public uint Definition;
        public GameObject Root;
        public ParticleSystem System;
        public Il2CppStructArray<ParticleSystem.Particle> Buffer;
        public void Dispose()
        {
            if (Root) { Root.SetActive(false); Object.Destroy(Root); }
            if (Buffer != null)
            {
                Buffer = null;
            }
        }
    }
    private readonly Dictionary<uint, Copy> copies = new();
    private readonly Dictionary<uint, ParticleSystem> templates = new();
    internal readonly HashSet<uint> Definitions = new();
    private ParticleFrame lastFrame;
    private float nextScan;
    private int scanScene = int.MinValue;
    private bool failed;
    public string Status { get; private set; } = "warte";
    private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Effects View");

    public void Update(Transform parent, ParticleFrame frame, PlayerSnapshot remote)
    {
        if (!NativeParticleBuffer.Ready) { Clear(); Status = "Partikelstream deaktiviert"; return; }
        if (!parent || frame == null || frame.Scene != remote.Scene || frame.Vehicle != remote.VehicleId || frame.Appearance != remote.AppearanceId)
        { Clear(); return; }
        if (failed) return;
        try
        {
            bool missing = frame.Effects.Any(e => !templates.TryGetValue(e.Definition, out var t) || !t);
            if (scanScene != frame.Scene || missing && Time.unscaledTime >= nextScan)
            {
                templates.Clear(); scanScene = frame.Scene; nextScan = Time.unscaledTime + 2;
                foreach (var p in Resources.FindObjectsOfTypeAll<ParticleSystem>())
                {
                    if (!p || CosmeticParticles.OwnedCopy(p.transform)) continue;
                    uint definition = CosmeticParticles.Definition(p);
                    if (definition != 0 && !templates.ContainsKey(definition) && CosmeticParticles.Safe(p)) templates.Add(definition, p);
                }
                Log.LogInfo($"Safe local particle definitions={templates.Count}; scene={frame.Scene}.");
            }
            if (ReferenceEquals(lastFrame, frame)) return;
            lastFrame = frame;
            var ids = new HashSet<uint>(frame.Effects.Select(e => e.Id));
            foreach (uint id in copies.Keys.Where(id => !ids.Contains(id)).ToArray()) { copies[id].Dispose(); copies.Remove(id); }
            Definitions.Clear();
            int particles = 0, unmatched = 0;
            foreach (var effect in frame.Effects)
            {
                if (copies.TryGetValue(effect.Id, out var copy) && (!copy.Root || copy.Root.transform.parent != parent || copy.Definition != effect.Definition))
                { copy.Dispose(); copies.Remove(effect.Id); copy = null; }
                if (copy == null)
                {
                    if (!templates.TryGetValue(effect.Definition, out var template) || !template) { unmatched++; continue; }
                    var root = new GameObject("Coop_OwnerParticles");
                    root.SetActive(false); root.transform.SetParent(parent, false);
                    copy = new Copy { Root = root, Definition = effect.Definition };
                    copies.Add(effect.Id, copy); // Own partial construction if a native call fails.
                    copy.Buffer = new Il2CppStructArray<ParticleSystem.Particle>(ParticleFrame.PerEffect);
                    var child = Object.Instantiate(template.gameObject, root.transform, false);
                    child.transform.localPosition = Vector3.zero; child.transform.localRotation = Quaternion.identity; child.transform.localScale = Vector3.one;
                    foreach (var t in child.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
                    child.SetActive(true); root.SetActive(true);
                    foreach (var system in child.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                        system.Play(false); system.Pause(false);
                    }
                    copy.System = child.GetComponent<ParticleSystem>();
                    var renderer = child.GetComponent<ParticleSystemRenderer>();
                    if (renderer) renderer.enabled = true; // Owner sends zero particles when hidden.
                }
                var p = effect.Pose;
                copy.Root.transform.localPosition = new(p.X,p.Y,p.Z);
                copy.Root.transform.localRotation = new(p.Qx,p.Qy,p.Qz,p.Qw);
                copy.Root.transform.localScale = new(p.Sx,p.Sy,p.Sz);
                // Paused native rendering: owner data supplies birth parameters
                // and age. No local guessed rates, Play loop or extra simulation.
                for (int i = 0; i < effect.Particles.Count; i++) copy.Buffer[i] = CosmeticParticles.Restore(effect.Particles[i]);
                copy.System.SetParticles(copy.Buffer, effect.Particles.Count, 0);
                particles += effect.Particles.Count;
                Definitions.Add(effect.Definition);
            }
            Status = $"{copies.Count} Effekte / {particles} Partikel / fehlen {unmatched}";
        }
        catch (Exception ex)
        {
            Clear(); failed = true; Status = "Fehler - siehe Log";
            Log.LogError($"Owner particle rendering disabled; previous cosmetic fallback retained: {ex}");
        }
    }

    public void Clear()
    {
        foreach (var copy in copies.Values) copy.Dispose();
        copies.Clear(); Definitions.Clear(); lastFrame = null; Status = "warte";
    }
    public void Dispose() { Clear(); templates.Clear(); failed = false; scanScene = int.MinValue; }
}
