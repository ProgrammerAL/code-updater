
using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class CsCodeUpdater(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public async ValueTask<DotnetFormatResult> RunDotnetFormatAsync(string csProjFilePath, CSharpStyleOptions cSharpStyleOptions)
    {
        if (!cSharpStyleOptions.RunDotnetFormat)
        {
            return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.DidNotRun);
        }

        string commandArgs = $"format \"{csProjFilePath}\" --no-restore --verbosity diagnostic";
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
