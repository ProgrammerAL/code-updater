using System.Collections.Immutable;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class CSharpUpdater
{
    private readonly NugetUpdater _nugetUpdater;
    private readonly CsProjUpdater _csProjUpdater;
    private readonly CsCodeUpdater _csCodeUpdater;

    private readonly ILogger _logger;
    private readonly IRunProcessHelper _runProcessHelper;
    private readonly UpdateOptions _updateOptions;

    public CSharpUpdater(
        ILogger logger,
        IRunProcessHelper runProcessHelper,
        UpdateOptions updateOptions)
    {
        _logger = logger;
        _runProcessHelper = runProcessHelper;
        _updateOptions = updateOptions;
        _nugetUpdater = new NugetUpdater(_logger, _runProcessHelper);
        _csProjUpdater = new CsProjUpdater(_logger);
        _csCodeUpdater = new CsCodeUpdater(_logger, _runProcessHelper);
    }

    public async ValueTask<ImmutableArray<CSharpUpdateResult>> UpdateAllCSharpProjectsAsync(UpdateWork updateWork)
    {
        if (_updateOptions.CSharpOptions is null)
        {
            _logger.Information("No C# options provided, skipping C# updates");
            return ImmutableArray<CSharpUpdateResult>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpUpdateResult>();
        foreach (var csProjFilePath in updateWork.CsProjectFiles)
        {
            _logger.Information($"Updating '{csProjFilePath}'");

            var csProjUpdates = UpdateCsProjPropertyValues(csProjFilePath, _updateOptions.CSharpOptions);
            var dotnetFormatUpdate = await RunDotnetFormatAsync(csProjFilePath, _updateOptions.CSharpOptions);
            var nugetUpdates = await UpdateNugetPackagesAsync(csProjFilePath, _updateOptions.CSharpOptions);

            builder.Add(new CSharpUpdateResult(
                csProjFilePath,
                NugetUpdates: nugetUpdates,
                LangVersionUpdate: csProjUpdates.LangVersionUpdate,
                TargetFrameworkUpdate: csProjUpdates.TargetFrameworkUpdate,
                DotnetFormatUpdate: dotnetFormatUpdate));
        }

        return builder.ToImmutableArray();
    }

    private async ValueTask<NugetUpdateResults> UpdateNugetPackagesAsync(string csProjFilePath, CSharpOptions cSharpOptions)
    {
        try
        {
            if (cSharpOptions.NugetOptions?.UpdateOptions is null)
            {
                _logger.Information("No NuGet options provided, skipping NuGet updates");
                return new NugetUpdateResults(RetrievedPackageListSuccessfully: true, ImmutableArray<NugetUpdateResult>.Empty);
            }

            return await _nugetUpdater.UpdateNugetPackagesAsync(csProjFilePath, cSharpOptions.NugetOptions.UpdateOptions);
        }
        catch (Exception ex)
        {
            _logger.Error("Error updating nuget packages in csproj file '{CsProjFilePath}'. {Ex}", csProjFilePath, ex.ToString());
            return new NugetUpdateResults(RetrievedPackageListSuccessfully: false, ImmutableArray<NugetUpdateResult>.Empty);
        }
    }

    private CsProjUpdateResult UpdateCsProjPropertyValues(string csProjFilePath, CSharpOptions cSharpOptions)
    {
        try
        {
            return _csProjUpdater.UpdateCsProjPropertyValues(
                csProjFilePath,
                cSharpOptions);
        }
        catch (Exception ex)
        {
            _logger.Error("Error updating csproj file '{CsProjFilePath}'. {Ex}", csProjFilePath, ex.ToString());
            return new CsProjUpdateResult(csProjFilePath, CsprojValueUpdateResultType.Unknown, CsprojValueUpdateResultType.Unknown);
        }
    }

    private async Task<DotnetFormatResult> RunDotnetFormatAsync(string csProjFilePath, CSharpOptions cSharpOptions)
    {
        try
        {
            if (cSharpOptions.CSharpStyleOptions is null)
            { 
                _logger.Information("No C# style options provided, skipping dotnet format");
                return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.DidNotRun);
            }

            return await _csCodeUpdater.RunDotnetFormatAsync(csProjFilePath, cSharpOptions.CSharpStyleOptions);
        }
        catch (Exception ex)
        {
            _logger.Error("Error running dotnet format on csproj file '{CsProjFilePath}'. {Ex}", csProjFilePath, ex.ToString());
            return new DotnetFormatResult(csProjFilePath, DotnetFormatResultType.Erroreded);
        }
    }
}
