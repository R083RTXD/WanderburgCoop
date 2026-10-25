using System.Numerics;

namespace WanderburgCoopTest;

// The remote presentation root uses snapshot position/yaw. Preserve the full
// sampled visual VP origin relative to that frame before applying local bones
// and detached vehicle-relative roots. Pure math for regression coverage.
public static class VisualRootPose
{
    // Normalize only near-unit samples from Unity; keep malformed data invalid.
    public static Quaternion NormalizeSample(Quaternion rotation) =>
        rotation.LengthSquared() is >= 0.999f and <= 1.001f
            ? Quaternion.Normalize(rotation) : rotation;

    public static VisualNodePose Capture(PlayerSnapshot snapshot, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, snapshot.Yaw * MathF.PI / 180);
        var inverseYaw = Quaternion.Inverse(yaw);
        var p = Vector3.Transform(position - new Vector3(snapshot.X, snapshot.Y, snapshot.Z), inverseYaw);
        var q = Quaternion.Normalize(inverseYaw * rotation);
        return new VisualNodePose(VehicleIdentity.VisualRootId, p.X, p.Y, p.Z,
            q.X, q.Y, q.Z, q.W, scale.X, scale.Y, scale.Z);
    }
}
