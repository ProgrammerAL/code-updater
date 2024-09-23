
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using ProgrammerAL.CodeUpdater;

using Serilog;
using Serilog.Core;

//TODO: Make this cli input
var startPath = @"./";

Console.ForegroundColor = ConsoleColor.White;

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var workLocator = new WorkLocator();
var validator = new PreRunValidator(workLocator, logger);
var nugetUpdater = new NugetUpdater();
var csProjUpdater = new CsProjUpdater();
var compileRunner = new CompileRunner();

var skipPaths = workLocator.DetermineSkipPaths();

var canRun = await validator.VerifyCanRunAsync(startPath, skipPaths);

if (!canRun)
{
    return;
}

var csProjFilesPaths = workLocator.FindCsProjFilesAsync(startPath, skipPaths);

foreach (var csProjFilePath in csProjFilesPaths)
{
    Console.WriteLine($"Updating '{csProjFilePath}'");

    nugetUpdater.UpdateNugetPackages(csProjFilePath);
    csProjUpdater.UpdateCsProjPropertyValues(csProjFilePath);
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine();

//After updating everything, compile all projects
//  Don't do this in the above loop in case a project needs an update that would cause it to not compile
//  So wait for all projects to be updated
await compileRunner.CompileProjectsAsync(csProjFilesPaths);

Console.ForegroundColor = ConsoleColor.White;
