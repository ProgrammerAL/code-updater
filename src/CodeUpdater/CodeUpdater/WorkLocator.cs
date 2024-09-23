using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAL.CodeUpdater;
public class WorkLocator
{
    public ImmutableArray<string> DetermineSkipPaths()
    {
        var skipPaths = new[]
        {
            //Ignore all obj and bin folders
            @"/obj/Debug/",
            @"/obj/Release/",
            @"/bin/Debug/",
            @"/bin/Release/",

            //Ignore this code
            @"/CodeUpdater/",

            //Ignore packages inside node_modules
            @"/node_modules/"
        }.ToImmutableArray();

        //Include the same paths, but with backslashes so this is cross-platform
        skipPaths = skipPaths.AddRange(skipPaths.Select(x => x.Replace("/", "\\")));

        return skipPaths;
    }

    public ImmutableArray<string> FindCsProjFilesAsync(string rootDirectory, ImmutableArray<string> skipPaths)
    {
        var allCsProjFilesPaths = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories);
        var validCsProjFilesPaths = new List<string>();

        foreach (var csProjFilePath in allCsProjFilesPaths)
        {
            var skipPath = skipPaths.FirstOrDefault(x => csProjFilePath.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (skipPath is object)
            {
                Console.WriteLine($"Skipping '{csProjFilePath}' file because it's path should be ignored by rule: {skipPath}");
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
        var validPackageJsonPaths = new List<string>();

        foreach (var packageJsonPath in allPackageJsonPaths)
        {
            var skipPath = skipPaths.FirstOrDefault(x => packageJsonPath.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (skipPath is object)
            {
                Console.WriteLine($"Skipping '{packageJsonPath}' file because it's path should be ignored by rule: {skipPath}");
            }
            else
            {
                validPackageJsonPaths.Add(packageJsonPath);
            }

            validPackageJsonPaths.Add(packageJsonPath);
        }

        return validPackageJsonPaths.ToImmutableArray();
    }
}
