using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Serilog;

namespace ProgrammerAL.CodeUpdater;
public class PreRunValidator(WorkLocator WorkLocator, ILogger Logger)
{
    public async ValueTask<bool> VerifyCanRunAsync(string rootDirectory, ImmutableArray<string> skipPaths)
    {
        var canRun = true;
        canRun &= await VerifyCanUpdateNpmPackagesAsync(rootDirectory, skipPaths);

        return canRun;
    }

    private async ValueTask<bool> VerifyCanUpdateNpmPackagesAsync(string rootDirectory, ImmutableArray<string> skipPaths)
    {
        //If no npm directories are found, then no npm packages to update, so this is valid
        if (!WorkLocator.FindNpmDirectories(rootDirectory, skipPaths).Any())
        {
            return true;
        }

        if (!await IsPowerShellInstalledAsync())
        {
            Logger.Error("PowerShell executable `pwsh` could not be run. It is required to update NPM packages. Install the latest version of PowerShell on this machine.");
            return false;
        }

        //Check if the "npm-check-updates" package is installed globally
        return await CanCheckNpmUpdatesAsync();
    }

    private async ValueTask<bool> IsPowerShellInstalledAsync()
    {
        var output = await RunProcessAndGetOutputAsync("Write-Host 'test'");
        return string.Equals(output, "test");
    }

    private async ValueTask<bool> CanCheckNpmUpdatesAsync()
    {
        var output = await RunProcessAndGetOutputAsync("npm list --global --json");
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        using var jsonDoc = JsonDocument.Parse(output);

        if (!jsonDoc.RootElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            Logger.Error($"`npm list` command is missing the `dependencies` element. Cannot verify the `npm-check-updates` package is installed");
            return false;
        }

        if (!dependenciesElement.TryGetProperty("npm-check-updates", out var npmCheckUpdatesElement))
        {
            Logger.Error($"`npm list` command is missing the `npm-check-updates` element, meaning that package is not installed. Before continuing, install that package by running `npm install -g npm-check-updates`.");
            return false;
        }

        return true;
    }

    private async ValueTask<string?> RunProcessAndGetOutputAsync(string commandString)
    {
        //Note: Running this in PowerShell because it's easier to run npm commands in PowerShell
        //      On Windows, for some reason running 'npm' using Process.Start() doesn't work. The process can't be found, you have to change it to npm.cmd
        //      But also on Windows, we can't get the output. So as a workaround to all of this, just run it through PowerShell
        var startInfo = new ProcessStartInfo("pwsh", $"-Command \"{commandString}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = Process.Start(startInfo);
        if (process is null)
        {
            Logger.Error("Could not start npm process");
            return null;
        }

        process.WaitForExit();

        var output = await process.StandardOutput.ReadToEndAsync();
        return output;
    }

    public class NpmPackagesListDto
    {
        public string? Name { get; set; }
        public Dictionary<string, string>? Dependencies { get; set; }
    }

    public class NpmPackageInfoDto
    {
        public string? Version { get; set; }
        public bool? Overridden { get; set; }
    }
}
