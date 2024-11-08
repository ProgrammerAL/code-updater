using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class RegexSearcher(ILogger Logger, UpdateOptions UpdateOptions)
{
    public RegexSearchResults SearchUpdatableFiles(UpdateWork updateWork)
    {
        if (UpdateOptions.RegexSearchOptions is null)
        {
            Logger.Information("No RegexSearchOptions config set, will not search files text");
            return new RegexSearchResults(ImmutableArray<RegexSearchResult>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<RegexSearchResult>();
        var allFiles = updateWork.ValidDirectories.SelectMany(x => Directory.GetFiles(x, "*", SearchOption.TopDirectoryOnly));
        foreach (var filePath in allFiles)
        {
            try
            {
                var fileText = File.ReadAllText(filePath);
                foreach (var search in UpdateOptions.RegexSearchOptions.Searches)
                {
                    var matches = Regex.Matches(fileText, search.SearchRegex);
                    if (matches.Any())
                    {
                        var matchedStrings = matches.Select(x => x.Value).ToImmutableArray();
                        builder.Add(new RegexSearchResult(search.Description, filePath, matchedStrings));
                    }
                }
            }
            catch (IOException)
            { 
                Logger.Error($"Could not read file '{filePath}' because of an IO Exception. It may be opened by something else. Skipping...");
            }
        }

        return new RegexSearchResults(builder.ToImmutableArray());
    }
}
