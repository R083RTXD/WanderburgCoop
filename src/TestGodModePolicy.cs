namespace WanderburgCoopTest;

public static class TestGodModePolicy
{
    public static bool Protect(bool enabled, bool testSaveGuard, bool localPlayer) => enabled && testSaveGuard && localPlayer;
}
