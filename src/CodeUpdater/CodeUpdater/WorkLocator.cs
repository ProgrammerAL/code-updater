using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater;

public record UpdateWork(ImmutableArray<string> ValidDirectories, ImmutableArray<string> CsProjectFiles, ImmutableArray<string> NpmDirectories);

public class WorkLocator(ILogger Logger, UpdateOptions UpdateOptions)
{
    public UpdateWork DetermineUpdateWork(string rootDirectory)
    {
        var validDirectories = DetermineValidDirectories(rootDirectory);
        var csProjFiles = FindCsProjFiles(validDirectories);
        var npmDirectories = FindNpmDirectories(validDirectories);

        return new UpdateWork(validDirectories, csProjFiles, npmDirectories);
    }

    private ImmutableArray<string> DetermineValidDirectories(string rootDirectory)
    {
        var skipPaths = DetermineSkipPaths(UpdateOptions.UpdatePathOptions.IgnorePatterns);

        //Get all directories, including subdirectories
        //  Make sure the directory path string includes a trailing slash so we can compare it to the skip paths
        //  Replace backslashes with forward slashes so we can compare them to the skip paths
        var allDirectories = Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                                       .Select(x => $"{x}/".Replace('\\', '/'));
        var validDirectories = new List<string>();

        foreach (var directoryPath in allDirectories)
        {
            var skipPath = skipPaths.FirstOrDefault(x => directoryPath.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (skipPath is object)
            {
                Logger.Debug($"Skipping directory '{directoryPath}' because it's path should be ignored by rule: {skipPath}");
            }
            else
            {
                validDirectories.Add(directoryPath);
            }
        }

        return validDirectories.ToImmutableArray();
    }

    public ImmutableArray<string> FindCsProjFiles(ImmutableArray<string> validDirectories)
    {
        if (UpdateOptions.CSharpOptions is null)
        {
            Logger.Information("No CSharpOptions config set, will not attempt to update any C# code");
            return ImmutableArray<string>.Empty;
        }

        var validCsProjFilesPaths = new List<string>();

        foreach (var dir in validDirectories)
        {
            var allCsProjFilesPaths = Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly);
            validCsProjFilesPaths.AddRange(allCsProjFilesPaths);
        }

        return validCsProjFilesPaths.ToImmutableArray();
    }

    public ImmutableArray<string> FindNpmDirectories(ImmutableArray<string> validDirectories)
    {
        if (UpdateOptions.NpmOptions is null)
        {
            Logger.Information("No NpmOptions config set, will not attempt to update NPM Packages");
            return ImmutableArray<string>.Empty;
        }

        var validPaths = new List<string>();

        foreach (var dir in validDirectories)
        {
            var packageJsonFiles = Directory.GetFiles(dir, "package.json", SearchOption.TopDirectoryOnly);

            var packageJsonFile = packageJsonFiles.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(packageJsonFile))
            {
                Logger.Debug($"Skipping directory '{dir}' because it doesn't have a package.json file in it");
                continue;
            }

            var packagePath = Path.GetDirectoryName(packageJsonFile);
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                Logger.Debug($"Skipping '{packageJsonFile}' file because it's package.json path is null or empty");
                continue;
            }

            validPaths.Add(packagePath);
        }

        return validPaths.ToImmutableArray();
    }

    private ImmutableArray<string> DetermineSkipPaths(IEnumerable<string> additionalSkipPaths)
    {
        //Only use forward slashes for paths, even on Windows. They work everywhere, even on Windows.
        var skipPaths = new[]
        {
            //Ignore all obj and bin folders
            @"/obj/",
            @"/obj/Debug/",
            @"/obj/Release/",
            @"/bin/",
            @"/bin/Debug/",
            @"/bin/Release/",

            //Ignore packages inside node_modules
            @"/node_modules/",

            //Ignore some other app specific folders
            @"/.git/",
            @"/.vs/",
        }
        .ToImmutableArray();

        var normalizedAdditionalPaths = additionalSkipPaths.Select(x => x.Replace('\\', '/'));
        skipPaths = skipPaths.AddRange(normalizedAdditionalPaths);

        return skipPaths;
    }
}
