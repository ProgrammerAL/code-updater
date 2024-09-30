using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater;

public record UpdateWork(ImmutableArray<string> CsProjectFiles, ImmutableArray<string> NpmDirectories);

public class WorkLocator(ILogger Logger)
{
    public ImmutableArray<string> DetermineSkipPaths(IEnumerable<string> additionalSkipPaths)
    {
        var skipPaths = new[]
        {
            //Ignore all obj and bin folders
            @"/obj/Debug/",
            @"/obj/Release/",
            @"/bin/Debug/",
            @"/bin/Release/",

            //Ignore packages inside node_modules
            @"/node_modules/"
        }
        .ToImmutableArray();

        //Include the same paths, but with backslashes so this is cross-platform
        skipPaths = skipPaths.AddRange(skipPaths.Select(x => x.Replace("/", "\\")));

        skipPaths = skipPaths.AddRange(additionalSkipPaths);

        return skipPaths;
    }

    public UpdateWork DetermineUpdateWork(string rootDirectory, ImmutableArray<string> skipPaths)
    { 
        var csProjFiles = FindCsProjFiles(rootDirectory, skipPaths);
        var npmDirectories = FindNpmDirectories(rootDirectory, skipPaths);

        return new UpdateWork(csProjFiles, npmDirectories);
    }

    public ImmutableArray<string> FindCsProjFiles(string rootDirectory, ImmutableArray<string> skipPaths)
    {
        var allCsProjFilesPaths = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories);
        var validCsProjFilesPaths = new List<string>();

        foreach (var csProjFilePath in allCsProjFilesPaths)
        {
            var skipPath = skipPaths.FirstOrDefault(x => csProjFilePath.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (skipPath is object)
            {
                Logger.Information($"Skipping '{csProjFilePath}' file because it's path should be ignored by rule: {skipPath}");
            }
            else
            {
                validCsProjFilesPaths.Add(csProjFilePath);
            }
        }

        return validCsProjFilesPaths.ToImmutableArray();
    }

    public ImmutableArray<string> FindNpmDirectories(string rootDirectory, ImmutableArray<string> skipPaths)
    {
        var allPackageJsonPaths = Directory.GetFiles(rootDirectory, "package.json", SearchOption.AllDirectories);
        var validPaths = new List<string>();

        foreach (var packageJsonPath in allPackageJsonPaths)
        {
            var packagePath = Path.GetDirectoryName(packageJsonPath);
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                Logger.Information($"Skipping '{packageJsonPath}' file because it's path is null or empty");
                continue;
            }

            var skipPath = skipPaths.FirstOrDefault(x => packagePath.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (skipPath is object)
            {
                Logger.Information($"Skipping '{packagePath}' because it's path should be ignored by rule: {skipPath}");
            }
            else
            {
                validPaths.Add(packagePath);
            }
        }

        return validPaths.ToImmutableArray();
    }
}
