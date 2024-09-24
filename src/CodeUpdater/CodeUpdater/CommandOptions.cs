using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace ProgrammerAL.CodeUpdater;
public class CommandOptions
{
    [Option(shortName: 'd', longName: "directory", Required = true, HelpText = "Root directory to search within for all projects to update")]
    public string Directory { get; set; } = string.Empty;

    [Option(shortName: 'p', longName: "ignorePatterns", Required = false, HelpText = "String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \\ as the path separator for Windows, and / as the path separator everywhere else. Eg: `\\my-skip-path\\` will ignore all projects that have the text `\\my-skip-path\\` within the full path. Which will only happen on Windows because that uses backslashes for path separators.")]
    public IEnumerable<string> IgnorePatterns { get; set; } = ImmutableArray<string>.Empty;

    [Option(shortName: 'n', longName: "npmBuildCommand", Required = false, HelpText = "Npm command to run to \"compile\" the npm directory. Default value is `publish`. Format run is: npm run <NpmBuildCommand>.")]
    public string NpmBuildCommand { get; set; } = "publish";

    [Option(shortName: 't', longName: "dotnetTargetFramework", Required = false, HelpText = "Target Framework to set in all *.CsProj files. Default value is `net8.0`")]
    public string DotNetTargetFramework { get; set; } = "net8.0";

    [Option(shortName: 'l', longName: "dotnetLangVersion", Required = false, HelpText = "C# language version to set in all *.CsProj files. Default value is `latest`")]
    public string DotNetLangVersion { get; set; } = "latest";
}
