using System.Globalization;

namespace WanderburgCoopTest;

public readonly record struct VisualNodePose(uint Id, float X, float Y, float Z,
    float Qx, float Qy, float Qz, float Qw, float Sx, float Sy, float Sz)
{
    // These are LOCAL transform scales, not world-space sizes. Imported rigs
    // use scale 100 and the spider stretches a node to 199.16663 during play.
    // Keep finite bounded validation without rejecting that native pose.
    public const float MaxLocalScale = 1000;
    private static bool Bound(float value, float limit) => float.IsFinite(value) && Math.Abs(value) <= limit;
    public bool IsValid => Id != 0 && Bound(X, 10000) && Bound(Y, 10000) && Bound(Z, 10000) &&
        Bound(Qx, 1) && Bound(Qy, 1) && Bound(Qz, 1) && Bound(Qw, 1) &&
        Qx * Qx + Qy * Qy + Qz * Qz + Qw * Qw is >= 0.25f and <= 2.25f &&
        Bound(Sx, MaxLocalScale) && Bound(Sy, MaxLocalScale) && Bound(Sz, MaxLocalScale);
}

// Immutable across the managed worker/main-thread boundary. No Unity types.
public sealed class VisualPoseFrame
{
    public const int MaxNodes = 256;
    public const int NodesPerChunk = 16;
    public int Scene { get; }
    public uint VehicleId { get; }
    public uint AppearanceId { get; }
    public IReadOnlyList<VisualNodePose> Nodes { get; }
    public bool IsValid { get; }
    public string InvalidReason { get; }

    public VisualPoseFrame(int scene, uint vehicleId, uint appearanceId, IEnumerable<VisualNodePose> nodes)
    {
        Scene = scene;
        VehicleId = vehicleId;
        AppearanceId = appearanceId;
        var copy = nodes.Take(MaxNodes + 1).ToArray();
        Nodes = Array.AsReadOnly(copy);
        IsValid = scene >= 0 && scene <= 10000 && vehicleId != 0 && appearanceId != 0 &&
            copy.Length is > 0 and <= MaxNodes && copy.All(n => n.IsValid) &&
            copy.Select(n => n.Id).Distinct().Count() == copy.Length;
        if (!IsValid)
        {
            if (scene < 0 || scene > 10000 || vehicleId == 0 || appearanceId == 0)
                InvalidReason = "invalid scene/vehicle/appearance identity";
            else if (copy.Length == 0 || copy.Length > MaxNodes)
                InvalidReason = $"node count {copy.Length}";
            else
            {
                int invalid = Array.FindIndex(copy, n => !n.IsValid);
                if (invalid >= 0)
                {
                    var n = copy[invalid];
                    InvalidReason = FormattableString.Invariant($"node[{invalid}] id={n.Id:X8}, position=({n.X:R},{n.Y:R},{n.Z:R}), quaternion=({n.Qx:R},{n.Qy:R},{n.Qz:R},{n.Qw:R}), scale=({n.Sx:R},{n.Sy:R},{n.Sz:R})");
                }
                else InvalidReason = "duplicate node identities";
            }
        }
    }

    public IEnumerable<string> ToPayloads(long sequence)
    {
        if (!IsValid || sequence < 0) yield break;
        int chunks = (Nodes.Count + NodesPerChunk - 1) / NodesPerChunk;
        for (int chunk = 0; chunk < chunks; chunk++)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            for (int i = chunk * NodesPerChunk; i < Math.Min(Nodes.Count, (chunk + 1) * NodesPerChunk); i++)
            {
                var n = Nodes[i];
                writer.Write(n.Id);
                writer.Write(n.X); writer.Write(n.Y); writer.Write(n.Z);
                writer.Write(n.Qx); writer.Write(n.Qy); writer.Write(n.Qz); writer.Write(n.Qw);
                writer.Write(n.Sx); writer.Write(n.Sy); writer.Write(n.Sz);
            }
            yield return string.Join("|", sequence.ToString(CultureInfo.InvariantCulture),
                Scene.ToString(CultureInfo.InvariantCulture), VehicleId.ToString(CultureInfo.InvariantCulture),
                AppearanceId.ToString(CultureInfo.InvariantCulture), chunk.ToString(CultureInfo.InvariantCulture),
                chunks.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(memory.ToArray()));
        }
    }
}

// At most one bounded frame is assembled. Missing chunks never publish a
// partial skeleton; the next complete frame supersedes packet loss.
public sealed class VisualPoseAssembler
{
    private long assembling = -1;
    private long published = -1;
    private int scene;
    private uint vehicle;
    private uint appearance;
    private VisualNodePose[][] chunks;

    public void Reset() { assembling = published = -1; chunks = null; }

    public bool Accept(string[] parts, out VisualPoseFrame frame)
    {
        frame = null;
        if (parts.Length != 10 ||
            !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long sequence) || sequence < 0 ||
            sequence <= published || sequence < assembling ||
            !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out int s) || s < 0 || s > 10000 ||
            !uint.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out uint v) || v == 0 ||
            !uint.TryParse(parts[6], NumberStyles.None, CultureInfo.InvariantCulture, out uint a) || a == 0 ||
            !int.TryParse(parts[7], NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            !int.TryParse(parts[8], NumberStyles.None, CultureInfo.InvariantCulture, out int count) ||
            count < 1 || count > VisualPoseFrame.MaxNodes / VisualPoseFrame.NodesPerChunk || index < 0 || index >= count ||
            parts[9].Length > 940) return false;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(parts[9]); }
        catch (FormatException) { return false; }
        if (bytes.Length == 0 || bytes.Length % 44 != 0 || bytes.Length > 44 * VisualPoseFrame.NodesPerChunk ||
            index < count - 1 && bytes.Length != 44 * VisualPoseFrame.NodesPerChunk) return false;
        using var memory = new MemoryStream(bytes);
        using var reader = new BinaryReader(memory);
        var nodes = new VisualNodePose[bytes.Length / 44];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new VisualNodePose(reader.ReadUInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (!nodes[i].IsValid) return false;
        }
        if (sequence > assembling)
        {
            assembling = sequence; scene = s; vehicle = v; appearance = a;
            chunks = new VisualNodePose[count][];
        }
        if (chunks == null || chunks.Length != count || scene != s || vehicle != v || appearance != a || chunks[index] != null)
            return false;
        chunks[index] = nodes;
        if (chunks.Any(c => c == null)) return false;
        var combined = new List<VisualNodePose>();
        foreach (var chunk in chunks) combined.AddRange(chunk);
        var complete = new VisualPoseFrame(scene, vehicle, appearance, combined);
        if (!complete.IsValid) return false;
        published = sequence;
        frame = complete;
        return true;
    }
}
