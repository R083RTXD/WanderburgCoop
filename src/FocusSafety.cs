namespace WanderburgCoopTest;

// Kept free of Unity references so the close-policy regression can be tested
// outside the game. Delayed input from a departing peer must never become
// permission to close the surviving process.
public sealed class FocusCloseGate
{
    private bool readyForFocusedInput;
    private bool peerLossDisarmed;
    private bool peerLossSawCleanFrame;

    public void OnFocusChanged(bool focused)
    {
        readyForFocusedInput = false;
    }

    public void OnPeerLost()
    {
        peerLossDisarmed = true;
        peerLossSawCleanFrame = false;
    }

    public void Observe(bool applicationIsFocused, bool confirmationChangedThisFrame,
        bool confirmationPressedThisFrame, bool confirmationHeld)
    {
        if (!applicationIsFocused)
        {
            readyForFocusedInput = false;
            return;
        }

        // A click that closes one window can be reported while Windows moves
        // focus to the surviving peer. Require one clean focused frame first.
        if (!readyForFocusedInput)
        {
            if (!confirmationChangedThisFrame && !confirmationHeld) readyForFocusedInput = true;
            return;
        }

        if (!peerLossDisarmed) return;
        if (!confirmationChangedThisFrame && !confirmationHeld)
        {
            peerLossSawCleanFrame = true;
            return;
        }

        // Only a new press after a clean frame can re-arm closing. A delayed
        // release inherited from the departing window can never satisfy this.
        if (peerLossSawCleanFrame && confirmationPressedThisFrame) peerLossDisarmed = false;
    }

    public bool TryConsumeClosePermission(bool applicationIsFocused,
        bool confirmationChangedThisFrame) =>
        !peerLossDisarmed && applicationIsFocused && readyForFocusedInput &&
        confirmationChangedThisFrame;
}
