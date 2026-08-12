namespace Prismatoid;

/// <summary>
/// Describes the severity of a log message and, when used as a threshold, the minimum
/// severity an application wishes to receive.
/// </summary>
/// <remarks>
/// The levels are ordered from least to most severe. A message is delivered only when its
/// level is greater than or equal to the current threshold, so a threshold of
/// <see cref="Warn"/> delivers warnings and errors while discarding trace, debug, and
/// informational messages. <see cref="None"/> is not a message severity: it suppresses
/// all messages when set as the threshold.
/// </remarks>
public enum PrismLogLevel
{
    /// <summary>The most verbose level, used for fine-grained tracing of internal operations.</summary>
    Trace,

    /// <summary>Diagnostic information useful during development.</summary>
    Debug,

    /// <summary>Informational messages describing normal operation.</summary>
    Info,

    /// <summary>Conditions that are not errors but may indicate a problem.</summary>
    Warn,

    /// <summary>Error conditions.</summary>
    Error,

    /// <summary>Not a message severity. When set as the threshold, suppresses all messages.</summary>
    None,
}
