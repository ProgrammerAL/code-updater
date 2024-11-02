
using System.ComponentModel.DataAnnotations;

namespace ProgrammerAL.Tools.CodeUpdater;

public class UpdateOptions
{
    /// <summary>
    /// Options for path to use when updating code
    /// </summary>
    [Required]
    public required UpdatePathOptions UpdatePathOptions { get; set; }

    /// <summary>
    /// Options for updating C# projects and code
    /// </summary>
    public CSharpOptions? CSharpOptions { get; set; }

    /// <summary>
    /// Options for updating Npm packages
    /// </summary>
    public NpmOptions? NpmOptions { get; set; }

    /// <summary>
    /// Options for output logging of the operation
    /// </summary>
    public LoggingOptions? LoggingOptions { get; set; }
}

public class UpdatePathOptions
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

}

public class NpmOptions
{
    /// <summary>
    /// Npm command to \"compile\" the npm directory. Format run is: npm run <NpmBuildCommand>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string NpmBuildCommand { get; set; }
}

public class CSharpOptions
{
    /// <summary>
    /// Versioning options for csproj files
    /// </summary>
    public CsProjVersioningOptions? CsProjVersioningOptions { get; set; }

    /// <summary>
    /// .NET Analyzer settings to set in all csproj files
    /// </summary>
    public CsProjDotNetAnalyzerOptions? CsProjDotNetAnalyzerOptions { get; set; }

    /// <summary>
    /// Options for any code styling updates that will be performed over C# code
    /// </summary>
    public CSharpStyleOptions? CSharpStyleOptions { get; set; }

    /// <summary>
    /// Settings to use for configuring Nuget Audit settings in csproj files.
    /// You can read more at https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
    /// </summary>
    public NugetAuditOptions? NugetAuditOptions { get; set; }

    /// <summary>
    /// Settings to use for updating NuGet packages in csproj files
    /// </summary>
    public NuGetUpdateOptions? NuGetUpdateOptions { get; set; }
}

public class CsProjVersioningOptions
{
    /// <summary>
    /// Target Framework to set in all *.csproj files
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string TargetFramework { get; set; }

    /// <summary>
    /// C# language version to set in all *.csproj files
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string LangVersion { get; set; }

    /// <summary>
    /// The value to set for the TreatWarningsAsErrors flag in all *.csproj files
    /// </summary>
    [Required]
    public required bool TreatWarningsAsErrors { get; set; }
}

public class CsProjDotNetAnalyzerOptions
{
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
}

public class CSharpStyleOptions
{
    /// <summary>
    /// True to run the `dotnet format` command
    /// </summary>
    [Required]
    public required bool RunDotnetFormat { get; set; }
}

public class NugetAuditOptions
{
    /// <summary>
    /// What value to set for the `NuGetAudit` property in the csproj file.
    /// </summary>
    [Required]
    public required bool NuGetAudit { get; set; }

    /// <summary>
    /// What value to set for the `NuGetAuditMode` property in the csproj file. Valid values are `direct` and `all`.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string AuditMode { get; set; }

    /// <summary>
    /// What value to set for the `NuGetAuditLevel` property in the csproj file. Valid values are: `low`, `moderate`, `high`, and `critical`
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string AuditLevel { get; set; }
}

public class NuGetUpdateOptions
{
    /// <summary>
    /// True to updates all referenced nugets to the latest version. These are the references in the csproj files.
    /// </summary>
    [Required]
    public bool UpdateTopLevelNugetsInCsProj { get; set; }

    /// <summary>
    /// True to updates all indirect nugets to the latest version. These are the nugets that are referenced automatically based on SDK chosen or something like that.
    /// </summary>
    [Required]
    public bool UpdateTopLevelNugetsNotInCsProj { get; set; }
}

public class LoggingOptions
{
    /// <summary>
    /// If this is set, it will be the file to write logs to, in addition to the console
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string OutputFile { get; set; }

    /// <summary>
    /// Verbosity level to log. Valid values are: Verbose, Info, Warn, Error. Default value: verbose.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string LogLevel { get; set; } = "verbose";
}
