using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;
public class NpmUpdater(IRunProcessHelper RunProcessHelper)
{
    public NpmUpdates UpdateNpmPackages(UpdateWork updateWork)
    {
        foreach (var projectPath in updateWork.NpmDirectories)
        {
            RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, "npm-check-updates --upgrade");
            RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, "npm install --legacy-peer-deps");
            RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, "npm audit fix --force");
        }

        return new NpmUpdates(updateWork.NpmDirectories);
    }
}
