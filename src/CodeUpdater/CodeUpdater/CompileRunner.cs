using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProgrammerAL.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.CodeUpdater;
public class CompileRunner(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public async ValueTask CompileProjectsAsync(UpdateWork updateWork, string npmBuildCommand)
    {
        //await CompileAllCSharpProjectsAsync(updateWork);
        await BuildAllNpmDirectoriesAsync(updateWork, npmBuildCommand);
    }

    private async ValueTask BuildAllNpmDirectoriesAsync(UpdateWork updateWork, string npmBuildCommand)
    {
        if (!updateWork.NpmDirectories.Any())
        {
            Logger.Information($"No NPM directories to build");
        }
        else
        {
            Logger.Information($"Building NPM Directories");

            var buildFailureProjects = new List<string>();
            foreach (var project in updateWork.NpmDirectories)
            {
                Logger.Information($"Building '{project}'");
                var command = $"npm run {npmBuildCommand}";
                var result = await RunProcessHelper.RunProwerShellCommandToCompletionAndGetOutputAsync(project, command);

                if (!result.Started)
                {
                    buildFailureProjects.Add($"{project} - Because process did not start");
                }
                else if (!result.CompletedSuccessfully)
                {
                    buildFailureProjects.Add($"{project} - From build timeout");
                }
                else if (result.Output.Contains("ERROR"))
                {
                    buildFailureProjects.Add($"{project} - From build errors");
                }
            }

            Logger.Information($"{buildFailureProjects.Count} projects failed to build");
            foreach (var proj in buildFailureProjects)
            {
                Logger.Error($"{proj}");
            }
        }
    }

    private async ValueTask CompileAllCSharpProjectsAsync(UpdateWork updateWork)
    {
        if (!updateWork.CsProjectFiles.Any())
        {
            Logger.Information($"No CSProj files to compile");
        }
        else
        {
            Logger.Information($"Compiling CSProj Files");


            var buildFailureProjects = new List<string>();
            foreach (var csProjFilePath in updateWork.CsProjectFiles)
            {
                //Sometimes files are still in use by the dotnet cli, so wait a bit before trying to compile
                Logger.Information($"Compiling '{csProjFilePath}'");

                var processResult = await RunProcessHelper.RunProcessToCompletionAndGetOutputAsync("dotnet", $"build \"{csProjFilePath}\" --no-incremental");

                if (!processResult.Started)
                {
                    buildFailureProjects.Add($"{csProjFilePath} - Because process did not start");
                }
                else if (!processResult.CompletedSuccessfully)
                {
                    buildFailureProjects.Add($"{csProjFilePath} - From build timeout");
                }
                else if (processResult.Output.Contains("Build FAILED"))
                {
                    buildFailureProjects.Add($"{csProjFilePath} - From build errors");
                }
            }

            Logger.Information($"{buildFailureProjects.Count} projects failed to compile");
            foreach (var proj in buildFailureProjects)
            {
                Logger.Error($"{proj}");
            }
        }
    }
}
