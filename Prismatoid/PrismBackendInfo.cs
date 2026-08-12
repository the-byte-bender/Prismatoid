namespace Prismatoid;

/// <summary>
/// Provides information about a backend registered with Prism.
/// </summary>
/// <remarks>
/// This describes the static registry entry: the registry's contents never change at
/// runtime. Whether a backend is usable at this moment is runtime availability, which is
/// reported through the availability callback (see <see cref="PrismContext"/>) and can be
/// queried on a backend instance via <see cref="IPrismBackend.Features"/>.
/// </remarks>
/// <param name="Id">The unique ID of the backend.</param>
/// <param name="Name">The human-readable name of the backend.</param>
/// <param name="Priority">The backend's priority (higher is more preferred).</param>
public readonly record struct PrismBackendInfo(
    PrismBackendId Id,
    string Name,
    int Priority
);
