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
    [Option(shortName: 'o', longName: "options", Required = true, HelpText = "Path to the file to use for values when updating code")]
    public required string OptionsFile { get; set; }
}
