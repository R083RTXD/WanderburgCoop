namespace WanderburgCoopTest;

public sealed class DustRateGate
{
    private double next;
    private int remaining;
    public bool TryTake(double now)
    {
        if (!double.IsFinite(now) || now < 0) return false;
        // A sampled frame can contain all sixteen valid foot contacts.
        if (now >= next) { remaining = 16; next = now + 0.075; }
        if (remaining == 0) return false;
        remaining--;
        return true;
    }
    public void Reset() { next = 0; remaining = 0; }
}

// Visible mesh lower edge, not an arbitrary IK-height allowance. Nonfinite
// bounds cannot poison a whole network frame; absent geometry stays explicit.
public struct SoleContact
{
    private bool hasValue;
    private float minimum;
    private System.Numerics.Vector3 point;
    public void Include(float centerX, float height, float centerZ)
    {
        if (!float.IsFinite(height) || !float.IsFinite(centerX) || !float.IsFinite(centerZ)) return;
        if (!hasValue || height < minimum)
        {
            minimum = height;
            point = new(centerX, height, centerZ);
        }
        hasValue = true;
    }
    public System.Numerics.Vector3 OrFallback(System.Numerics.Vector3 target) => hasValue ? point : target;
}

public static class GroundMarkTiming
{
    public const float WheelLifetime = 1.2f;
    public const float FootFallbackLifetime = 2.4f;
    public static float SafeFootLifetime(float value) => float.IsFinite(value) && value > 0
        ? Math.Clamp(value, 0.2f, 6) : FootFallbackLifetime;
    public static float FallbackScale(float normalizedAge)
    {
        float t = Math.Clamp(normalizedAge, 0, 1);
        return t < 0.75f ? 0.7f + t * 0.5f : 4 * (1 - t) * 1.075f;
    }
}

// Per-contact cosmetic spacing. No Unity or game authority; resets across
// missing samples, airborne feet, teleports and model/scene changes.
public sealed class GroundMarkGate
{
    private bool sampled;
    private float lastX, lastZ;
    public void Reset() => sampled = false;
    public bool Sample(float x, float z, bool grounded, bool moving, bool foot)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || !grounded || !moving)
        {
            Reset();
            return false;
        }
        if (!sampled)
        {
            sampled = true;
            lastX = x; lastZ = z;
            return foot; // A new moving foot contact, not one stamp per frame.
        }
        float distance2 = (x - lastX) * (x - lastX) + (z - lastZ) * (z - lastZ);
        if (distance2 > 400) { lastX = x; lastZ = z; return false; }
        float spacing = foot ? 2.5f : 0.35f;
        if (distance2 < spacing * spacing) return false;
        lastX = x; lastZ = z;
        return true;
    }
}
