using System.Diagnostics;
using System.Text.RegularExpressions;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;


namespace PackageLister;

class Program
{
    static void Main()
    {
        string solutionPath = "../../../PackageLister.sln";

        string outputFilePath = "../../../output.txt";

        ListInstalledPackages(solutionPath, outputFilePath);
    }

    static void ListInstalledPackages(string solutionPath, string outputFilePath)
    {
        using (var streamWriter = new StreamWriter(outputFilePath))
        {
            Process process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"list {solutionPath} package";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                string? line = process.StandardOutput.ReadLine() ?? "";
                var match = Regex.Match(line, @">\s([^\s]+)\s+([0-9]+.[0-9]+.[0-9])");
                if (match.Success)
                {
                    string packageId = match.Groups[1].Value;
                    string version = match.Groups[2].Value;
                    Console.WriteLine($"Fetching Metadata for: {packageId} version {version}", packageId, version);
                    WritePackageInfoToFile(packageId, version, streamWriter);
                }
            }

            process.WaitForExit();
        }
    }

    static void WritePackageInfoToFile(string packageId, string version, StreamWriter streamWriter)
    {
        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        var sourceRepository = Repository.Factory.GetCoreV3(packageSource);
        var metadataResource = sourceRepository.GetResource<PackageMetadataResource>();

        var metadata = metadataResource
            .GetMetadataAsync(packageId, true, true, new SourceCacheContext(), NullLogger.Instance, default)
            .GetAwaiter().GetResult().FirstOrDefault(m => m.Identity.Version.ToString() == version);

        if (metadata is null) return;
        streamWriter.WriteLine($"Package: {metadata.Identity.Id} - {metadata.Identity.Version}");
        streamWriter.WriteLine($"License: {metadata.LicenseMetadata?.License ?? "See License URL"}");
        streamWriter.WriteLine($"License URL: {metadata.LicenseUrl?.ToString() ?? "Not Available"}");
        streamWriter.WriteLine($"Project URL: {metadata.ProjectUrl?.ToString() ?? "Not Available"}");
        
        streamWriter.WriteLine();
    }
}