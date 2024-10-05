
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAL.Tools.CodeUpdater;
public class UpdateOptions
{
    /// <summary>
    /// Root directory to run from.
    /// Code Updater will search all child directories within this for projects to update.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string RootDirectory { get; set; }

    /// <summary>
    /// String to ignore within file paths when looking for projects to update. 
    /// This is OS sensitive, so use \\ as the path separator for Windows, and / as the path separator everywhere else. 
    /// Eg: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. 
    /// Which will only happen on Windows because that uses backslashes for path separators.
    /// </summary>
    [Required]
    public required IEnumerable<string> IgnorePatterns { get; set; }

    /// <summary>
    /// Npm command to run to \"compile\" the npm directory. Format run is: npm run <NpmBuildCommand>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string NpmBuildCommand { get; set; }

    /// <summary>
    /// Target Framework to set in all *.CsProj files
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string DotNetTargetFramework { get; set; }

    /// <summary>
    /// C# language version to set in all *.CsProj files
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string DotNetLangVersion { get; set; }

    /// <summary>
    /// True to set the `EnableNetAnalyzers` csproj value to true, false to set it to false
    /// </summary>
    [Required]
    public required bool EnableNetAnalyzers { get; set; }

    /// <summary>
    /// True to set the `EnforceCodeStyleInBuild` csproj value to true, false to set it to false
    /// </summary>
    [Required]
    public required bool EnforceCodeStyleInBuild { get; set; }

    /// <summary>
    /// If this is set, it will be the file to write logs to, in addition to the console
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Verbosity level to log. Valid values are: Verbose, Info, Warn, Error. Default value: verbose.
    /// </summary>
    public string? LogLevel { get; set; } = "verbose";
}
