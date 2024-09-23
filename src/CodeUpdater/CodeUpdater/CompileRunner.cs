using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace ProgrammerAL.CodeUpdater;
public class CompileRunner(ILogger Logger)
{
    public async Task CompileProjectsAsync(IEnumerable<string> csProjFilePaths)
    {
        var buildFailureProjects = new List<string>();
        foreach (var csProjFilePath in csProjFilePaths)
        {
            //Sometimes files are still in use by the dotnet cli, so wait a bit before trying to compile
            Logger.Information($"Compiling '{csProjFilePath}'");
            var processArgs = new ProcessStartInfo("dotnet", $"build \"{csProjFilePath}\" --no-incremental")
            {
                RedirectStandardOutput = true
            };
            var process = Process.Start(processArgs)!;
            var processSuccessfullyExited = process.WaitForExit(TimeSpan.FromMinutes(5));
            var processOutput = await process.StandardOutput.ReadToEndAsync();
            if (processOutput.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase))
            {
                buildFailureProjects.Add($"{csProjFilePath} - From build errors");
            }
            else if (!processSuccessfullyExited)
            {
                buildFailureProjects.Add($"{csProjFilePath} - From build timeout");
            }
        }

        Logger.Information($"{buildFailureProjects.Count} projects failed to compile");
        foreach (var proj in buildFailureProjects)
        {
            Logger.Error($"\t{proj}");
        }
    }
}
