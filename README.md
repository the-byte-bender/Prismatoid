# Prismatoid

High-performance .NET bindings for the [Prism speech library](https://github.com/ethindp/prism).

## Quick Start

Initialize a `PrismContext` at application startup. Use the context to acquire backends, either by requesting the highest-priority one or selecting a specific driver.

```csharp
using Prismatoid;

// Initialize context; keep this around and dispose it when done.
using var context = new PrismContext();

// Acquire the current best backend.
using var backend = context.AcquireBestBackend();

// Output to speech, braille, or both.
backend.Speak("Speech only", interrupt: false);
backend.Braille("Braille only");
backend.Output("Both speech and braille", interrupt: true);
```

## Backend Selection

Backends are identified by a stable `PrismBackendId`. Use the predefined constants to select a specific driver, or enumerate the registry to discover what is available.

```csharp
// Select a specific backend by its well-known identifier.
using var sapi = context.AcquireBackend(PrismBackendId.Sapi);

// Or look one up by name.
var id = context.GetBackendId("NVDA");
if (id != PrismBackendId.Invalid)
{
    using var nvda = context.AcquireBackend(id);
}

// Enumerate all registered backends, in descending priority order.
foreach (var info in context.AvailableBackends)
{
    Console.WriteLine($"[{info.Id}] {info.Name} (priority {info.Priority})");
}
```

## Background Availability

Some backends (screen readers, for example) start and stop while your app runs. Pass a callback to `PrismContext` to be notified of transitions.

```csharp
using var context = new PrismContext(
    availabilityChanged: (id, name, available) =>
        Console.WriteLine($"{name} is now {(available ? "available" : "unavailable")}"),
);
// Check the documentation for details on more parameters you can pass to the constructor
```

You can also pause/resume polling manually, and check whether automatic power management is supported:

```csharp
context.PauseAvailabilityPolling();
context.ResumeAvailabilityPolling();
bool autoPower = PrismContext.IsAutoPowerManagementSupported();
```

The callback fires only on transitions; the first scan establishes a baseline silently. To check the current availability of a backend instance, query its features:

```csharp
bool availableNow = backend.Features.HasFlag(PrismBackendFeature.IsSupportedAtRuntime);
```

## Voices and Parameters

Voices and speech parameters are exposed per backend. Check the feature flags before using an operation, since backends differ in what they support.

```csharp
// Find a voice by language and name.
var voice = backend.Voices
    .FirstOrDefault(v => v.Language.StartsWith("en") && v.Name.Contains("Hazel"));

if (voice != default)
{
    backend.CurrentVoice = voice;
}

if (backend.Features.HasFlag(PrismBackendFeature.SupportsSetRate))
    backend.Rate = 0.75f;

if (backend.Features.HasFlag(PrismBackendFeature.SupportsSetVolume))
    backend.Volume = 1.0f;
```

## Audio Synthesis

Some backends support direct synthesis to memory.

```csharp
// Synthesize text and stream 32-bit float PCM chunks (interleaved).
foreach (var chunk in backend.SpeakToMemory("Hello from the memory backend"))
{
    // Each chunk is a ReadOnlyMemory<float>; valid until the next iteration.
    Console.WriteLine($"Received {chunk.Length} samples");
}
```

The audio format can be queried via `backend.Channels`, `backend.SampleRate`, and `backend.BitDepth`. Note that `BitDepth` reflects the backend's native format; samples delivered to `SpeakToMemory` are always 32-bit float.

## Logging

```csharp
PrismLog.SetHandler((level, source, message) =>
    Console.WriteLine($"[{level}] {source}: {message}")
);
PrismLog.SetLevel(PrismLogLevel.Debug);   

// Emit your own messages through the same pipeline.
PrismLog.Log(PrismLogLevel.Info, "my-app", "Starting up");
```

`SetHandler` returns the previously installed handler, so you can save and restore it later. Passing `null` disables delivery.

## Manual Backend Management

```csharp
// Reuse the shared instance for SAPI.
using var shared = context.AcquireBackend(PrismBackendId.Sapi);

// Create an independent instance with its own state.
using var unique = context.CreateBackend(PrismBackendId.OneCore);
```

## Error Handling

Operations throw `PrismException` on failure. The native error code is available via `PrismException.Error`:

```csharp
try
{
    backend.Speak("Hello");
}
catch (PrismException ex)
{
    Console.WriteLine($"Failed: {ex.Error} ({ex.Message})");
}
```

## License

MPL-2.0
