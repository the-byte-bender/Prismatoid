namespace Prismatoid
{
    [Flags]
    public enum PrismBackendFeature : ulong
    {
        IsSupportedAtRuntime = 1UL << 0,
        SupportsSpeak = 1UL << 2,
        SupportsSpeakToMemory = 1UL << 3,
        SupportsBraille = 1UL << 4,
        SupportsOutput = 1UL << 5,
        SupportsIsSpeaking = 1UL << 6,
        SupportsStop = 1UL << 7,
        SupportsPause = 1UL << 8,
        SupportsResume = 1UL << 9,
        SupportsSetVolume = 1UL << 10,
        SupportsGetVolume = 1UL << 11,
        SupportsSetRate = 1UL << 12,
        SupportsGetRate = 1UL << 13,
        SupportsSetPitch = 1UL << 14,
        SupportsGetPitch = 1UL << 15,
        SupportsRefreshVoices = 1UL << 16,
        SupportsCountVoices = 1UL << 17,
        SupportsGetVoiceName = 1UL << 18,
        SupportsGetVoiceLanguage = 1UL << 19,
        SupportsGetVoice = 1UL << 20,
        SupportsSetVoice = 1UL << 21,
        SupportsGetChannels = 1UL << 22,
        SupportsGetSampleRate = 1UL << 23,
        SupportsGetBitDepth = 1UL << 24,
        PerformsSilenceTrimmingOnSpeak = 1UL << 25,
        PerformsSilenceTrimmingOnSpeakToMemory = 1UL << 26,
        SupportsSpeakSsml = 1UL << 27,
        SupportsSpeakToMemorySsml = 1UL << 28,
    }
}

namespace Prismatoid.NativeInterop
{
    public partial struct PrismBackend { }
}
