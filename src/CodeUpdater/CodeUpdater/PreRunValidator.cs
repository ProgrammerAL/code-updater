using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater;
public class PreRunValidator(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public async ValueTask<bool> VerifyCanRunAsync(UpdateWork updateWork)
    {
        var canRun = true;
        canRun &= await VerifyCanUpdateNpmPackagesAsync(updateWork);

        return canRun;
    }

    private async ValueTask<bool> VerifyCanUpdateNpmPackagesAsync(UpdateWork updateWork)
    {
        //If no npm directories are found, then no npm packages to update, so this is valid
        if (!updateWork.NpmDirectories.Any())
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
        var commandResult = await RunProcessHelper.RunProwerShellCommandToCompletionAndGetOutputAsync(path: "./", "Write-Host 'test'");
        return string.Equals(commandResult.Output.Trim(), "test");
    }

    private async ValueTask<bool> CanCheckNpmUpdatesAsync()
    {
        var commandResult = await RunProcessHelper.RunProwerShellCommandToCompletionAndGetOutputAsync(path: "./", "npm list --global --json");
        if (string.IsNullOrWhiteSpace(commandResult.Output))
        {
            return false;
        }

        using var jsonDoc = JsonDocument.Parse(commandResult.Output);

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
