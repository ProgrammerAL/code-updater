using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;

public class CsProjUpdater(ILogger Logger, UpdateOptions UpdateOptions)
{
    public CsProjUpdateResult UpdateCsProjPropertyValues(string csProjFilePath)
    {
        var csProjXmlDoc = XDocument.Load(csProjFilePath, LoadOptions.PreserveWhitespace);

        var propertyGroups = csProjXmlDoc.Descendants("PropertyGroup").ToList();

        var targetFrameworkUpdates = new CsprojUpdateTracker(
            "TargetFramework",
            UpdateOptions.DotNetTargetFramework,
            skipStartsWithValues: ["netstandard"]);//Project is set to .NET Standard are there for a reason, don't change it
        var langUpdates = new CsprojUpdateTracker(
            "LangVersion",
            UpdateOptions.DotNetLangVersion);
        var enableNETAnalyzersUpdates = new CsprojUpdateTracker(
            "EnableNETAnalyzers",
            UpdateOptions.EnableNetAnalyzers.ToString().ToLower());
        var enforceCodeStyleInBuildUpdates = new CsprojUpdateTracker(
            "EnforceCodeStyleInBuild",
            UpdateOptions.EnforceCodeStyleInBuild.ToString().ToLower());

        UpdateOrAddCsProjValues(
            csProjXmlDoc,
            propertyGroups,
            targetFrameworkUpdates,
            langUpdates,
            enableNETAnalyzersUpdates,
            enforceCodeStyleInBuildUpdates);

        //Write the file back out
        //Note: Use File.WriteAllText instead of Save() because calling XDocument.ToString() doesn't include the xml header
        File.WriteAllText(csProjFilePath, csProjXmlDoc.ToString(), Encoding.UTF8);

        var langVersionUpdateType = langUpdates.GetFinalResult();
        var targetFrameworkUpdate = targetFrameworkUpdates.GetFinalResult();
        return new CsProjUpdateResult(csProjFilePath, langVersionUpdateType, targetFrameworkUpdate);
    }

    private void UpdateOrAddCsProjValues(XDocument csProjXmlDoc, List<XElement> propertyGroupsElements, params CsprojUpdateTracker[] updates)
    {
        foreach (var propertyGroupElement in propertyGroupsElements)
        {
            foreach (var update in updates)
            {
                UpdateCsprojValue(propertyGroupElement, update);
            }
        }

        AddMissingElements(csProjXmlDoc, updates);
    }

    private void AddMissingElements(XDocument csProjXmlDoc, params CsprojUpdateTracker[] updates)
    {
        var updatesToMake = new List<CsprojUpdateTracker>();
        foreach (var update in updates)
        {
            if (update.ShouldAddElement())
            {
                updatesToMake.Add(update);
            }
        }

        if (updatesToMake.Any())
        {
            var propertyGroup = csProjXmlDoc.Descendants("PropertyGroup").FirstOrDefault();
            if (propertyGroup is null)
            {
                Logger.Information("Adding new PropertyGroup element for other required elements");
                var newPropertyGroup = new XElement("PropertyGroup");
                csProjXmlDoc.Add(newPropertyGroup);

                propertyGroup = newPropertyGroup;
            }

            foreach (var elementToAdd in updatesToMake)
            {
                Logger.Information($"Adding {elementToAdd.ElementName} element to csproj");
                var newElement = new XElement(elementToAdd.ElementName, elementToAdd.NewValue);
                propertyGroup.Add(newElement);

                elementToAdd.SetResults.Add(CsprojValueUpdateResultType.AddedElement);
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

    public class CsprojUpdateTracker
    {
        public CsprojUpdateTracker(string elementName, string newValue)
            : this(elementName, newValue, ImmutableArray<string>.Empty)
        {
        }

        public CsprojUpdateTracker(string elementName, string newValue, ImmutableArray<string> skipStartsWithValues)
        {
            ElementName = elementName;
            NewValue = newValue;
            SkipStartsWithValues = skipStartsWithValues;
            SetResults = new List<CsprojValueUpdateResultType>();
        }

        public string ElementName { get; }
        public string NewValue { get; }
        public ImmutableArray<string> SkipStartsWithValues { get; }

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

        public bool ShouldAddElement()
        {
            return SetResults.All(x => x == CsprojValueUpdateResultType.NotFound);
        }
    }
}
