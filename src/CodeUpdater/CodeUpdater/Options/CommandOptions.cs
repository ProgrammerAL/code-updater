using CommandLine;

namespace ProgrammerAL.Tools.CodeUpdater.Options;
public class CommandOptions
{
    [Option(shortName: 'o', longName: "options", Required = true, HelpText = "Path to the file to use for values when updating code")]
    public required string OptionsFile { get; set; }
}
