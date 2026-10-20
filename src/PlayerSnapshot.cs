using System.Globalization;

namespace WanderburgCoopTest;

// Contains no Unity references, so the exact wire format can be checked outside the game.
public readonly record struct PlayerSnapshot(bool InRun, int Scene, float X, float Y, float Z, float Yaw)
{
    public bool IsValid => Scene >= -1 && Scene <= 10000 &&
        float.IsFinite(X) && Math.Abs(X) <= 1000000 &&
        float.IsFinite(Y) && Math.Abs(Y) <= 100000 &&
        float.IsFinite(Z) && Math.Abs(Z) <= 1000000 &&
        float.IsFinite(Yaw) && Yaw >= 0 && Yaw < 360;

    public string ToPayload(long sequence) => string.Join("|",
        sequence.ToString(CultureInfo.InvariantCulture), InRun ? "1" : "0",
        Scene.ToString(CultureInfo.InvariantCulture),
        X.ToString("R", CultureInfo.InvariantCulture), Y.ToString("R", CultureInfo.InvariantCulture),
        Z.ToString("R", CultureInfo.InvariantCulture), Yaw.ToString("R", CultureInfo.InvariantCulture));

    public static bool TryRead(string[] parts, out long sequence, out PlayerSnapshot snapshot)
    {
        sequence = -1;
        snapshot = default;
        if (parts.Length != 10 ||
            !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out sequence) ||
            sequence < 0 || parts[4] is not ("0" or "1") ||
            !int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int scene) ||
            !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
            !float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float z) ||
            !float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float yaw)) return false;
        snapshot = new PlayerSnapshot(parts[4] == "1", scene, x, y, z, yaw);
        return snapshot.IsValid;
    }
}
