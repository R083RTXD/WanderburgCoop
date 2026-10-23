namespace WanderburgCoopTest;

// Visible mesh lower edge, not an arbitrary IK-height allowance. Nonfinite
// bounds cannot poison a whole network frame; absent geometry stays explicit.
public struct SoleHeight
{
    private bool hasValue;
    private float minimum;
    public void Include(float height)
    {
        if (!float.IsFinite(height)) return;
        if (!hasValue || height < minimum) minimum = height;
        hasValue = true;
    }
    public float OrFallback(float targetHeight) => hasValue ? minimum : targetHeight;
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
