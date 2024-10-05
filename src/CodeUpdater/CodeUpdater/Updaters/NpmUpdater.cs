using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ProgrammerAL.Tools.CodeUpdater.Helpers;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;
public class NpmUpdater(ILogger Logger, IRunProcessHelper RunProcessHelper)
{
    public NpmUpdates UpdateNpmPackages(UpdateWork updateWork)
    {
        foreach (var projectPath in updateWork.NpmDirectories)
        {
            string command = "";
            try
            {
                command = "npm-check-updates --upgrade";
                RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, command);

                command = "npm install --legacy-peer-deps";
                RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, command);

                command = "npm audit fix --force";
                RunProcessHelper.RunProwerShellCommandToCompletion(projectPath, command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating npm packages at path'{ProjectPath}'. Command was '{Command}'", projectPath, command);
            }
        }

        return new NpmUpdates(updateWork.NpmDirectories);
    }
}
