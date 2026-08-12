namespace Prismatoid;

/// <summary>
/// Provides high-level access to a Prism output backend.
/// </summary>
public interface IPrismBackend : IDisposable
{
    /// <summary>
    /// Gets the display name of the backend.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the features supported by this backend.
    /// </summary>
    PrismBackendFeature Features { get; }

    /// <summary>
    /// Gets or sets the speech volume.
    /// </summary>
    /// <exception cref="PrismException">Thrown if not supported.</exception>
    float Volume { get; set; }

    /// <summary>
    /// Gets or sets the speech rate.
    /// </summary>
    /// <exception cref="PrismException">Thrown if not supported.</exception>
    float Rate { get; set; }

    /// <summary>
    /// Gets or sets the voice pitch.
    /// </summary>
    /// <exception cref="PrismException">Thrown if not supported.</exception>
    float Pitch { get; set; }

    /// <summary>
    /// Gets whether the backend is currently speaking.
    /// </summary>
    bool IsSpeaking { get; }

    /// <summary>
    /// Gets an enumerable of available voices for this backend.
    /// </summary>
    IEnumerable<PrismVoice> Voices { get; }

    /// <summary>
    /// Gets the number of audio channels for memory synthesis.
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// Gets the sample rate (Hz) for memory synthesis.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Gets the native bit depth of the audio produced by the backend. Common values are
    /// 8 and 16.
    /// </summary>
    /// <remarks>
    /// This is the backend's native format, not the format delivered to the audio
    /// callback: samples delivered by <see cref="SpeakToMemory"/> are always 32-bit
    /// floating-point values in the range [-1.0, 1.0], regardless of the native bit depth.
    /// This value is informational; applications typically do not need it unless they are
    /// converting the audio to a specific format for storage or transmission.
    /// </remarks>
    int BitDepth { get; }

    /// <summary>
    /// Gets or sets the current active voice.
    /// </summary>
    PrismVoice? CurrentVoice { get; set; }

    /// <summary>
    /// Speaks the specified text.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="interrupt">Whether to stop current speech before speaking.</param>
    void Speak(string text, bool interrupt = true);

    /// <summary>
    /// Synthesizes speech to memory as a stream of audio chunks.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <returns>An enumerable of audio chunks (32-bit float, interleaved samples).</returns>
    /// <remarks>
    /// <para>
    /// This method starts synthesis and yields chunks of audio as they are produced.
    /// The application SHOULD NOT call other methods on this backend while the enumerable is being consumed.
    /// </para>
    /// <para>
    /// <b>Performance Warning:</b> To minimize allocations, the internal buffers may be pooled.
    /// The data in each <see cref="ReadOnlyMemory{T}"/> is only guaranteed to be valid until the next iteration of the enumerable.
    /// If you need to keep the audio data, you MUST copy it to a private buffer.
    /// </para>
    /// </remarks>
    IEnumerable<ReadOnlyMemory<float>> SpeakToMemory(string text);

    /// <summary>
    /// Sends the specified text to a braille display.
    /// </summary>
    /// <param name="text">The text to braille.</param>
    void Braille(string text);

    /// <summary>
    /// Performs both speech and braille output.
    /// </summary>
    /// <param name="text">The text to output.</param>
    /// <param name="interrupt">Whether to stop current speech before speaking.</param>
    void Output(string text, bool interrupt = true);

    /// <summary>
    /// Stops all current output.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses speech output.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes paused speech output.
    /// </summary>
    void Resume();
}
