using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ProgrammerAL.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;
public class NpmUpdater(IRunProcessHelper RunProcessHelper)
{
    public NpmUpdates UpdateNpmPackages(UpdateWork updateWork)
    {
        foreach (var projectPath in updateWork.NpmDirectories)
        {
            RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, "npm-check-updates --upgrade");
        }

        return new NpmUpdates(updateWork.NpmDirectories);
    }
}
