using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Driving effects explicitly registered by any vehicle, including tracks without
// wheel-contact transforms. Never inspect arbitrary weapon VFX branches.
internal sealed class RemoteDrivingEffects : IDisposable
{
    private readonly List<GameObject> clones = new();
    private readonly List<TrailRenderer> trails = new();
    private sealed class Particle
    {
        public ParticleSystem System;
        public bool Engine, Running, Manual;
        public float Rate, Started, Carry;
    }
    private readonly List<Particle> particles = new();
    private Vector3 lastPosition;
    private float lastMotion = float.NegativeInfinity;
    private bool initialized, emitting, failed;
    public bool HasTrails => !failed && trails.Count > 0;
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Wheels");

    internal static IEnumerable<Transform> Sources(VP source)
    {
        if (!source) yield break;
        if (source.myTrails != null)
            foreach (var trail in source.myTrails) if (trail) yield return trail.transform;
        if (source.df && source.df.drivingParticles != null)
            foreach (var particle in source.df.drivingParticles) if (particle) yield return particle.transform;
        if (source.df && source.df.engineSmokeParticles != null)
            foreach (var particle in source.df.engineSmokeParticles) if (particle) yield return particle.transform;
    }

    private static bool IsEngine(VP source, Transform t) => source.df &&
        source.df.engineSmokeParticles != null && source.df.engineSmokeParticles.Any(p => p && p.transform == t);

    private static float Rate(VP source, Transform t, bool engine)
    {
        var feedback = source.df;
        if (feedback && feedback.drivingParticles != null && feedback.drivingParticlesEmissionRate != null)
            for (int i = 0; i < Math.Min(feedback.drivingParticles.Count, feedback.drivingParticlesEmissionRate.Count); i++)
                if (feedback.drivingParticles[i] && feedback.drivingParticles[i].transform == t)
                {
                    float rate = feedback.drivingParticlesEmissionRate[i];
                    if (float.IsFinite(rate) && rate > 0) return Math.Clamp(rate, 1, 60);
                }
        return engine ? 6 : 8; // Explicit bounded fallback cadence, not native timing.
    }

    public void Build(VP source, Func<Transform, Transform> map)
    {
        Dispose(); failed = false;
        var seen = new HashSet<int>();
        try
        {
            foreach (var t in Sources(source))
            {
                if (clones.Count == 16 || !seen.Add(t.GetInstanceID())) continue;
                bool underVehicle = false;
                for (var p = t; p; p = p.parent) if (p == source.transform) underVehicle = true;
                if (!underVehicle) continue;
                bool safe = true;
                foreach (var c in t.GetComponentsInChildren<Component>(true))
                    if (!c || (c.TryCast<Transform>() == null && c.TryCast<TrailRenderer>() == null &&
                        c.TryCast<ParticleSystem>() == null && c.TryCast<ParticleSystemRenderer>() == null))
                    { safe = false; break; }
                if (!safe) { Log.LogWarning($"Wheel effect {t.name} rejected: non-cosmetic component."); continue; }
                // The mapped anchor carries this source's animated transform.
                var anchor = map(t);
                var holder = new GameObject("Coop_WheelEffectHolder");
                holder.SetActive(false);
                holder.transform.SetParent(anchor, false);
                clones.Add(holder);
                var copy = Object.Instantiate(t.gameObject, holder.transform, false);
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;
                foreach (var n in copy.GetComponentsInChildren<Transform>(true)) n.gameObject.layer = 2;
                foreach (var trail in copy.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.emitting = false; trail.Clear(); trails.Add(trail);
                }
                foreach (var particle in copy.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    bool engine = IsEngine(source, t);
                    particles.Add(new Particle { System = particle, Engine = engine, Rate = Rate(source, t, engine) });
                }
                copy.SetActive(true);
                holder.SetActive(true);
                foreach (var particle in particles)
                    if (particle.System) particle.System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                Log.LogInfo($"Native wheel effect {t.name}: authored trail/particle settings retained.");
            }
            Log.LogInfo($"Native driving effects for {source.name}, drive={(source.df ? source.df.myDriveType.ToString() : "unknown")}: trails={trails.Count}, particle systems={particles.Count}. Model variants require runtime validation.");
        }
        catch (Exception ex) { Disable(ex); }
    }

    public void Update(Transform root, bool fresh)
    {
        if (failed || !root) return;
        try
        {
            var p = root.position;
            float distance = initialized ? Vector3.Distance(lastPosition, p) : 0;
            lastPosition = p; initialized = true;
            if (distance > 20) foreach (var trail in trails) if (trail) trail.Clear();
            if (distance > 20) lastMotion = float.NegativeInfinity;
            else if (distance > 0.002f) lastMotion = Time.unscaledTime;
            // Render frames between network samples must not repeatedly restart
            // native start delays or reset the zero-output observation window.
            bool moving = fresh && Time.unscaledTime - lastMotion < 0.2f;
            if (moving != emitting)
                foreach (var trail in trails) if (trail) trail.emitting = moving;
            emitting = moving;
            foreach (var particle in particles)
                if (particle.System)
                {
                    bool run = fresh && (particle.Engine || moving);
                    if (particle.Running != run)
                    {
                        particle.Running = run;
                        particle.Started = Time.unscaledTime;
                        particle.Manual = false; particle.Carry = 0;
                        if (run) particle.System.Play(false);
                        else particle.System.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    }
                    if (!run) continue;
                    // A live idle source can carry a zero emission rate. Do not
                    // guess or call stripped module setters: detect zero output.
                    if (!particle.Manual && Time.unscaledTime - particle.Started > 0.35f && particle.System.particleCount == 0)
                    {
                        particle.Manual = true;
                        Log.LogInfo($"Zero-output particle {particle.System.name}: explicit native Emit at {particle.Rate}/s; engine={particle.Engine}. Cadence may differ from original.");
                    }
                    if (particle.Manual)
                    {
                        particle.Carry += Mathf.Clamp(Time.unscaledDeltaTime, 0, 0.1f) * particle.Rate;
                        int count = (int)particle.Carry;
                        if (count > 0) { particle.System.Emit(count); particle.Carry -= count; }
                    }
                }
        }
        catch (Exception ex) { Disable(ex); }
    }

    private void Disable(Exception ex)
    {
        Dispose(); failed = true;
        Log.LogError($"Native wheel effects disabled; castle retained: {ex}");
    }

    public void Dispose()
    {
        foreach (var clone in clones) if (clone) Object.Destroy(clone);
        clones.Clear(); trails.Clear(); particles.Clear();
        initialized = emitting = false;
        lastMotion = float.NegativeInfinity;
    }
}
