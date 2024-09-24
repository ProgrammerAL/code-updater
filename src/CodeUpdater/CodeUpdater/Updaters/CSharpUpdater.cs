using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ProgrammerAL.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;

public class CSharpUpdater
{
    private readonly NugetUpdater _nugetUpdater;
    private readonly CsProjUpdater _csProjUpdater;

    private readonly ILogger _logger;
    private readonly IRunProcessHelper _runProcessHelper;
    private readonly CommandOptions _commandOptions;

    public CSharpUpdater(ILogger logger, IRunProcessHelper runProcessHelper, CommandOptions commandOptions)
    {
        _logger = logger;
        _runProcessHelper = runProcessHelper;
        _commandOptions = commandOptions;
        _nugetUpdater = new NugetUpdater(_logger, _runProcessHelper);
        _csProjUpdater = new CsProjUpdater(_logger, _commandOptions);
    }

    public async ValueTask<ImmutableArray<CSharpUpdateResult>> UpdateAllCSharpProjectsAsync(UpdateWork updateWork)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpUpdateResult>();
        foreach (var csProjFilePath in updateWork.CsProjectFiles)
        {
            _logger.Information($"Updating '{csProjFilePath}'");

            var nugetUpdates = await _nugetUpdater.UpdateNugetPackagesAsync(csProjFilePath);
            var csProjUpdates = _csProjUpdater.UpdateCsProjPropertyValues(csProjFilePath);

            builder.Add(new CSharpUpdateResult(
                csProjFilePath,
                NugetUpdates: nugetUpdates.Updates,
                LangVersionUpdate: csProjUpdates.LangVersionUpdate,
                TargetFrameworkUpdate: csProjUpdates.TargetFrameworkUpdate));
        }

        return builder.ToImmutableArray();
    }
}
