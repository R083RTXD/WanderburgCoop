namespace WanderburgCoopTest;

// Process-independent identity; string.GetHashCode is randomized across peers.
// IDs select locally loaded assets only. The network never supplies asset paths.
public static class VehicleIdentity
{
    public static uint FromAppearance(IEnumerable<string> visibleParts)
    {
        var parts = visibleParts.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        return parts.Length == 0 ? 0 : FromModuleId(string.Join("\n", parts));
    }

    public static uint FromModuleId(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return 0;
        uint value = 2166136261;
        foreach (char character in moduleId)
        {
            value = unchecked((value ^ (byte)character) * 16777619);
            value = unchecked((value ^ (byte)(character >> 8)) * 16777619);
        }
        return value == 0 ? 1u : value;
    }
}
