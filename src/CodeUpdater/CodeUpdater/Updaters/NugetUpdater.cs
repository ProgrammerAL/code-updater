using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAL.CodeUpdater.Updaters;
public class NugetUpdater
{
    public void UpdateNugetPackages(string csProjFilePath)
    {
        var csProjText = File.ReadAllText(csProjFilePath);
        var processStartArgs = new ProcessStartInfo("dotnet", $"list {csProjFilePath} package --format json")
        {
            RedirectStandardOutput = true,
        };
        var outdatedPackagesProcess = Process.Start(processStartArgs);
        outdatedPackagesProcess!.WaitForExit();

        var packagesJsonString = outdatedPackagesProcess.StandardOutput.ReadToEnd();
        var outdatedPackagesDto = System.Text.Json.JsonSerializer.Deserialize<NugetPackagesDto>(packagesJsonString, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var packages = outdatedPackagesDto?.Projects
            ?.SelectMany(x => x?.Frameworks ?? [])
            ?.SelectMany(x => x?.TopLevelPackages ?? [])
            .Where(x => x is object && !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => new NugetPackageToUpdate(Id: x!.Id!))
            .ToList() ?? [];

        //Some packages are automatically included based on the sdk, or something else I guess, I don't actually know
        //  Anyway, if the dotnet cli says a top-level package exists, but it's not in the csproj text, skip it
        //  Only update the nugets that are in the csproj file
        var topLevelPackages = packages.Where(x => csProjText.Contains($"Include=\"{x.Id}\"", StringComparison.OrdinalIgnoreCase))
            .ToList();

        //Note: dotnet cli doesn't have a great way to know what the latest version of a nuget package is
        //  So we'll just update all packages to the latest version by using the 'dotnet add package' command
        foreach (var package in topLevelPackages)
        {
            var packageId = package.Id;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"\t Updating package: {packageId}");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            var addProcess = Process.Start("dotnet", $"add \"{csProjFilePath}\" package {packageId}");
            addProcess.WaitForExit();
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    public class NugetPackagesDto
    {
        public ProjectDto?[]? Projects { get; set; }
        public class ProjectDto
        {
            public FrameworkDto?[]? Frameworks { get; set; }
        }

        public class FrameworkDto
        {
            public string? Framework { get; set; }
            public TopLevelPackagesDto?[]? TopLevelPackages { get; set; }
        }

        public class TopLevelPackagesDto
        {
            public string? Id { get; set; }
        }
    }

    public record NugetPackageToUpdate(string Id);
}
