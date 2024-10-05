
using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

using CommandLine;

using ProgrammerAL.Tools.CodeUpdater;
using ProgrammerAL.Tools.CodeUpdater.Helpers;
using ProgrammerAL.Tools.CodeUpdater.Options;
using ProgrammerAL.Tools.CodeUpdater.Updaters;

using Serilog;

await Parser.Default.ParseArguments<CommandOptions>(args)
         .WithParsedAsync(async options =>
         {
             await RunAsync(options);
         });

static async ValueTask RunAsync(CommandOptions commandOptions)
{
    var updateOptions = await LoadUpdateOptionsAsync(commandOptions.ConfigFile);
    var logger = SetupLogger(updateOptions);

    var runProcessHelper = new RunProcessHelper(logger);
    var workLocator = new WorkLocator(logger);
    var validator = new PreRunValidator(logger, runProcessHelper);
    var cSharpUpdater = new CSharpUpdater(logger, runProcessHelper, updateOptions);
    var npmUpdater = new NpmUpdater(runProcessHelper);
    var compileRunner = new CompileRunner(logger, runProcessHelper);

    var skipPaths = workLocator.DetermineSkipPaths(updateOptions.IgnorePatterns);

    var updateWork = workLocator.DetermineUpdateWork(updateOptions.RootDirectory, skipPaths);

    var canRun = await validator.VerifyCanRunAsync(updateWork);

    if (!canRun)
    {
        logger.Fatal($"Cannot run, exiting before trying to do any work...");
        return;
    }

    var csUpdates = await cSharpUpdater.UpdateAllCSharpProjectsAsync(updateWork);
    var npmUpdates = npmUpdater.UpdateNpmPackages(updateWork);

    //After updating everything, compile all projects
    //  Don't do this in the above loop in case a project needs an update that would cause it to not compile
    //  So wait for all projects to be updated
    var compileResults = await compileRunner.CompileProjectsAsync(updateWork, updateOptions.NpmBuildCommand);

    OutputSummary(updateWork, csUpdates, npmUpdates, compileResults, logger);
}

static ILogger SetupLogger(UpdateOptions updateOptions)
{
    var loggerConfig = new LoggerConfiguration()
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(updateOptions.OutputFile))
    {
        loggerConfig = loggerConfig.WriteTo.File(updateOptions.OutputFile);
    }

    if (string.IsNullOrWhiteSpace(updateOptions.LogLevel))
    {
        loggerConfig = loggerConfig.MinimumLevel.Verbose();
    }
    else
    {
        loggerConfig = updateOptions.LogLevel.ToLower() switch
        {
            "verbose" => loggerConfig.MinimumLevel.Verbose(),
            "info" => loggerConfig.MinimumLevel.Information(),
            "warn" => loggerConfig.MinimumLevel.Warning(),
            "error" => loggerConfig.MinimumLevel.Error(),
            _ => loggerConfig.MinimumLevel.Verbose(),
        };
    }

    Log.Logger = loggerConfig.CreateLogger();
    return Log.Logger;
}

static async Task<UpdateOptions> LoadUpdateOptionsAsync(string configFilePath)
{
    if (!File.Exists(configFilePath))
    {
        throw new Exception($"Config file does not exist at path: {configFilePath}");
    }

    var updateOptionJson = await File.ReadAllTextAsync(configFilePath);
    if (string.IsNullOrWhiteSpace(updateOptionJson))
    {
        throw new Exception($"Config file is empty at path: {configFilePath}");
    }

    var updateOptions = JsonSerializer.Deserialize<UpdateOptions>(updateOptionJson, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });

    if (updateOptions is null)
    {
        throw new Exception($"Could not deserialize config file from path: {configFilePath}");
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(updateOptions, serviceProvider: null, items: null);
    var isValid = Validator.TryValidateObject(updateOptions, validationContext, validationResults: validationResults, validateAllProperties: true);

    if (!isValid)
    {
        Log.Logger.Error($"Deserialized config file has invalid value(s). From path: {configFilePath}");
        foreach (var result in validationResults)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                Log.Logger.Error(result.ErrorMessage);
            }
        }

        throw new Exception($"Deserialized config file invalid from path: {configFilePath}");
    }

    return updateOptions;
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
