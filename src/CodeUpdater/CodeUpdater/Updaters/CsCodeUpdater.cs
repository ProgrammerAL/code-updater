using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class CsCodeUpdater(ILogger Logger, IRunProcessHelper RunProcessHelper, UpdateOptions UpdateOptions)
{
    public async ValueTask<DotnetFormatResult> RunDotnetFormatAsync(string csProjFilePath)
    {
        if (!UpdateOptions.RunDotnetFormat)
        {
            return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.DidNotRun);
        }

        string commandArgs = $"format \"{csProjFilePath}\"";
        try
        {
            var processResult = await RunProcessHelper.RunProcessToCompletionAndGetOutputAsync("dotnet", commandArgs);
            if (processResult.CompletedSuccessfully)
            {
                return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.RanSuccessfully);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error running dotnet format with command args '{CommandArgs}'", commandArgs);
        }

        return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.Erroreded);
    }
}
