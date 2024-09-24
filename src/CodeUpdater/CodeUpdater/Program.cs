
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
    var cSharpUpdater = new CSharpUpdater(logger);
    var npmUpdater = new NpmUpdater(runProcessHelper);
    var compileRunner = new CompileRunner(logger, runProcessHelper);

    var skipPaths = workLocator.DetermineSkipPaths(options.IgnorePatterns);

    var updateWork = workLocator.DetermineUpdateWork(options.RootDirectory, skipPaths);

    var canRun = await validator.VerifyCanRunAsync(updateWork);

    if (!canRun)
    {
        return;
    }

    //cSharpUpdater.UpdateAllCSharpProjects(updateWork);
    npmUpdater.UpdateNpmPackages(updateWork);

    //After updating everything, compile all projects
    //  Don't do this in the above loop in case a project needs an update that would cause it to not compile
    //  So wait for all projects to be updated
    await compileRunner.CompileProjectsAsync(updateWork, options.NpmBuildCommand);
}


