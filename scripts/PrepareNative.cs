// This will download a pinned Prism SDK version from GitHub releases, extract the native binaries for each supported platform, and stage them so that on the next `dotnet pack` they will be included properly in the NuGet package.

using System.IO.Compression;
using System.Net.Http.Headers;

const string GithubRepo = "ethindp/prism";
const string PrismVersion = "v0.17.3";
const string TargetProject = "Prismatoid";

(string SdkPath, string Rid)[] Rids =
[
    ("windows/x64/dynamic/release/bin", "win-x64"),
    ("windows/arm64/dynamic/release/bin", "win-arm64"),
    ("linux/x64/dynamic/release/lib", "linux-x64"),
    ("linux/arm64/dynamic/release/lib", "linux-arm64"),
    ("macos/universal/dynamic/release/lib", "osx-x64"),
    ("macos/universal/dynamic/release/lib", "osx-arm64"),
];

var rootDir = FindProjectRoot(Directory.GetCurrentDirectory());
var stagingDir = Path.Combine(rootDir, TargetProject, "staging", "runtimes");
var stagingRoot = Path.Combine(rootDir, TargetProject, "staging");
var versionFile = Path.Combine(stagingRoot, "version.txt");

Console.WriteLine(
    $"""
    Target: {TargetProject}
    Repo:   {GithubRepo}
    Pin:    {PrismVersion}
    Output: {stagingDir}
    """
);

// Avoid network access entirely when the pinned version is already staged.
if (
    File.Exists(versionFile)
    && File.ReadAllText(versionFile).Trim() == PrismVersion
    && Directory.Exists(stagingDir)
)
{
    Console.WriteLine($"Already up to date ({PrismVersion}). Skipping.");
    return;
}

var sdkAssetName = $"prism-sdk-{PrismVersion}.zip";
var sdkAssetUrl =
    $"https://github.com/{GithubRepo}/releases/download/{PrismVersion}/{sdkAssetName}";

Console.WriteLine($"Fetching: {sdkAssetName}");

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Prism-Stager", "1.0"));

var tempZip = Path.GetTempFileName();
var extractDir = Path.Combine(Path.GetTempPath(), $"prism-sdk-{Guid.NewGuid()}");

try
{
    Console.WriteLine($"Downloading...");
    using (var stream = await http.GetStreamAsync(sdkAssetUrl))
    using (var fs = File.Create(tempZip))
    {
        await stream.CopyToAsync(fs);
    }

    Console.WriteLine($"Extracting to temporary folder...");
    ZipFile.ExtractToDirectory(tempZip, extractDir);

    var actualSdkRoot =
        Directory.EnumerateDirectories(extractDir).FirstOrDefault()
        ?? throw new DirectoryNotFoundException(
            "SDK zip did not contain the expected root folder."
        );

    if (Directory.Exists(stagingDir))
    {
        Directory.Delete(stagingDir, true);
    }

    foreach (var (sdkPath, rid) in Rids)
    {
        var sourceDir = Path.Combine(actualSdkRoot, sdkPath);
        var destDir = Path.Combine(stagingDir, rid, "native");

        if (Directory.Exists(sourceDir))
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);

                // Tolk is a screen-reader compatibility shim that prism bundles for
                // legacy backends; Prismatoid doesn't need it. Skip it everywhere it
                // appears (tolk.dll on Windows, libtolk*.dylib on macOS).
                if (fileName.Contains("tolk", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetFile = Path.Combine(destDir, fileName);
                File.Copy(file, targetFile, true);
            }
        }
        else
        {
            Console.WriteLine($"SDK path not found: {sdkPath}");
        }
    }

    File.WriteAllText(versionFile, PrismVersion);
    Console.WriteLine("Native binaries successfully staged!");
}
finally
{
    if (File.Exists(tempZip))
        File.Delete(tempZip);
    if (Directory.Exists(extractDir))
        Directory.Delete(extractDir, true);
}

static string FindProjectRoot(string startDir)
{
    var curr = new DirectoryInfo(startDir);
    while (curr != null && !File.Exists(Path.Combine(curr.FullName, "Prismatoid.slnx")))
        curr = curr.Parent;
    return curr?.FullName
        ?? throw new Exception("Could not find project root by looking for Prismatoid.slnx");
}
