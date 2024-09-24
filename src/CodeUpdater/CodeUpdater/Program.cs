
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using CommandLine;

using ProgrammerAL.CodeUpdater;
using ProgrammerAL.CodeUpdater.Helpers;
using ProgrammerAL.CodeUpdater.Updaters;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

await Parser.Default.ParseArguments<CommandOptions>(args)
         .WithParsedAsync(async options =>
         {
             await RunAsync(options);
         });

static async ValueTask RunAsync(CommandOptions options)
{
    var logger = Log.Logger;

    var runProcessHelper = new RunProcessHelper(logger);
    var workLocator = new WorkLocator(logger);
    var validator = new PreRunValidator(logger, runProcessHelper);
    var cSharpUpdater = new CSharpUpdater(logger, runProcessHelper, options);
    var npmUpdater = new NpmUpdater(runProcessHelper);
    var compileRunner = new CompileRunner(logger, runProcessHelper);

    var skipPaths = workLocator.DetermineSkipPaths(options.IgnorePatterns);

    var updateWork = workLocator.DetermineUpdateWork(options.Directory, skipPaths);

    var canRun = await validator.VerifyCanRunAsync(updateWork);

    if (!canRun)
    {
        return;
    }

    var csUpdates = await cSharpUpdater.UpdateAllCSharpProjectsAsync(updateWork);
    var npmUpdates = npmUpdater.UpdateNpmPackages(updateWork);

    //After updating everything, compile all projects
    //  Don't do this in the above loop in case a project needs an update that would cause it to not compile
    //  So wait for all projects to be updated
    var compileResults = await compileRunner.CompileProjectsAsync(updateWork, options.NpmBuildCommand);

    OutputSummary(updateWork, csUpdates, npmUpdates, compileResults, logger);
}

static void OutputSummary(UpdateWork updateWork, ImmutableArray<CSharpUpdateResult> csUpdates, NpmUpdates npmUpdates, CompileResults compileResults, ILogger logger)
{
#pragma warning disable IDE0058 // Expression value is never used
    var builder = new StringBuilder();

    builder.AppendLine();
    builder.AppendLine("Summary of updates:");
    builder.AppendLine($"CsProj Files: {csUpdates.Length}");
    builder.AppendLine($"NPM Directories: {npmUpdates.NpmDirectories.Length}");

    builder.AppendLine();
    var nugetListFailures = csUpdates.Where(x => !x.NugetUpdates.RetrievedPackageListSuccessfully).ToImmutableArray();
    builder.AppendLine($"Nuget List Failures: {nugetListFailures.Length}");
    foreach (var nugetListFailure in nugetListFailures)
    {
        builder.AppendLine($"\t{nugetListFailure.CsprojFile}");
    }

    builder.AppendLine();
    var nugetUpdateFailuresCount = csUpdates.SelectMany(x => x.NugetUpdates.Updates).Where(x => !x.UpdatedSuccessfully).Count();
    builder.AppendLine($"Nuget Update Failures: {nugetUpdateFailuresCount}");
    foreach (var csUpdate in csUpdates)
    {
        var nugetUpdateFailures = csUpdate.NugetUpdates.Updates.Where(x => !x.UpdatedSuccessfully).ToImmutableArray();
        if (nugetUpdateFailures.Any())
        {
            builder.AppendLine($"\t{csUpdate.CsprojFile}");
            foreach (var nugetUpdate in nugetUpdateFailures)
            {
                builder.AppendLine($"\t\t{nugetUpdate.PackageId}");
            }
        }
    }

    builder.AppendLine();
    var csProjCompileFailures = compileResults.CompileCsProjResults.Results.Where(x => x.BuildResult != CompileResultType.Success).ToImmutableArray();
    builder.AppendLine($"CsProj Build Failures: {csProjCompileFailures.Length}");
    foreach (var csProjCompileFailure in csProjCompileFailures)
    {
        builder.AppendLine($"\t{csProjCompileFailure.BuildResult}:{csProjCompileFailure.CsProjFile}");
    }

    builder.AppendLine();
    var npmCompileFailures = compileResults.CompileNpmDirectoryResults.Results.Where(x => x.BuildResult != CompileResultType.Success).ToImmutableArray();
    builder.AppendLine($"NPM Directory Build Failures: {npmCompileFailures.Length}");
    foreach (var npmCompileFailure in npmCompileFailures)
    {
        builder.AppendLine($"\t{npmCompileFailure.BuildResult}:{npmCompileFailure.Directory}");
    }

    logger.Information(builder.ToString());
#pragma warning restore IDE0058 // Expression value is never used
}
