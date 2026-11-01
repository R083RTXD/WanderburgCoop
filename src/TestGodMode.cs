using BepInEx.Logging;
using HarmonyLib;

namespace WanderburgCoopTest;

// Runtime-only test switch. No health writes, unlocks or persistence changes.
internal static class TestGodMode
{
    public static bool Enabled { get; private set; }
    public static bool Ready { get; private set; }
    public static int BlockedHits { get; private set; }
    private static ManualLogSource log;

    public static void Install(Harmony harmony, ManualLogSource logger)
    {
        log = logger;
        var damage = AccessTools.Method(typeof(VM), nameof(VM.TakeDamage), new[] { typeof(float), typeof(int), typeof(bool) });
        var death = AccessTools.Method(typeof(VM), nameof(VM.Death));
        if (damage == null || death == null) throw new MissingMethodException("Local VM damage/death hooks missing.");
        harmony.Patch(damage, prefix: new HarmonyMethod(typeof(TestGodMode), nameof(BeforeDamage)));
        harmony.Patch(death, prefix: new HarmonyMethod(typeof(TestGodMode), nameof(BeforeDeath)));
        Ready = true;
        log.LogInfo("Local test godmode available, OFF by default. F9 or mod panel toggles protection; no save data changed.");
    }
    public static void SetEnabled(bool enabled)
    {
        if (!Ready || Enabled == enabled) return;
        Enabled = enabled; BlockedHits = 0;
        log.LogInfo($"Local test godmode {(enabled ? "ON" : "OFF")}. Existing dead runs are not revived.");
    }
    private static bool Protect(VM instance) => TestGodModePolicy.Protect(Enabled, TestSaveGuard.Ready,
        instance && GM.gm && GM.gm.vm && instance == GM.gm.vm);
    private static bool BeforeDamage(VM __instance, float __0)
    {
        if (!Protect(__instance) || !(__0 > 0)) return true;
        BlockedHits++;
        if (BlockedHits <= 3) log.LogInfo($"Godmode blocked local damage hit {BlockedHits}, amount={__0}.");
        return false;
    }
    private static bool BeforeDeath(VM __instance) => !Protect(__instance);
}
