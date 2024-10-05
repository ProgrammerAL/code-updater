using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ProgrammerAL.Tools.CodeUpdater.Helpers;
using ProgrammerAL.Tools.CodeUpdater.Options;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class CSharpUpdater
{
    private readonly NugetUpdater _nugetUpdater;
    private readonly CsProjUpdater _csProjUpdater;

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
        _csProjUpdater = new CsProjUpdater(_logger, updateOptions);
    }

    public async ValueTask<ImmutableArray<CSharpUpdateResult>> UpdateAllCSharpProjectsAsync(UpdateWork updateWork)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpUpdateResult>();
        foreach (var csProjFilePath in updateWork.CsProjectFiles)
        {
            _logger.Information($"Updating '{csProjFilePath}'");


            var nugetUpdates = await UpdateNugetPackagesAsync(csProjFilePath);
            var csProjUpdates = UpdateCsProjPropertyValues(csProjFilePath);

            builder.Add(new CSharpUpdateResult(
                csProjFilePath,
                NugetUpdates: nugetUpdates,
                LangVersionUpdate: csProjUpdates.LangVersionUpdate,
                TargetFrameworkUpdate: csProjUpdates.TargetFrameworkUpdate));
        }

        return builder.ToImmutableArray();
    }

    private async ValueTask<NugetUpdateResults> UpdateNugetPackagesAsync(string csProjFilePath)
    {
        try
        {
            return await _nugetUpdater.UpdateNugetPackagesAsync(csProjFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error("Error updating nuget packages in csproj file '{CsProjFilePath}'. {Ex}", csProjFilePath, ex.ToString());
            return new NugetUpdateResults(RetrievedPackageListSuccessfully: false, ImmutableArray<NugetUpdateResult>.Empty);
        }
    }

    private CsProjUpdateResult UpdateCsProjPropertyValues(string csProjFilePath)
    {
        try
        {
            return _csProjUpdater.UpdateCsProjPropertyValues(csProjFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error("Error updating csproj file '{CsProjFilePath}'. {Ex}", csProjFilePath, ex.ToString());
            return new CsProjUpdateResult(csProjFilePath, CsprojValueUpdateResultType.Unknown, CsprojValueUpdateResultType.Unknown);
        }
    }
}
