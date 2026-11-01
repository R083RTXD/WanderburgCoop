namespace WanderburgCoopTest;

// The typed native particle API caused a startup regression in 0.6.22.
// Keep exact particle streaming disabled until a safe supported access path
// is established. Do not run native allocation or API probes at startup.
internal static class NativeParticleBuffer
{
    public static bool Ready => false;
}
