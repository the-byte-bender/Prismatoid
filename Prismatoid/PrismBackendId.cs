namespace Prismatoid;

/// <summary>
/// Identifies a backend in a Prism registry.
/// </summary>
/// <remarks>
/// Prism defines constants for all known backend identifiers. These constants are 64-bit unsigned integers computed at compile time using a deterministic hash function. Applications MAY use these constants to request specific backends by ID rather than by name, which avoids the overhead of a string lookup.
/// </remarks>
/// <param name="Value">The raw 64-bit backend identifier.</param>
public readonly record struct PrismBackendId(ulong Value)
{
    /// <summary>Invalid/sentinel value (always 0).</summary>
    public static readonly PrismBackendId Invalid = new(0);

    /// <summary>Microsoft SAPI (Windows).</summary>
    public static readonly PrismBackendId Sapi = new(0x1D6DF72422CEEE66);

    /// <summary>AVSpeechSynthesizer (macOS, iOS, tvOS, WatchOS, VisionOS).</summary>
    public static readonly PrismBackendId AvSpeech = new(0x28E3429577805C24);

    /// <summary>VoiceOver screen reader (macOS, iOS, WatchOS, VisionOS).</summary>
    public static readonly PrismBackendId VoiceOver = new(0xCB4897961A754BCB);

    /// <summary>Speech Dispatcher (Linux/BSD).</summary>
    public static readonly PrismBackendId SpeechDispatcher = new(0xE3D6F895D949EBFE);

    /// <summary>NVDA screen reader (Windows).</summary>
    public static readonly PrismBackendId Nvda = new(0x89CC19C5C4AC1A56);

    /// <summary>JAWS screen reader (Windows).</summary>
    public static readonly PrismBackendId Jaws = new(0xAC3D60E9BD84B53E);

    /// <summary>Windows OneCore speech API (Windows 10+).</summary>
    public static readonly PrismBackendId OneCore = new(0x6797D32F0D994CB4);

    /// <summary>Orca screen reader (Linux/BSD).</summary>
    public static readonly PrismBackendId Orca = new(0x10AA1FC05A17F96C);

    /// <summary>Android TTS engine (Android).</summary>
    public static readonly PrismBackendId AndroidTts = new(0xBC175831BFE4E5CC);

    /// <summary>Android screen readers (Android).</summary>
    public static readonly PrismBackendId AndroidScreenReader = new(0xD199C175AEEC494B);

    /// <summary>Web SpeechSynthesis API (web).</summary>
    public static readonly PrismBackendId WebSpeech = new(0x3572538D44D44A8F);

    /// <summary>UIAutomation backend (Windows only).</summary>
    public static readonly PrismBackendId Uia = new(0x6238F019DB678F8E);

    /// <summary>Zhengdu Screen Reader (Windows).</summary>
    public static readonly PrismBackendId Zdsr = new(0x3D93C56C9E7F2A2E);

    /// <summary>ZoomText (Windows).</summary>
    public static readonly PrismBackendId ZoomText = new(0xAE439D62DC7B1479);

    /// <summary>BoyPCReader (Windows only).</summary>
    public static readonly PrismBackendId BoyPcReader = new(0x285ABA1C16F3300F);

    /// <summary>PCTalker (Windows only).</summary>
    public static readonly PrismBackendId PcTalker = new(0x344B951962E3B835);

    /// <summary>Sense Reader screen reader (Windows).</summary>
    public static readonly PrismBackendId SenseReader = new(0xED4760890B55C2F2);

    /// <summary>SystemAccess screen reader (windows) (only available if explicitly enabled at build time)</summary>
    public static readonly PrismBackendId SystemAccess = new(0x8380F2A37B2C3EB6);

    /// <summary>WindowEyes screen reader (windows) (only available if explicitly enabled at build time)</summary>
    public static readonly PrismBackendId WindowEyes = new(0x9120D89908785C13);

    /// <summary>Spiel (Linux and BSDs only).</summary>
    public static readonly PrismBackendId Spiel = new(0x478B44F14AD3D89C);

    /// <inheritdoc />
    public override string ToString() => $"0x{Value:X16}";
}
