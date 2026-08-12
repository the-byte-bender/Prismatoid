namespace Prismatoid;

/// <summary>
/// The type of a function invoked when a backend's runtime availability changes.
/// </summary>
/// <param name="backendId">The identifier of the backend whose availability changed.</param>
/// <param name="name">The name of the backend, e.g. "SAPI", "NVDA", "OneCore".</param>
/// <param name="available">
/// <see langword="true"/> if the backend has become available, <see langword="false"/> if
/// it has become unavailable.
/// </param>
/// <remarks>
/// <para>
/// The callback is invoked only when a backend's confirmed availability changes: a backend
/// that remains available or remains unavailable across many scans produces no callbacks,
/// and the first scan after polling starts establishes a baseline without invoking the
/// callback. An application that needs to know the initial availability of a backend can
/// query it directly (see <see cref="PrismContext.GetBackendAvailability"/>).
/// </para>
/// <para>
/// An application typically responds to a callback by discarding a backend instance it can
/// no longer use and, when a preferred backend becomes available, acquiring it. The
/// callback is a notification that the application's cached choice of backend may be
/// stale, and does not itself change any backend instance the application holds.
/// </para>
/// </remarks>
public delegate void AvailabilityChangedCallback(
    PrismBackendId backendId,
    string name,
    bool available
);
