namespace WanderburgCoopTest;

// GUI coordinates: Y grows down. Projection input is Unity viewport space.
public static class PlayerEdgePosition
{
    public static bool Eligible(bool connected, bool fresh, PlayerSnapshot local, PlayerSnapshot remote) =>
        connected && fresh && local.InRun && remote.InRun && local.Scene == remote.Scene;

    public static bool TryPlace(float vx, float vy, float depth, float width, float height,
        out float x, out float y, out float angle)
    {
        x = y = angle = 0;
        if (!float.IsFinite(vx) || !float.IsFinite(vy) || !float.IsFinite(depth) ||
            !float.IsFinite(width) || !float.IsFinite(height) || width < 100 || height < 100) return false;
        if (depth > 0 && vx >= 0 && vx <= 1 && vy >= 0 && vy <= 1) return false;
        double dx = ((double)vx - 0.5) * width, dy = (0.5 - (double)vy) * height;
        if (depth <= 0) { dx = -dx; dy = -dy; }
        if (Math.Abs(dx) + Math.Abs(dy) < 0.0001) dy = 1;
        double margin = Math.Min(48, Math.Min(width, height) / 4);
        double tx = Math.Abs(dx) > 0.000001 ? (width / 2 - margin) / Math.Abs(dx) : double.PositiveInfinity;
        double ty = Math.Abs(dy) > 0.000001 ? (height / 2 - margin) / Math.Abs(dy) : double.PositiveInfinity;
        double scale = Math.Min(tx, ty);
        x = (float)(width / 2 + dx * scale);
        y = (float)(height / 2 + dy * scale);
        angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
        return true;
    }
}
