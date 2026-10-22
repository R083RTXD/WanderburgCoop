using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace WanderburgCoopTest;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class WanderburgCoopPlugin : BasePlugin
{
    public const string PluginGuid = "de.wanderburg.coop";
    public const string PluginName = "Wanderburg Co-op";
    public const string PluginVersion = "0.5.0";

    public override void Load()
    {
        var harmony = new Harmony(PluginGuid);
        try
        {
            TestSaveGuard.Install(harmony, Log);
            LocalTestFocus.Install(harmony, Log);
            SharedWorldSeed.Install(harmony, Log);
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            Log.LogError($"Required test hooks could not be installed; connection/position test disabled. {ex}");
            return;
        }
        AddComponent<ConnectionPanel>();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded. Shared world seed, background network worker, focus hooks and F8 cursor release enabled. SaveLoad persistence disabled for this development build.");
    }
}
