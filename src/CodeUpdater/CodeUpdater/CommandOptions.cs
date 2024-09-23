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
    [Option(Required = true, HelpText = "Root directory to search within for all projects to update")]
    public string RootDirectory { get; set; } = string.Empty;

    [Option(Required = false, HelpText = "String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \\ as the path separator for Windows, and / as the path separator everywhere else. Eg: \"\\my-skip-path\\\" will ignore all projects that have the text \"\\my-skip-path\\\" within the full path. Which will only happen on Windows because that uses backslashes for path separators.")]
    public IEnumerable<string> IgnorePatterns { get; set; } = ImmutableArray<string>.Empty;
}
