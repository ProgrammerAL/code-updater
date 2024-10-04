using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class NugetUpdater(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public async ValueTask<NugetUpdateResults> UpdateNugetPackagesAsync(string csProjFilePath)
    {
        var csProjText = File.ReadAllText(csProjFilePath);
        var nugetUpdateOutput = await RunProcessHelper.RunProcessToCompletionAndGetOutputAsync("dotnet", $"list \"{csProjFilePath}\" package --format json");
        if (!nugetUpdateOutput.CompletedSuccessfully)
        {
            return new NugetUpdateResults(RetrievedPackageListSuccessfully: false, ImmutableArray<NugetUpdateResult>.Empty);
        }

        var outdatedPackagesDto = System.Text.Json.JsonSerializer.Deserialize<NugetPackagesDto>(nugetUpdateOutput.Output, new System.Text.Json.JsonSerializerOptions
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

        var builder = ImmutableArray.CreateBuilder<NugetUpdateResult>();

        //Note: dotnet cli doesn't have a great way to know what the latest version of a nuget package is
        //  So we'll just update all packages to the latest version by using the 'dotnet add package' command
        foreach (var package in topLevelPackages)
        {
            var packageId = package.Id;
            Logger.Information($"\t Updating package: {packageId}");

            var updateResult = await RunProcessHelper.RunProcessToCompletionAndGetOutputAsync("dotnet", $"add \"{csProjFilePath}\" package {packageId}");
            builder.Add(new NugetUpdateResult(csProjFilePath, packageId, UpdatedSuccessfully: updateResult.CompletedSuccessfully));
        }

        return new NugetUpdateResults(RetrievedPackageListSuccessfully: true, builder.ToImmutable());
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
