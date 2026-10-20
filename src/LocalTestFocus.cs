using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderburgCoopTest;

internal static class LocalTestFocus
{
    // Set on Unity's main thread. Only focus-specific GM behavior is bypassed;
    // Escape, upgrade screens, game over and deliberate pauses remain intact.
    public static bool Active { get; set; }
    public static bool FreeMouse { get; set; }
    private static bool cursorOverridden;
    private static CursorLockMode originalCursorMode;
    private static bool originalCursorVisible;
    private static ManualLogSource log;
    private static readonly FocusCloseGate closeGate = new();

    public static void Install(Harmony harmony, ManualLogSource logger)
    {
        log = logger;
        foreach (string name in new[] { "OnApplicationFocus", "OnApplicationPause", "HandleFocusLoss",
            "PauseActiveGameplayForFocusLoss", "HoldGameplayForFocusLoss", "OpenFocusLossMainMenu", "RestoreFocusStateIfNeeded" })
        {
            var method = AccessTools.Method(typeof(GM), name) ?? throw new MissingMethodException(typeof(GM).FullName, name);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(LocalTestFocus), nameof(AllowFocusHandling)));
        }
        foreach (string name in new[] { "AbortGameplayEntryForFocusLoss", "ShouldOpenEscapeMenuOnFocusLoss" })
        {
            var method = AccessTools.Method(typeof(GM), name) ?? throw new MissingMethodException(typeof(GM).FullName, name);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(LocalTestFocus), nameof(AllowFocusDecision)));
        }
        var closeApplication = AccessTools.Method(typeof(GM), nameof(GM.CloseApplication)) ??
            throw new MissingMethodException(typeof(GM).FullName, nameof(GM.CloseApplication));
        harmony.Patch(closeApplication,
            prefix: new HarmonyMethod(typeof(LocalTestFocus), nameof(AllowApplicationClose)));
        // DDOL releases a temporary boot background mode after scene startup.
        // Re-assert this for the test; setting it only once in Awake was not enough.
        foreach (string name in new[] { "ReleaseBootBackgroundExecution", "OnApplicationFocus", "OnApplicationPause" })
        {
            var method = AccessTools.Method(typeof(DDOL), name) ?? throw new MissingMethodException(typeof(DDOL).FullName, name);
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(LocalTestFocus), nameof(KeepBackgroundExecution)));
        }
    }

    private static bool AllowFocusHandling() => !Active;
    private static bool AllowFocusDecision(ref bool __result)
    {
        if (!Active) return true;
        __result = false;
        return false;
    }
    private static void KeepBackgroundExecution() => Application.runInBackground = true;

    private static bool AllowApplicationClose()
    {
        bool allow = closeGate.TryConsumeClosePermission(Application.isFocused,
            HasCloseConfirmationThisFrame());
        log?.LogInfo($"PID {Environment.ProcessId}: GM.CloseApplication requested; allowed={allow}; focused={Application.isFocused}; backgroundProtection={Active}");
        return allow;
    }

    public static void SuppressCloseAfterPeerLoss()
    {
        closeGate.OnPeerLost();
        log?.LogInfo("Peer disconnected: close input disarmed until a clean frame and a new local press.");
    }

    public static bool HasCloseConfirmationPressedThisFrame() =>
        Mouse.current?.leftButton.wasPressedThisFrame == true ||
        Keyboard.current?.enterKey.wasPressedThisFrame == true ||
        Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
        Gamepad.current?.buttonSouth.wasPressedThisFrame == true;

    public static bool HasCloseConfirmationHeld() =>
        Mouse.current?.leftButton.isPressed == true ||
        Keyboard.current?.enterKey.isPressed == true ||
        Keyboard.current?.spaceKey.isPressed == true ||
        Gamepad.current?.buttonSouth.isPressed == true;

    public static bool HasCloseConfirmationThisFrame() =>
        Mouse.current?.leftButton.wasPressedThisFrame == true ||
        Mouse.current?.leftButton.wasReleasedThisFrame == true ||
        Keyboard.current?.enterKey.wasPressedThisFrame == true ||
        Keyboard.current?.enterKey.wasReleasedThisFrame == true ||
        Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
        Keyboard.current?.spaceKey.wasReleasedThisFrame == true ||
        Gamepad.current?.buttonSouth.wasPressedThisFrame == true ||
        Gamepad.current?.buttonSouth.wasReleasedThisFrame == true;

    public static void ObserveCloseInput(bool focused, bool confirmationChangedThisFrame,
        bool confirmationPressedThisFrame, bool confirmationHeld) =>
        closeGate.Observe(focused, confirmationChangedThisFrame, confirmationPressedThisFrame,
            confirmationHeld);

    public static void Apply()
    {
        Application.runInBackground = true;
        // Keep F8 usable even after a failed join/disconnect. Otherwise the game
        // could re-capture the pointer before the player can click Host/Join.
        if (FreeMouse)
        {
            // Cursor APIs are window/focus sensitive on Windows. Once focus is
            // elsewhere, leave the already released cursor alone until Unity
            // tells us this window has focus again.
            if (!Application.isFocused) return;
            if (!cursorOverridden)
            {
                originalCursorMode = Cursor.lockState;
                originalCursorVisible = Cursor.visible;
                cursorOverridden = true;
            }
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }
        else RestoreCursor();
    }

    public static void OnFocusChanged(bool focused)
    {
        closeGate.OnFocusChanged(focused);
        if (!focused || !FreeMouse) return;

        // F8 is a temporary window-switching mode. Re-entering the game ends
        // it before LateUpdate can fight the game's own focus/cursor handling.
        FreeMouse = false;
        RestoreCursor();
    }

    public static void RestoreCursor()
    {
        if (!cursorOverridden) return;
        // Do not capture the mouse from another focused game window.
        if (Application.isFocused)
        {
            Cursor.lockState = originalCursorMode;
            Cursor.visible = originalCursorVisible;
        }
        cursorOverridden = false;
    }
}
