namespace Prismatoid;

/// <summary>
/// The type of a function invoked to deliver a single log message.
/// </summary>
/// <param name="level">The severity of the message.</param>
/// <param name="source">The name of the component that produced the message.</param>
/// <param name="message">The message text.</param>
/// <remarks>
/// Prismatoid marshals the invocation to the <see cref="SynchronizationContext"/> current when the handler was installed (or to a
/// thread-pool thread when none is available), so this delegate itself does not run on the
/// logging thread. Implementations that touch shared state must provide their own synchronization
/// </remarks>
public delegate void PrismLogHandler(PrismLogLevel level, string source, string message);
