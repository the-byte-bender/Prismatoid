using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Prismatoid.NativeInterop;

namespace Prismatoid;

/// <summary>
/// Provides access to Prism's process-wide logging subsystem.
/// </summary>
/// <remarks>
/// Logging is a process-wide facility rather than a per-context one: there is a single
/// logger shared by the entire library, and the members of this class operate on it
/// directly. They do not require a <see cref="PrismContext"/> and can be called before any
/// context is created or after all contexts have been disposed. By default messages are
/// discarded; install a handler with <see cref="SetHandler"/> to observe them.
/// </remarks>
public static unsafe class PrismLog
{
    private static readonly object _lock = new();
    private static PrismLogHandler? _handler;
    private static SynchronizationContext? _syncContext;
    private static GCHandle _stateHandle;

    /// <summary>
    /// Installs the handler that receives log messages, replacing any previously installed
    /// handler, and returns the handler that was previously installed (or
    /// <see langword="null"/> if none was installed).
    /// </summary>
    /// <param name="handler">
    /// The handler to install. Pass <see langword="null"/> to disable delivery. The
    /// returned handler may be retained and reinstalled later to restore prior behavior.
    /// </param>
    /// <remarks>
    /// Installing a handler makes Prism's diagnostic output visible to the application;
    /// until a handler is installed, messages are discarded. This method is thread-safe and
    /// can be called at any time, including before any context is created and concurrently
    /// with logging activity on other threads. The replacement takes effect for messages
    /// delivered after the call; a message already in flight may still be delivered to the
    /// previous handler, so the previous handler must not be assumed idle immediately.
    /// </remarks>
    public static PrismLogHandler? SetHandler(PrismLogHandler? handler)
    {
        lock (_lock)
        {
            var previous = _handler;
            _handler = handler;
            _syncContext = SynchronizationContext.Current;

            if (_stateHandle.IsAllocated)
                _stateHandle.Free();

            if (handler is not null)
            {
                var state = new LogCallbackState(handler, _syncContext);
                _stateHandle = GCHandle.Alloc(state);

                var native = new PrismLogHandlerNative
                {
                    fn = &OnLogMessage,
                    userdata = (void*)GCHandle.ToIntPtr(_stateHandle),
                };
                Methods.prism_set_log_handler(native);
            }
            else
            {
                var none = new PrismLogHandlerNative { fn = null, userdata = null };
                Methods.prism_set_log_handler(none);
            }

            return previous;
        }
    }

    /// <summary>
    /// Sets the minimum severity of messages that will be delivered, returning the
    /// threshold that was in effect before the call.
    /// </summary>
    /// <remarks>
    /// Messages whose severity is below this level are discarded before they are queued, so
    /// raising the threshold immediately reduces the work performed for suppressed
    /// messages. <see cref="PrismLogLevel.None"/> suppresses all messages. The threshold
    /// and the installed handler are independent.
    /// </remarks>
    public static PrismLogLevel SetLevel(PrismLogLevel level) =>
        Methods.prism_set_log_level(level);

    /// <summary>
    /// Emits a log message at the given severity, routed through the same handler as the
    /// library's own diagnostics.
    /// </summary>
    public static void Log(PrismLogLevel level, string source, string message)
    {
        using var sourceUtf8 = new Utf8String(source);
        using var messageUtf8 = new Utf8String(message);
        Methods.prism_log(level, sourceUtf8.Pointer, messageUtf8.Pointer);
    }

    /// <summary>
    /// Blocks until all queued log messages have been delivered to the installed handler.
    /// </summary>
    public static void Flush() => Methods.prism_log_flush();

    /// <summary>
    /// Shuts down the logging subsystem, releasing the internal logging thread.
    /// </summary>
    /// <remarks>
    /// After shutdown, log messages are no longer delivered. This is typically called
    /// during application teardown, after all contexts have been disposed.
    /// </remarks>
    public static void Shutdown()
    {
        lock (_lock)
        {
            Methods.prism_log_shutdown();
            if (_stateHandle.IsAllocated)
                _stateHandle.Free();
            _handler = null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnLogMessage(
        void* userdata,
        PrismLogLevel level,
        sbyte* source,
        sbyte* message
    )
    {
        var state = (LogCallbackState)GCHandle.FromIntPtr((IntPtr)userdata).Target!;
        state.Invoke(
            level,
            Marshal.PtrToStringUTF8((IntPtr)source) ?? "",
            Marshal.PtrToStringUTF8((IntPtr)message) ?? ""
        );
    }

    private sealed class LogCallbackState(
        PrismLogHandler handler,
        SynchronizationContext? syncContext
    )
    {
        public void Invoke(PrismLogLevel level, string source, string message)
        {
            if (syncContext is not null)
            {
                syncContext.Post(_ => handler(level, source, message), null);
            }
            else
            {
                Task.Run(() => handler(level, source, message));
            }
        }
    }
}
