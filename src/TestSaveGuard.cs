using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace WanderburgCoopTest;

// This development build is intentionally read-only for SaveLoad persistence.
// All peers may read the same existing profile, but run results are not saved.
// Install before SaveLoad.Awake, which runs after the BepInEx chainloader.
internal static class TestSaveGuard
{
    public static bool Ready { get; private set; }

    public static void Install(Harmony harmony, ManualLogSource log)
    {
        string[] noWrite = {
            "Save", "SaveThis", "RequestSave", "SaveRunCheckpoint", "FlushRequestedSave",
            "EnsureLocalBackupSnapshot", "ArchiveFileCopy", "ArchiveAndDeleteFile",
            "FallbackReplace", "MarkLegacyRootSaveHandled", "ArchiveCurrentSaveAndStartFresh",
            "ResetCompleteSaveFile", "ResetPlayerprefs"
        };
        var targets = noWrite.Select(name => Require(name)).ToArray();
        var writer = Require("TryWriteSave");
        var resolve = Require("ResolveBuildIdentity");
        var load = Require("Load");
        // Resolve every target before installing any patches: version mismatches fail loudly.
        foreach (var method in targets)
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(TestSaveGuard), nameof(SkipWrite)));
        harmony.Patch(writer, prefix: new HarmonyMethod(typeof(TestSaveGuard), nameof(SkipTryWrite)));
        harmony.Patch(resolve, postfix: new HarmonyMethod(typeof(TestSaveGuard), nameof(DisableWrites)));
        harmony.Patch(load, prefix: new HarmonyMethod(typeof(TestSaveGuard), nameof(DisableWrites)),
            postfix: new HarmonyMethod(typeof(TestSaveGuard), nameof(DisableWrites)));
        Ready = true;
        log.LogWarning("TEST SAVE GUARD ACTIVE: SaveLoad writes are blocked. Run progress will NOT be saved while this development mod is installed.");
    }

    private static MethodInfo Require(string name) => AccessTools.Method(typeof(SaveLoad), name) ??
        throw new MissingMethodException(typeof(SaveLoad).FullName, name);
    private static bool SkipWrite() => false;
    private static bool SkipTryWrite(ref bool __result)
    {
        __result = false;
        return false;
    }
    private static void DisableWrites(SaveLoad __instance) => __instance.writesEnabled = false;
}
