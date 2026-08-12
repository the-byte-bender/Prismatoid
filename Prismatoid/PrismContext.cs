using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Prismatoid.NativeInterop;

namespace Prismatoid;

/// <summary>
/// Represents a Prism context through which backends are acquired and managed.
/// </summary>
public sealed unsafe class PrismContext : IDisposable
{
    private readonly NativeInterop.PrismContext* _handle;
    private readonly object _lock = new();
    private readonly SynchronizationContext? _syncContext;
    private GCHandle _availabilityStateHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrismContext"/> class.
    /// </summary>
    /// <param name="availabilityChanged">
    /// A function invoked when a backend's runtime availability changes, or
    /// <see langword="null"/>. When this is <see langword="null"/>, the context performs
    /// no background availability polling and creates no poll thread. When non-null, the
    /// context runs an internal thread that samples backend availability and invokes this
    /// callback on each confirmed transition.
    /// </param>
    /// <param name="pollIntervalMs">
    /// The base interval, in milliseconds, between availability scans. A value of
    /// <c>0</c> selects the default of 1000 milliseconds.
    /// </param>
    /// <param name="debounceSamples">
    /// The number of consecutive agreeing samples required before a change in a backend's
    /// availability is confirmed and reported. A value of <c>0</c> selects the default of
    /// 2. A value of <c>1</c> confirms every observed change immediately, without
    /// debouncing.
    /// </param>
    /// <param name="backoffMaxMs">
    /// The upper bound, in milliseconds, for adaptive backoff of the sampling interval.
    /// While availability is unchanging, the interval doubles from
    /// <paramref name="pollIntervalMs"/> toward this bound, and returns to the base
    /// interval as soon as any change is observed. A value of <c>0</c>, or any value not
    /// greater than the base interval, disables backoff and holds the interval constant.
    /// </param>
    /// <param name="autoPowerManage">
    /// When <see langword="true"/>, and when the library was built with power-management
    /// support, the poll thread is paused automatically when the operating system suspends
    /// and resumed when it wakes. When <see langword="false"/>, or on builds and platforms
    /// without power-management support, this field has no effect and the application MAY
    /// drive pausing itself. Use <see cref="IsAutoPowerManagementSupported"/> to determine
    /// whether this option is honored.
    /// </param>
    public PrismContext(
        AvailabilityChangedCallback? availabilityChanged = null,
        uint pollIntervalMs = 0,
        uint debounceSamples = 0,
        uint backoffMaxMs = 0,
        bool autoPowerManage = true
    )
    {
        var config = Methods.prism_config_init();

        if (availabilityChanged is not null)
        {
            _syncContext = SynchronizationContext.Current;
            var state = new AvailabilityCallbackState(availabilityChanged, _syncContext);
            _availabilityStateHandle = GCHandle.Alloc(state);
            config.availability_callback = &OnAvailabilityChanged;
            config.availability_userdata = (void*)GCHandle.ToIntPtr(_availabilityStateHandle);
            config.availability_poll_interval_ms = pollIntervalMs;
            config.availability_debounce_samples = debounceSamples;
            config.availability_backoff_max_ms = backoffMaxMs;
            config.availability_auto_power_manage = (byte)(autoPowerManage ? 1 : 0);
        }

        _handle = Methods.prism_init(&config);

        if (_handle is null)
        {
            if (_availabilityStateHandle.IsAllocated)
                _availabilityStateHandle.Free();
            throw new PrismException(PrismError.PRISM_ERROR_INTERNAL);
        }
    }

    /// <summary>
    /// Gets a list of all backends registered with Prism, in descending priority order.
    /// </summary>
    /// <remarks>
    /// The registry's contents are fixed for the lifetime of the context. This is
    /// <em>not</em> a statement about runtime availability; the availability callback (see
    /// <see cref="PrismContext"/>) reports changes, and runtime availability can be
    /// queried on a backend instance via <see cref="IPrismBackend.Features"/>.
    /// </remarks>
    public IReadOnlyList<PrismBackendInfo> AvailableBackends
    {
        get
        {
            lock (_lock)
            {
                if (_handle is null)
                    return [];
                var count = Methods.prism_registry_count(_handle);
                var backends = new List<PrismBackendInfo>((int)count);
                for (nuint i = 0; i < count; i++)
                {
                    var id = Methods.prism_registry_id_at(_handle, i);
                    var namePtr = Methods.prism_registry_name(_handle, id);
                    var name = Marshal.PtrToStringUTF8((IntPtr)namePtr) ?? "Unknown";
                    var priority = Methods.prism_registry_priority(_handle, id);

                    backends.Add(new PrismBackendInfo(new PrismBackendId(id), name, priority));
                }
                return backends;
            }
        }
    }

    /// <summary>
    /// Looks up a backend by its human-readable name (exact, case-sensitive).
    /// </summary>
    /// <param name="name">The backend name, e.g. "SAPI", "NVDA", "OneCore".</param>
    /// <returns>The backend's ID, or <see cref="PrismBackendId.Invalid"/> if not found.</returns>
    public PrismBackendId GetBackendId(string name)
    {
        using var utf8 = new Utf8String(name);
        lock (_lock)
        {
            return new PrismBackendId(Methods.prism_registry_id(_handle, utf8.Pointer));
        }
    }

    /// <summary>
    /// Gets the human-readable name of a backend.
    /// </summary>
    public string GetBackendName(PrismBackendId backendId)
    {
        lock (_lock)
        {
            var namePtr = Methods.prism_registry_name(_handle, backendId.Value);
            return Marshal.PtrToStringUTF8((IntPtr)namePtr) ?? "Unknown";
        }
    }

    /// <summary>
    /// Suspends availability polling for this context.
    /// </summary>
    /// <remarks>
    /// While paused, the poll thread parks and consumes no processor time. A scan already
    /// in progress when this method is called is allowed to complete; the pause takes
    /// effect at the following scan. This method does nothing if the context was not
    /// configured with an availability callback or if polling is already paused. Pausing is
    /// intended for applications that wish to suppress polling while they are backgrounded
    /// or otherwise idle, particularly on platforms where automatic power management is
    /// unavailable.
    /// </remarks>
    public void PauseAvailabilityPolling()
    {
        lock (_lock)
        {
            Methods.prism_availability_poll_pause(_handle);
        }
    }

    /// <summary>
    /// Resumes availability polling for a context previously paused with
    /// <see cref="PauseAvailabilityPolling"/>.
    /// </summary>
    /// <remarks>
    /// On resume, the poll thread performs an immediate re-synchronizing scan and invokes
    /// the availability callback for every backend whose availability differs from the
    /// state last reported, without debouncing. The sampling interval is reset to its base
    /// value. This method does nothing if the context was not configured with an
    /// availability callback or if polling is not paused.
    /// </remarks>
    public void ResumeAvailabilityPolling()
    {
        lock (_lock)
        {
            Methods.prism_availability_poll_resume(_handle);
        }
    }

    /// <summary>
    /// Reports whether this build can pause and resume availability polling automatically
    /// in response to operating-system power transitions.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the <c>autoPowerManage</c> configuration option is honored
    /// on this build, and <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// This reflects a compile-time and platform property and can be called at any time,
    /// including before any context is created. When it returns <see langword="false"/>, an
    /// application that wishes to avoid polling while the machine is unattended should
    /// drive <see cref="PauseAvailabilityPolling"/> and
    /// <see cref="ResumeAvailabilityPolling"/> itself, from whatever lifecycle
    /// notifications it receives from the operating system.
    /// </remarks>
    public static bool IsAutoPowerManagementSupported() =>
        Methods.prism_availability_auto_power_supported();

    /// <summary>
    /// Acquires the highest-priority backend that successfully initializes, reusing a cached instance if available.
    /// </summary>
    /// <returns>An initialized backend instance.</returns>
    /// <remarks>
    /// This method uses the internal Prism registry cache. Multiple calls to this method (or <see cref="AcquireBackend"/>)
    /// for the same backend ID will return wrappers pointing to the same native resource.
    /// </remarks>
    /// <exception cref="PrismException">Thrown if no backends are available.</exception>
    public IPrismBackend AcquireBestBackend()
    {
        lock (_lock)
        {
            var backendPtr = Methods.prism_registry_acquire_best(_handle);
            if (backendPtr is null)
            {
                throw new PrismException(PrismError.PRISM_ERROR_BACKEND_NOT_AVAILABLE);
            }
            return new PrismBackend(backendPtr);
        }
    }

    /// <summary>
    /// Creates a fresh instance of the highest-priority backend, bypassing the cache.
    /// </summary>
    /// <returns>A new initialized backend instance.</returns>
    /// <remarks>
    /// Unlike <see cref="AcquireBestBackend"/>, this creates a unique native instance.
    /// </remarks>
    public IPrismBackend CreateBestBackend()
    {
        lock (_lock)
        {
            var backendPtr = Methods.prism_registry_create_best(_handle);
            if (backendPtr is null)
            {
                throw new PrismException(PrismError.PRISM_ERROR_BACKEND_NOT_AVAILABLE);
            }
            return new PrismBackend(backendPtr);
        }
    }

    /// <summary>
    /// Acquires a backend instance, reusing a cached instance if available or creating a new one otherwise.
    /// </summary>
    /// <param name="info">The info of the backend to acquire.</param>
    /// <returns>A backend instance (either existing or newly created).</returns>
    public IPrismBackend AcquireBackend(PrismBackendInfo info) => AcquireBackend(info.Id);

    /// <summary>
    /// Acquires a backend instance, reusing a cached instance if available or creating a new one otherwise.
    /// </summary>
    /// <param name="backendId">The ID of the backend to acquire.</param>
    /// <returns>A backend instance (either existing or newly created).</returns>
    /// <remarks>
    /// This method uses the internal Prism registry cache. Multiple calls for the same ID will return wrappers
    /// sharing the same native instance.
    /// </remarks>
    /// <exception cref="PrismException">Thrown if the specified backend is not available.</exception>
    public IPrismBackend AcquireBackend(PrismBackendId backendId)
    {
        lock (_lock)
        {
            var backendPtr = Methods.prism_registry_acquire(_handle, backendId.Value);
            if (backendPtr is null)
            {
                throw new PrismException(PrismError.PRISM_ERROR_BACKEND_NOT_AVAILABLE);
            }
            return new PrismBackend(backendPtr);
        }
    }

    /// <summary>
    /// Creates a fresh instance of a backend, bypassing the cache.
    /// </summary>
    /// <param name="backendId">The ID of the backend to create.</param>
    /// <returns>A new initialized backend instance.</returns>
    /// <remarks>
    /// Each call creates a unique native instance with its own state.
    /// Use this if you need independent voice/rate/pitch state from the rest of the application.
    /// </remarks>
    public IPrismBackend CreateBackend(PrismBackendId backendId)
    {
        lock (_lock)
        {
            var backendPtr = Methods.prism_registry_create(_handle, backendId.Value);
            if (backendPtr is null)
            {
                throw new PrismException(PrismError.PRISM_ERROR_BACKEND_NOT_AVAILABLE);
            }
            return new PrismBackend(backendPtr);
        }
    }

    /// <summary>
    /// Disposes the context and releases all associated resources.
    /// </summary>
    /// <remarks>
    /// This does not automatically dispose backend instances that were obtained from the context. If any backends obtained through this context stay alive when the context is disposed, those backends remain valid and may continue to be used.
    /// </remarks>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_handle is not null)
            {
                Methods.prism_shutdown(_handle);
            }
            if (_availabilityStateHandle.IsAllocated)
            {
                _availabilityStateHandle.Free();
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAvailabilityChanged(
        void* userdata,
        ulong backendId,
        sbyte* name,
        byte available
    )
    {
        try
        {
            var state = (AvailabilityCallbackState)GCHandle.FromIntPtr((IntPtr)userdata).Target!;
            state.Invoke(
                new PrismBackendId(backendId),
                Marshal.PtrToStringUTF8((IntPtr)name) ?? "",
                available != 0
            );
        }
        catch (Exception ex)
        {
            // Never let exceptions escape to the native poll thread: the poll thread cannot
            // perform further scans until the callback returns. Log and swallow.
            Console.Error.WriteLine($"Exception in availability callback: {ex}");
        }
    }

    private sealed class AvailabilityCallbackState(
        AvailabilityChangedCallback callback,
        SynchronizationContext? syncContext
    )
    {
        public void Invoke(PrismBackendId backendId, string name, bool available)
        {
            if (syncContext is not null)
            {
                syncContext.Post(_ => callback(backendId, name, available), null);
            }
            else
            {
                Task.Run(() => callback(backendId, name, available));
            }
        }
    }
}
