using System.Globalization;

namespace WanderburgCoopTest;

// Fixed numeric schema of the generated Particle struct. No Unity references,
// pointers, asset paths or script instructions cross the worker boundary.
public sealed class ParticleSample
{
    private readonly float[] values;
    public IReadOnlyList<float> Values { get; }
    public uint Color { get; }
    public uint Seed { get; }
    public uint ParentSeed { get; }
    public int Mesh { get; }
    public uint Flags { get; }
    public ParticleSample(IEnumerable<float> values, uint color, uint seed, uint parentSeed, int mesh, uint flags)
    {
        this.values = values.Take(29).ToArray(); Values = Array.AsReadOnly(this.values);
        Color = color; Seed = seed; ParentSeed = parentSeed; Mesh = mesh; Flags = flags;
    }
    public bool IsValid => values.Length == 28 && values.All(v => float.IsFinite(v) && Math.Abs(v) <= 100000) &&
        values[24] >= 0 && values[25] is > 0 and <= 3600 && values[24] <= values[25] + 1 &&
        Mesh is >= -1 and <= 1024 && Flags <= 65535;
    internal void Write(BinaryWriter w)
    {
        foreach (float f in values) w.Write(f);
        w.Write(Color); w.Write(Seed); w.Write(ParentSeed); w.Write(Mesh); w.Write(Flags);
    }
    internal static ParticleSample Read(BinaryReader r) => new(Enumerable.Range(0, 28).Select(_ => r.ReadSingle()).ToArray(),
        r.ReadUInt32(), r.ReadUInt32(), r.ReadUInt32(), r.ReadInt32(), r.ReadUInt32());
}

public sealed class ParticleEffect
{
    public uint Id { get; }
    public uint Definition { get; }
    public VisualNodePose Pose { get; }
    public IReadOnlyList<ParticleSample> Particles { get; }
    public ParticleEffect(uint id, uint definition, VisualNodePose pose, IEnumerable<ParticleSample> particles)
    {
        Id = id; Definition = definition; Pose = pose;
        Particles = Array.AsReadOnly(particles.Take(ParticleFrame.PerEffect + 1).ToArray());
    }
    public bool IsValid => Id != 0 && Definition != 0 && Pose.Id == Id && Pose.IsValid &&
        Particles.Count <= ParticleFrame.PerEffect && Particles.All(p => p.IsValid);
}

public sealed class ParticleFrame
{
    public const int MaxEffects = 32, PerEffect = 128, MaxParticles = 1024, ChunkBytes = 720, MaxBytes = 140000;
    public int Scene { get; }
    public uint Vehicle { get; }
    public uint Appearance { get; }
    public IReadOnlyList<ParticleEffect> Effects { get; }
    public bool IsValid => Scene is >= 0 and <= 10000 && Vehicle != 0 && Appearance != 0 &&
        Effects.Count <= MaxEffects && Effects.All(e => e.IsValid) && Effects.Sum(e => e.Particles.Count) <= MaxParticles &&
        Effects.Select(e => e.Id).Distinct().Count() == Effects.Count;
    public ParticleFrame(int scene, uint vehicle, uint appearance, IEnumerable<ParticleEffect> effects)
    {
        Scene = scene; Vehicle = vehicle; Appearance = appearance;
        Effects = Array.AsReadOnly(effects.Take(MaxEffects + 1).ToArray());
    }
    public IEnumerable<string> ToPayloads(long sequence)
    {
        if (!IsValid || sequence < 0) yield break;
        using var memory = new MemoryStream();
        using (var w = new BinaryWriter(memory, System.Text.Encoding.UTF8, true))
        {
            w.Write(Effects.Count);
            foreach (var e in Effects)
            {
                w.Write(e.Id); w.Write(e.Definition);
                var p = e.Pose;
                foreach (float f in new[] {p.X,p.Y,p.Z,p.Qx,p.Qy,p.Qz,p.Qw,p.Sx,p.Sy,p.Sz}) w.Write(f);
                w.Write(e.Particles.Count);
                foreach (var particle in e.Particles) particle.Write(w);
            }
        }
        var bytes = memory.ToArray();
        int count = (bytes.Length + ChunkBytes - 1) / ChunkBytes;
        for (int i = 0; i < count; i++)
            yield return FormattableString.Invariant($"{sequence}|{Scene}|{Vehicle}|{Appearance}|{i}|{count}|") +
                Convert.ToBase64String(bytes, i * ChunkBytes, Math.Min(ChunkBytes, bytes.Length - i * ChunkBytes));
    }
    internal static ParticleFrame Decode(int scene, uint vehicle, uint appearance, byte[] bytes)
    {
        try
        {
            using var r = new BinaryReader(new MemoryStream(bytes));
            int count = r.ReadInt32();
            if (count < 0 || count > MaxEffects) return null;
            var effects = new List<ParticleEffect>();
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                uint id = r.ReadUInt32(), definition = r.ReadUInt32();
                var p = new VisualNodePose(id,r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),
                    r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
                int n = r.ReadInt32(); total += n;
                if (n < 0 || n > PerEffect || total > MaxParticles) return null;
                var particles = new ParticleSample[n];
                for (int j = 0; j < n; j++) particles[j] = ParticleSample.Read(r);
                effects.Add(new(id, definition, p, particles));
            }
            if (r.BaseStream.Position != bytes.Length) return null;
            var frame = new ParticleFrame(scene, vehicle, appearance, effects);
            return frame.IsValid ? frame : null;
        }
        catch (EndOfStreamException) { return null; }
    }
}

public sealed class ParticleAssembler
{
    private long assembling = -1, published = -1;
    private int scene;
    private uint vehicle, appearance;
    private byte[][] chunks;
    public void Reset() { assembling = published = -1; chunks = null; }
    public bool Accept(string[] p, out ParticleFrame frame)
    {
        frame = null;
        if (p.Length != 10 || !long.TryParse(p[3], NumberStyles.None, CultureInfo.InvariantCulture, out long seq) ||
            seq < 0 || seq <= published || seq < assembling ||
            !int.TryParse(p[4], NumberStyles.None, CultureInfo.InvariantCulture, out int s) || s < 0 || s > 10000 ||
            !uint.TryParse(p[5], NumberStyles.None, CultureInfo.InvariantCulture, out uint v) || v == 0 ||
            !uint.TryParse(p[6], NumberStyles.None, CultureInfo.InvariantCulture, out uint a) || a == 0 ||
            !int.TryParse(p[7], NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            !int.TryParse(p[8], NumberStyles.None, CultureInfo.InvariantCulture, out int count) ||
            count < 1 || count > (ParticleFrame.MaxBytes + ParticleFrame.ChunkBytes - 1) / ParticleFrame.ChunkBytes ||
            index < 0 || index >= count || p[9].Length > 960) return false;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(p[9]); } catch (FormatException) { return false; }
        if (bytes.Length < 1 || bytes.Length > ParticleFrame.ChunkBytes ||
            index < count - 1 && bytes.Length != ParticleFrame.ChunkBytes) return false;
        if (seq > assembling)
        {
            assembling = seq; scene = s; vehicle = v; appearance = a; chunks = new byte[count][];
        }
        if (chunks == null || chunks.Length != count || scene != s || vehicle != v || appearance != a || chunks[index] != null) return false;
        chunks[index] = bytes;
        if (chunks.Any(c => c == null)) return false;
        using var assembled = new MemoryStream();
        foreach (var chunk in chunks) assembled.Write(chunk, 0, chunk.Length);
        var combined = assembled.ToArray();
        if (combined.Length > ParticleFrame.MaxBytes) return false;
        frame = ParticleFrame.Decode(scene, vehicle, appearance, combined);
        if (frame == null) return false;
        published = seq; return true;
    }
}
