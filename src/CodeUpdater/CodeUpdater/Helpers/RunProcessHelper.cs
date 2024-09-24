using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

using ProgrammerAl.SourceGenerators.PublicInterfaceGenerator.Attributes;

namespace ProgrammerAL.CodeUpdater.Helpers;

[GenerateInterface]
public class RunProcessHelper(ILogger Logger) : IRunProcessHelper
{
    public record ProcessOutput(bool Started, bool CompletedSuccessfully, string Output);

    public void RunProwerShellCommandToCompletion(string path, string commandString)
    {
        //Note: Running this in PowerShell because it's easier to run npm commands in PowerShell
        //      On Windows, for some reason running 'npm' using Process.Start() doesn't work. The process can't be found, you have to change it to npm.cmd
        //      But also on Windows, we can't get the output. So as a workaround to all of this, just run it through PowerShell
        var startInfo = new ProcessStartInfo("pwsh", $"-Command \"{commandString}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = path
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            Logger.Error("Could not start PowerShell process to run command");
        }
        else
        {
            process.WaitForExit();
        }
    }

    public async ValueTask<ProcessOutput> RunProwerShellCommandToCompletionAndGetOutputAsync(string path, string commandString)
    {
        //Note: Running this in PowerShell because it's easier to run npm commands in PowerShell
        //      On Windows, for some reason running 'npm' using Process.Start() doesn't work. The process can't be found, you have to change it to npm.cmd
        //      But also on Windows, we can't get the output. So as a workaround to all of this, just run it through PowerShell
        var startInfo = new ProcessStartInfo("pwsh", $"-Command \"{commandString}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = path
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            return new ProcessOutput(Started: false, CompletedSuccessfully: false, Output: "");
        }
        else
        {
            process.WaitForExit();
        }

        var completedSuccessfully = process.WaitForExit(TimeSpan.FromMinutes(5));
        var processOutput = await process.StandardOutput.ReadToEndAsync();

        return new ProcessOutput(Started: true, CompletedSuccessfully: completedSuccessfully, Output: processOutput);
    }


    public async Task<ProcessOutput> RunProcessToCompletionAndGetOutputAsync(string fileName, string arguments)
    {
        var processArgs = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true
        };
        var process = Process.Start(processArgs);

        if (process is null)
        {
            return new ProcessOutput(Started: false, CompletedSuccessfully: false, Output: "");
        }

        var completedSuccessfully = process.WaitForExit(TimeSpan.FromMinutes(5));
        var processOutput = await process.StandardOutput.ReadToEndAsync();

        return new ProcessOutput(Started: true, CompletedSuccessfully: completedSuccessfully, Output: processOutput);
    }
}
