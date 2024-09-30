using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace ProgrammerAL.CodeUpdater.Options;
public class CommandOptions
{
    [Option(shortName: 'c', longName: "config-file", Required = true, HelpText = "Path to the file to use for config values when updating code")]
    public required string ConfigFile { get; set; }
}
