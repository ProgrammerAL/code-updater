using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace ProgrammerAL.Tools.CodeUpdater.Options;
public class CommandOptions
{
    [Option(shortName: 'c', longName: "config-file", Required = true, HelpText = "Path to the file to use for config values when updating code")]
    public required string ConfigFile { get; set; }

    [Option(shortName: 'o', longName: "output-file", Required = false, HelpText = "If this is set, it will be the file to write logs to.")]
    public string? OutputFile { get; set; }

    [Option(shortName: 'l', longName: "log-level", Required = false, HelpText = "Level to log. Valid values are: Verbose, Info, Warn, Error. Default value is verbose.")]
    public string LogLevel { get; set; } = "verbose";
}
