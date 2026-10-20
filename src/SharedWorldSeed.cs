using System.Threading;
using BepInEx.Logging;
using HarmonyLib;

namespace WanderburgCoopTest;

internal static class SharedWorldSeed
{
    private static int current;
    private static int lastLogged;
    private static ManualLogSource log;

    public static void Install(Harmony harmony, ManualLogSource logger)
    {
        log = logger;
        var awake = AccessTools.Method(typeof(MapGenerator), nameof(MapGenerator.Awake)) ??
            throw new MissingMethodException(typeof(MapGenerator).FullName, nameof(MapGenerator.Awake));
        harmony.Patch(awake, prefix: new HarmonyMethod(typeof(SharedWorldSeed), nameof(ApplyBeforeGeneration)));
    }

    public static void Update(int seed) => Volatile.Write(ref current, seed > 0 ? seed : 0);

    private static void ApplyBeforeGeneration(MapGenerator __instance)
    {
        int seed = Volatile.Read(ref current);
        if (seed <= 0) return;

        __instance.useSeed = true;
        __instance.seed = seed;
        if (Interlocked.Exchange(ref lastLogged, seed) != seed)
            log?.LogInfo($"Applying shared world seed {seed} before MapGenerator.Awake.");
    }
}
