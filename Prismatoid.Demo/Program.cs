using Prismatoid;

using PrismContext context = new();

Console.WriteLine("Available Backends:");
foreach (var info in context.AvailableBackends)
{
    Console.WriteLine(
        $"[{info.Id:X16}] {info.Name, -15} | Priority: {info.Priority} | Supported: {info.IsSupported}"
    );
}

using var backend = context.AcquireBestBackend();
Console.WriteLine($"Active Backend: {backend.Name}");

Console.WriteLine("Supported Features:");
foreach (PrismBackendFeature feature in Enum.GetValues<PrismBackendFeature>())
{
    if (backend.Features.HasFlag(feature))
    {
        Console.WriteLine($" - {feature}");
    }
}

string vol =
    (backend.Features & PrismBackendFeature.SupportsGetVolume) != 0
        ? backend.Volume.ToString("P0")
        : "Not Supported";
string rate =
    (backend.Features & PrismBackendFeature.SupportsGetRate) != 0
        ? backend.Rate.ToString("P0")
        : "Not Supported";
string pitch =
    (backend.Features & PrismBackendFeature.SupportsGetPitch) != 0
        ? backend.Pitch.ToString("P0")
        : "Not Supported";
Console.WriteLine($"Current Settings: Volume: {vol}, Rate: {rate}, Pitch: {pitch}");

Console.WriteLine("Available Voices:");
foreach (var voice in backend.Voices)
{
    Console.WriteLine($"- {voice.Name} ({voice.Language})");
}

if (backend.CurrentVoice is PrismVoice current)
{
    Console.WriteLine($"\nCurrent Voice: {current.Name}");
}

const string testText = "Hello, world!";
Console.WriteLine($"Speaking: \"{testText}\"");
backend.Speak(testText);

try
{
    Console.WriteLine($"Format: {backend.Channels} channels, {backend.SampleRate}Hz");
    int chunkCount = 0;
    long totalSamples = 0;
    foreach (var chunk in backend.SpeakToMemory("This is a test of memory synthesis."))
    {
        chunkCount++;
        totalSamples += chunk.Length;
    }
    Console.WriteLine(
        $"Memory Synthesis Complete: Received {chunkCount} chunks, {totalSamples} total samples."
    );
}
catch (PrismException e)
{
    Console.WriteLine($"Memory synthesis not supported: {e.Message}");
}

Console.ReadLine();

backend.Stop();
