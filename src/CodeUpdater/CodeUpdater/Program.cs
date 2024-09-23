
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using CommandLine;

using ProgrammerAL.CodeUpdater;
using ProgrammerAL.CodeUpdater.Updaters;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

await Parser.Default.ParseArguments<CommandOptions>(args)
         .WithParsedAsync<CommandOptions>(async options =>
         {
             await RunAsync(options);
         });

static async ValueTask RunAsync(CommandOptions options)
{
    var logger = Log.Logger;

    var workLocator = new WorkLocator(logger);
    var validator = new PreRunValidator(logger);
    var nugetUpdater = new NugetUpdater(logger);
    var csProjUpdater = new CsProjUpdater(logger);
    var compileRunner = new CompileRunner(logger);

    var skipPaths = workLocator.DetermineSkipPaths(options.IgnorePatterns);

    var updateWork = workLocator.DetermineUpdateWork(options.RootDirectory, skipPaths);

    var canRun = await validator.VerifyCanRunAsync(updateWork);

    if (!canRun)
    {
        return;
    }

    var csProjFilesPaths = workLocator.FindCsProjFiles(options.RootDirectory, skipPaths);

    foreach (var csProjFilePath in csProjFilesPaths)
    {
        logger.Information($"Updating '{csProjFilePath}'");

        nugetUpdater.UpdateNugetPackages(csProjFilePath);
        csProjUpdater.UpdateCsProjPropertyValues(csProjFilePath);
    }

    //After updating everything, compile all projects
    //  Don't do this in the above loop in case a project needs an update that would cause it to not compile
    //  So wait for all projects to be updated
    await compileRunner.CompileProjectsAsync(csProjFilesPaths);
}


