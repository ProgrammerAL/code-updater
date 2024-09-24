using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;

public class CSharpUpdater
{
    private readonly NugetUpdater _nugetUpdater;
    private readonly CsProjUpdater _csProjUpdater;

    private readonly ILogger _logger;

    public CSharpUpdater(ILogger logger)
    {
        _logger = logger;
        _nugetUpdater = new NugetUpdater(_logger);
        _csProjUpdater = new CsProjUpdater(_logger);
    }

    public void UpdateAllCSharpProjects(UpdateWork updateWork)
    {
        foreach (var csProjFilePath in updateWork.CsProjectFiles)
        {
            _logger.Information($"Updating '{csProjFilePath}'");

            _nugetUpdater.UpdateNugetPackages(csProjFilePath);
            _csProjUpdater.UpdateCsProjPropertyValues(csProjFilePath);
        }
    }
}
