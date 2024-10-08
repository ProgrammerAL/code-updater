using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Serilog;

namespace ProgrammerAL.Tools.CodeUpdater.Updaters;

public class CsProjUpdater(ILogger Logger, UpdateOptions UpdateOptions)
{
    public CsProjUpdateResult UpdateCsProjPropertyValues(string csProjFilePath)
    {
        var csProjXmlDoc = XDocument.Load(csProjFilePath, LoadOptions.PreserveWhitespace);

        var propertyGroups = csProjXmlDoc.Descendants("PropertyGroup").ToList();

        var targetFrameworkUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.TargetFramework,
            UpdateOptions.DotNetTargetFramework,
            addIfElementNotFound: false,
            skipStartsWithValues: ["netstandard"]);//Project is set to .NET Standard are there for a reason, don't change it
        var langUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.LangVersion,
            UpdateOptions.DotNetLangVersion,
            addIfElementNotFound: true);
        var enableNETAnalyzersUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.EnableNETAnalyzers,
            UpdateOptions.EnableNetAnalyzers.ToString().ToLower(),
            addIfElementNotFound: true);
        var enforceCodeStyleInBuildUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.EnforceCodeStyleInBuild,
            UpdateOptions.EnforceCodeStyleInBuild.ToString().ToLower(),
            addIfElementNotFound: true);
        var nuGetAuditUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.NuGetAudit,
            UpdateOptions.NugetAudit.NuGetAudit.ToString().ToLower(),
            addIfElementNotFound: true);
        var nugetAuditModeUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.NuGetAuditMode,
            UpdateOptions.NugetAudit.AuditMode.ToString().ToLower(),
            addIfElementNotFound: true);
        var nugetAuditLevelUpdates = new CsprojUpdateTracker(
            CsprojUpdateTracker.NuGetAuditLevel,
            UpdateOptions.NugetAudit.AuditLevel,
            addIfElementNotFound: true);

        UpdateOrAddCsProjValues(
            csProjXmlDoc,
            propertyGroups,
            new CsprojUpdateGroupTracker(CsprojUpdateGroupTracker.NotFoundActionType.DoNothing,
            [
                targetFrameworkUpdates,
            ]),
            new CsprojUpdateGroupTracker(CsprojUpdateGroupTracker.NotFoundActionType.AddElementToFirstPropertyGroup,
            [
                langUpdates,
                enableNETAnalyzersUpdates,
                enforceCodeStyleInBuildUpdates,
            ]),
            new CsprojUpdateGroupTracker(CsprojUpdateGroupTracker.NotFoundActionType.AddElementToNewPropertyGroup,
            [
                nuGetAuditUpdates,
                nugetAuditModeUpdates,
                nugetAuditLevelUpdates
            ]));

        //Write the file back out
        //Note: Use File.WriteAllText instead of Save() because calling XDocument.ToString() doesn't include the xml header
        File.WriteAllText(csProjFilePath, csProjXmlDoc.ToString(), Encoding.UTF8);

        var langVersionUpdateType = langUpdates.GetFinalResult();
        var targetFrameworkUpdate = targetFrameworkUpdates.GetFinalResult();
        return new CsProjUpdateResult(csProjFilePath, langVersionUpdateType, targetFrameworkUpdate);
    }

    private void UpdateOrAddCsProjValues(XDocument csProjXmlDoc, List<XElement> propertyGroupsElements, params CsprojUpdateGroupTracker[] updateGroups)
    {
        //Separate updates into groups
        //  This way, when the groups are added to the csproj, they are grouped together to the same PropetyGroup
        //  Not important for functional reasons, but it makes the csproj file easier to read
        foreach (var group in updateGroups)
        {
            foreach (var propertyGroupElement in propertyGroupsElements)
            {
                foreach (var update in group.UpdateTrackers)
                {
                    if (update.ElementName == CsprojUpdateTracker.TargetFramework)
                    {
                        UpdateTargetFrameworkValue(propertyGroupElement, update);
                    }
                    else
                    {
                        UpdateCsprojValue(propertyGroupElement, update);
                    }
                }
            }

            AddMissingElements(csProjXmlDoc, group);
        }
    }

    private void AddMissingElements(XDocument csProjXmlDoc, CsprojUpdateGroupTracker updateGroup)
    {
        XElement? propertyGroupToAddTo = null;
        if (updateGroup.NotFoundAction == CsprojUpdateGroupTracker.NotFoundActionType.AddElementToFirstPropertyGroup)
        {
            propertyGroupToAddTo = csProjXmlDoc.Descendants("PropertyGroup").FirstOrDefault();
        }
        else if (updateGroup.NotFoundAction == CsprojUpdateGroupTracker.NotFoundActionType.AddElementToNewPropertyGroup)
        {
            Logger.Information("Adding new PropertyGroup element for other required elements");
            var newPropertyGroup = new XElement("PropertyGroup");
            csProjXmlDoc.Root!.Add(newPropertyGroup);

            propertyGroupToAddTo = newPropertyGroup;
        }

        //Add the trackers that haven't already set the final value
        var toUpdate = updateGroup.UpdateTrackers.Where(x => !x.HasMadeRequiredUpdate()).ToImmutableArray();
        if (propertyGroupToAddTo is not null
            && toUpdate.Any())
        {
            foreach (var trackers in toUpdate)
            {
                Logger.Information($"Adding {trackers.ElementName} element to csproj");
                var newElement = new XElement(trackers.ElementName, trackers.NewValue);
                propertyGroupToAddTo.Add(newElement);

                trackers.SetResults.Add(CsprojValueUpdateResultType.AddedElement);
            }
        }
    }

    private void UpdateCsprojValue(XElement childElm, CsprojUpdateTracker updateTracker)
    {
        var element = childElm.Element(updateTracker.ElementName);
        if (string.IsNullOrEmpty(element?.Value))
        {
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.NotFound);
        }
        else if (string.Equals(element.Value, updateTracker.NewValue))
        {
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.AlreadyHasCorrectValue);
        }
        else if (updateTracker.SkipStartsWithValues.Any(x => element.Value.StartsWith(x)))
        {
            Logger.Information($"Skipping {updateTracker.ElementName} update because value '{element.Value}' starts with one of these: {string.Join(", ", updateTracker.SkipStartsWithValues)}");
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.HasSkipValue);
        }
        else
        {
            Logger.Information($"Updating {updateTracker.ElementName} from '{element.Value}' to '{updateTracker.NewValue}'");
            element.Value = updateTracker.NewValue;

            updateTracker.SetResults.Add(CsprojValueUpdateResultType.Updated);
        }
    }

    private void UpdateTargetFrameworkValue(XElement childElm, CsprojUpdateTracker updateTracker)
    {
        var element = childElm.Element(CsprojUpdateTracker.TargetFramework);
        if (string.IsNullOrEmpty(element?.Value))
        {
            //If there's no TargetFramework, maybe the project has a TargetFrameworks element
            UpdateTargetFrameworksValue(childElm, updateTracker);
        }
        else if (string.Equals(element.Value, updateTracker.NewValue))
        {
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.AlreadyHasCorrectValue);
        }
        else if (updateTracker.SkipStartsWithValues.Any(x => element.Value.StartsWith(x)))
        {
            Logger.Information($"Skipping {updateTracker.ElementName} update because value '{element.Value}' starts with one of these: {string.Join(", ", updateTracker.SkipStartsWithValues)}");
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.HasSkipValue);
        }
        else
        {
            Logger.Information($"Updating {updateTracker.ElementName} from '{element.Value}' to '{updateTracker.NewValue}'");
            element.Value = updateTracker.NewValue;

            updateTracker.SetResults.Add(CsprojValueUpdateResultType.Updated);
        }
    }

    private void UpdateTargetFrameworksValue(XElement childElm, CsprojUpdateTracker updateTracker)
    {
        //TargetFrameworks can appear multiple times in a csproj file, so we need to update all of them
        var elements = childElm.Elements(CsprojUpdateTracker.TargetFrameworks);
        if (!elements.Any())
        {
            updateTracker.SetResults.Add(CsprojValueUpdateResultType.NotFound);
            return;
        }

        foreach (var element in elements)
        {
            //TargetFrameworks are a string like "net8.0-android;net8.0-ios;"
            //We want to update the first part of the string, so we need to split on the dash
            var newElementString = "";
            var existingValues = element.Value.Split(";");
            foreach (var existingValue in existingValues)
            {
                var split = existingValue.Split("-");
                if (split.Length == 2)
                {
                    newElementString += $"{updateTracker.NewValue}-{split[1]};";
                }
                else
                {
                    //As a just in case, just add the existing value and don't make any changes
                    newElementString += $"{existingValue};";
                }
            }

            newElementString = newElementString.TrimEnd(';');//Remove the last semicolon

            Logger.Information($"Updating {updateTracker.ElementName} from '{element.Value}' to '{newElementString}'");
            element.Value = newElementString;

            updateTracker.SetResults.Add(CsprojValueUpdateResultType.Updated);
        }
    }

    private record CsprojUpdateGroupTracker(
        CsprojUpdateGroupTracker.NotFoundActionType NotFoundAction,
        IEnumerable<CsprojUpdateTracker> UpdateTrackers)
    {
        public enum NotFoundActionType
        {
            DoNothing,
            AddElementToFirstPropertyGroup,
            AddElementToNewPropertyGroup
        }
    }

    private class CsprojUpdateTracker
    {
        public const string TargetFramework = "TargetFramework";
        public const string TargetFrameworks = "TargetFrameworks";
        public const string LangVersion = "LangVersion";
        public const string EnableNETAnalyzers = "EnableNETAnalyzers";
        public const string EnforceCodeStyleInBuild = "EnforceCodeStyleInBuild";
        public const string NuGetAudit = "NuGetAudit";
        public const string NuGetAuditMode = "NuGetAuditMode";
        public const string NuGetAuditLevel = "NuGetAuditLevel";

        public CsprojUpdateTracker(string elementName, string newValue, bool addIfElementNotFound)
            : this(elementName, newValue, addIfElementNotFound, ImmutableArray<string>.Empty)
        {
        }

        public CsprojUpdateTracker(string elementName, string newValue, bool addIfElementNotFound, ImmutableArray<string> skipStartsWithValues)
        {
            ElementName = elementName;
            NewValue = newValue;
            SkipStartsWithValues = skipStartsWithValues;
            AddIfElementNotFound = addIfElementNotFound;
            SetResults = new List<CsprojValueUpdateResultType>();
        }

        public string ElementName { get; }
        public string NewValue { get; }
        public ImmutableArray<string> SkipStartsWithValues { get; }
        public bool AddIfElementNotFound { get; }

        public List<CsprojValueUpdateResultType> SetResults { get; }

        public CsprojValueUpdateResultType GetFinalResult()
        {
            if (SetResults.Count == 0)
            {
                return CsprojValueUpdateResultType.Unknown;
            }

            var firstNotNotFound = SetResults.FirstOrDefault(x => x != CsprojValueUpdateResultType.NotFound);
            if (firstNotNotFound != CsprojValueUpdateResultType.Unknown)
            {
                return firstNotNotFound;
            }

            return CsprojValueUpdateResultType.NotFound;
        }

        public bool HasMadeRequiredUpdate()
        {
            return SetResults.Any(x => x != CsprojValueUpdateResultType.NotFound
                                    && x != CsprojValueUpdateResultType.Unknown);
        }
    }
}
