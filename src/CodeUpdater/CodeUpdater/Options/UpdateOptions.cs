﻿
using System.ComponentModel.DataAnnotations;

namespace ProgrammerAL.Tools.CodeUpdater;

public class UpdateOptions
{
    /// <summary>
    /// This object determines what will be updated based on file paths
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
    /// Regex to search for specific string. Handy for finding things you need to manually update, that this tool can't easily do. For example, use this to search for the hard coded .NET version in a YAML file for a CI/CD Pipeline so you know where to manually update it
    /// </summary>
    public RegexSearchOptions? RegexSearchOptions { get; set; }

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
    /// Options for compiling Npm packages after updates. Note if this is not set, but the parent NpmOptions is set, NPM Packages will be updated but not tested with a compile.
    /// </summary>
    public NpmCompileOptions? CompileOptions { get; set; }
}

public class NpmCompileOptions
{
    /// <summary>
    /// Npm command to \"compile\" the npm directory. The CLI command that will be run is: npm run {{NpmBuildCommand}}
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string BuildCommand { get; set; }
}

public class CSharpOptions
{
    /// <summary>
    /// Versioning options for csproj files
    /// </summary>
    public CsProjVersioningOptions? CsProjVersioningOptions { get; set; }

    /// <summary>
    /// .NET First Party Analyzer settings to set in all `*.csproj` files. You can read more at https://learn.microsoft.com/en-us/visualstudio/code-quality/install-net-analyzers?view=vs-2022
    /// </summary>
    public CsProjDotNetAnalyzerOptions? CsProjDotNetAnalyzerOptions { get; set; }

    /// <summary>
    /// Options for any code styling updates that will be performed over C# code
    /// </summary>
    public CSharpStyleOptions? CSharpStyleOptions { get; set; }

    /// <summary>
    /// Options for updating Nuget packages in csproj files
    /// </summary>
    public NugetOptions? NugetOptions { get; set; }
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

public class NugetOptions
{
    /// <summary>
    /// Settings to use for configuring Nuget Audit settings in csproj files.
    /// You can read more at https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
    /// </summary>
    public NugetAuditOptions? AuditOptions { get; set; }

    /// <summary>
    /// Settings to use for updating NuGet packages in csproj files
    /// </summary>
    public NuGetUpdateOptions? UpdateOptions { get; set; }
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
    public string? OutputFile { get; set; }

    /// <summary>
    /// Verbosity level to log. Valid values are: Verbose, Info, Warn, Error. Default value: verbose.
    /// </summary>
    public string LogLevel { get; set; } = "verbose";
}

public class RegexSearchOptions
{
    /// <summary>
    /// Collection of searches to make in all files that are not ignored
    /// </summary>
    [Required]
    public required IEnumerable<StringSearch> Searches { get; set; }

    public class StringSearch
    {
        /// <summary>
        /// Regex to search for in all files that are not ignored
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string SearchRegex { get; set; }

        /// <summary>
        /// Description to show in the output
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string Description { get; set; }
    }
}
