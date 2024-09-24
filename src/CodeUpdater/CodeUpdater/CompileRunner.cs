using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProgrammerAL.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.CodeUpdater;
public class CompileRunner(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public async ValueTask<CompileResults> CompileProjectsAsync(UpdateWork updateWork, string npmBuildCommand)
    {
        var csharpBuildResults = await CompileAllCSharpProjectsAsync(updateWork);
        var npmDirectoryBuildResults = await BuildAllNpmDirectoriesAsync(updateWork, npmBuildCommand);

        return new CompileResults(csharpBuildResults, npmDirectoryBuildResults);
    }

    private async ValueTask<CompileNpmDirectoryResults> BuildAllNpmDirectoriesAsync(UpdateWork updateWork, string npmBuildCommand)
    {
        if (!updateWork.NpmDirectories.Any())
        {
            Logger.Information($"No NPM directories to build");
            return new CompileNpmDirectoryResults(ImmutableArray<CompileNpmDirectoryResult>.Empty);
        }

        Logger.Information($"Building NPM Directories");

        var builder = ImmutableArray.CreateBuilder<CompileNpmDirectoryResult>();
        foreach (var project in updateWork.NpmDirectories)
        {
            Logger.Information($"Building '{project}'");
            var command = $"npm run {npmBuildCommand}";
            var result = await RunProcessHelper.RunProwerShellCommandToCompletionAndGetOutputAsync(project, command);

            if (!result.Started)
            {
                Logger.Error($"{project} - Because process did not start");
                builder.Add(new CompileNpmDirectoryResult(project, CompileResultType.ProcessDidNotStart));
            }
            else if (!result.CompletedSuccessfully)
            {
                Logger.Error($"{project} - From build timeout");
                builder.Add(new CompileNpmDirectoryResult(project, CompileResultType.BuildTimeout));
            }
            else if (result.Output.Contains("ERROR"))
            {
                Logger.Error($"{project} - From build errors");
                builder.Add(new CompileNpmDirectoryResult(project, CompileResultType.BuildErrors));
            }
            else
            {
                builder.Add(new CompileNpmDirectoryResult(project, CompileResultType.Success));
            }
        }

        return new CompileNpmDirectoryResults(builder.ToImmutable());
    }

    private async ValueTask<CompileCsProjResults> CompileAllCSharpProjectsAsync(UpdateWork updateWork)
    {
        if (!updateWork.CsProjectFiles.Any())
        {
            Logger.Information($"No CSProj files to compile");
            return new CompileCsProjResults(ImmutableArray<CompileCsProjResult>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<CompileCsProjResult>();
        Logger.Information($"Compiling CSProj Files");

        foreach (var csProjFilePath in updateWork.CsProjectFiles)
        {
            //Sometimes files are still in use by the dotnet cli, so wait a bit before trying to compile
            Logger.Information($"Compiling '{csProjFilePath}'");

            var processResult = await RunProcessHelper.RunProcessToCompletionAndGetOutputAsync("dotnet", $"build \"{csProjFilePath}\" --no-incremental");

            if (!processResult.Started)
            {
                Logger.Error($"{csProjFilePath} - Because process did not start");
                builder.Add(new CompileCsProjResult(csProjFilePath, CompileResultType.ProcessDidNotStart));
            }
            else if (!processResult.CompletedSuccessfully)
            {
                Logger.Error($"{csProjFilePath} - From build timeout");
                builder.Add(new CompileCsProjResult(csProjFilePath, CompileResultType.BuildTimeout));
            }
            else if (processResult.Output.Contains("Build FAILED"))
            {
                Logger.Error($"{csProjFilePath} - From build errors");
                builder.Add(new CompileCsProjResult(csProjFilePath, CompileResultType.BuildErrors));
            }
            else
            {
                builder.Add(new CompileCsProjResult(csProjFilePath, CompileResultType.Success));
            }
        }

        return new CompileCsProjResults(builder.ToImmutable());
    }
}
