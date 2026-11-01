using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace WanderburgCoopTest;

// All access to native particles is on Unity's main thread. Read/write the
// generated public value fields; stripped convenience getters are not required.
internal static class CosmeticParticles
{
    internal static string Clean(string name) => name.Replace("(Clone)", "").Replace(" (Instance)", "").Trim();
    internal static uint Definition(ParticleSystem system)
    {
        var r = system.GetComponent<ParticleSystemRenderer>();
        if (!r || !r.sharedMaterial) return 0;
        return VehicleIdentity.FromModuleId("particle-v1:" + Clean(system.name) + "|" + r.renderMode + "|" +
            (r.renderMode == ParticleSystemRenderMode.Mesh && r.mesh ? r.mesh.name : "") + "|" +
            string.Join(";", r.sharedMaterials.Select(m => m ? Clean(m.name) : "none")));
    }
    internal static bool OwnedCopy(Transform t)
    {
        for (var p = t; p; p = p.parent)
            if (p.name.StartsWith("Coop_") || p.name.StartsWith("WanderburgCoop_") || p.name.StartsWith("RemoteVehicle_")) return true;
        return false;
    }
    internal static bool Safe(ParticleSystem system)
    {
        var components = system.GetComponentsInChildren<Component>(true);
        return components.Length <= 64 && components.All(c => c &&
            (c.TryCast<Transform>() != null || c.TryCast<ParticleSystem>() != null || c.TryCast<ParticleSystemRenderer>() != null));
    }
    internal static ParticleSample Capture(ParticleSystem.Particle p)
    {
        var a = p.m_Position; var b = p.m_Velocity; var c = p.m_AnimatedVelocity; var d = p.m_InitialVelocity;
        var e = p.m_AxisOfRotation; var f = p.m_Rotation; var g = p.m_AngularVelocity; var h = p.m_StartSize;
        var color = p.m_StartColor;
        return new ParticleSample(new[] {a.x,a.y,a.z,b.x,b.y,b.z,c.x,c.y,c.z,d.x,d.y,d.z,
            e.x,e.y,e.z,f.x,f.y,f.z,g.x,g.y,g.z,h.x,h.y,h.z,p.m_Lifetime,p.m_StartLifetime,p.m_EmitAccumulator0,p.m_EmitAccumulator1},
            (uint)(color.r | color.g << 8 | color.b << 16 | color.a << 24),
            p.m_RandomSeed, p.m_ParentRandomSeed, p.m_MeshIndex, p.m_Flags);
    }
    internal static ParticleSystem.Particle Restore(ParticleSample p)
    {
        var f = p.Values;
        return new ParticleSystem.Particle {
            m_Position=new(f[0],f[1],f[2]),m_Velocity=new(f[3],f[4],f[5]),m_AnimatedVelocity=new(f[6],f[7],f[8]),
            m_InitialVelocity=new(f[9],f[10],f[11]),m_AxisOfRotation=new(f[12],f[13],f[14]),m_Rotation=new(f[15],f[16],f[17]),
            m_AngularVelocity=new(f[18],f[19],f[20]),m_StartSize=new(f[21],f[22],f[23]),
            m_Lifetime=f[24],m_StartLifetime=f[25],m_EmitAccumulator0=f[26],m_EmitAccumulator1=f[27],
            m_StartColor=new Color32((byte)p.Color,(byte)(p.Color>>8),(byte)(p.Color>>16),(byte)(p.Color>>24)),
            m_RandomSeed=p.Seed,m_ParentRandomSeed=p.ParentSeed,m_MeshIndex=p.Mesh,m_Flags=p.Flags
        };
    }
}

internal sealed class ParticleCapture : IDisposable
{
    private VP source;
    private uint appearance;
    private float nextScan;
    private ParticleSystem[] systems = Array.Empty<ParticleSystem>();
    private Il2CppStructArray<ParticleSystem.Particle> buffer;
    public string Status { get; private set; } = "warte";
    private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Effects Capture");

    public ParticleFrame Capture(VP vehicle, PlayerSnapshot snapshot)
    {
        if (!NativeParticleBuffer.Ready) { Status = "Partikelstream deaktiviert"; return null; }
        if (buffer == null) buffer = new Il2CppStructArray<ParticleSystem.Particle>(ParticleFrame.PerEffect);
        if (!vehicle || !snapshot.InRun) { source = null; systems = Array.Empty<ParticleSystem>(); Status = "kein Run"; return null; }
        if (source != vehicle || appearance != snapshot.AppearanceId || Time.unscaledTime >= nextScan)
        {
            bool changed = source != vehicle || appearance != snapshot.AppearanceId;
            source = vehicle; appearance = snapshot.AppearanceId; nextScan = Time.unscaledTime + 1;
            var found = new Dictionary<int, ParticleSystem>();
            void Add(ParticleSystem p) { if (p && !CosmeticParticles.OwnedCopy(p.transform) && CosmeticParticles.Definition(p) != 0) found.TryAdd(p.GetInstanceID(), p); }
            foreach (var p in vehicle.GetComponentsInChildren<ParticleSystem>(true)) Add(p);
            var vm = GM.gm && GM.gm.vm ? GM.gm.vm : null;
            if (vm) foreach (var p in vm.GetComponentsInChildren<ParticleSystem>(true)) Add(p);
            foreach (var t in RemoteDrivingEffects.Sources(vehicle)) Add(t.GetComponent<ParticleSystem>());
            systems = found.Values.OrderByDescending(p => p.gameObject.activeInHierarchy && p.particleCount > 0)
                .Take(ParticleFrame.MaxEffects).ToArray();
            if (changed) Log.LogInfo($"Particle inventory {vehicle.name}: {found.Count} emitters, streaming {systems.Length}; names={string.Join(",", systems.Select(p => p.name))}. Owner hierarchy and registered driving effects only.");
        }
        int quota = Math.Min(ParticleFrame.PerEffect, ParticleFrame.MaxParticles / Math.Max(1, systems.Length));
        var effects = new List<ParticleEffect>();
        var ids = new HashSet<uint>();
        int count = 0, clipped = 0, invalid = 0;
        foreach (var system in systems)
        {
            if (!system) continue;
            var t = system.transform;
            var path = new List<string>();
            for (var p = t; p && p != vehicle.transform && path.Count < 32; p = p.parent)
                path.Add(CosmeticParticles.Clean(p.name) + "#" + p.GetSiblingIndex());
            uint id = VehicleIdentity.FromModuleId("effect:" + string.Join("/", path));
            if (!ids.Add(id)) continue;
            var pos = t.position; var rot = t.rotation; var scale = t.lossyScale;
            var pose = VisualRootPose.Capture(snapshot, new(pos.x,pos.y,pos.z), new(rot.x,rot.y,rot.z,rot.w), new(scale.x,scale.y,scale.z)) with { Id = id };
            if (!pose.IsValid) { invalid++; continue; }
            var particles = new List<ParticleSample>();
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (system.gameObject.activeInHierarchy && renderer && renderer.enabled)
            {
                int n = system.GetParticles(buffer, quota, 0);
                clipped += Math.Max(0, system.particleCount - n);
                for (int i = 0; i < n; i++)
                {
                    var sample = CosmeticParticles.Capture(buffer[i]);
                    if (sample.IsValid) particles.Add(sample); else invalid++;
                }
            }
            count += particles.Count;
            effects.Add(new(id, CosmeticParticles.Definition(system), pose, particles));
        }
        Status = $"{effects.Count} Emitter / {count} P. / Limit {clipped} / Fehler {invalid}";
        var frame = new ParticleFrame(snapshot.Scene, snapshot.VehicleId, snapshot.AppearanceId, effects);
        return frame.IsValid ? frame : null;
    }
    public void Dispose() { buffer = null; systems = Array.Empty<ParticleSystem>(); source = null; }
}
