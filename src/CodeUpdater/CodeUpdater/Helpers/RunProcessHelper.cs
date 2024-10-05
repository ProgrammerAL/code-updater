using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProgrammerAl.SourceGenerators.PublicInterfaceGenerator.Attributes;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Helpers;

[GenerateInterface]
public class RunProcessHelper(ILogger Logger) : IRunProcessHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public record ProcessOutput(bool Started, bool CompletedSuccessfully, string Output);

    public void RunProwerShellCommandToCompletion(string path, string commandString)
    {
        //Note: Running this in PowerShell because it's easier to run npm commands in PowerShell
        //      On Windows, for some reason running 'npm' using Process.Start() doesn't work. The process can't be found, you have to change it to npm.cmd
        //      But also on Windows, we can't get the output. So as a workaround to all of this, just run it through PowerShell
        var startInfo = new ProcessStartInfo("pwsh", $"-Command \"{commandString}\"")
        {
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
            _ = process.WaitForExit(DefaultTimeout);
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

        //Get the task, then finalize the read once the wait is comepleted
        //https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why/53504707#53504707
        var processOutputTask = process.StandardOutput.ReadToEndAsync();
        var completedSuccessfully = process.WaitForExit(DefaultTimeout);
        if (completedSuccessfully)
        {
            var processOutput = await processOutputTask;
            return new ProcessOutput(Started: true, CompletedSuccessfully: completedSuccessfully, Output: processOutput);
        }

        return new ProcessOutput(Started: true, CompletedSuccessfully: false, Output: "");
    }


    public async Task<ProcessOutput> RunProcessToCompletionAndGetOutputAsync(string fileName, string arguments)
    {
        var processArgs = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
        };
        var process = Process.Start(processArgs);

        if (process is null)
        {
            return new ProcessOutput(Started: false, CompletedSuccessfully: false, Output: "");
        }

        var processOutputTask = process.StandardOutput.ReadToEndAsync();
        var completedSuccessfully = process.WaitForExit(DefaultTimeout);
        var processOutput = await processOutputTask;

        return new ProcessOutput(Started: true, CompletedSuccessfully: completedSuccessfully, Output: processOutput);
    }
}
