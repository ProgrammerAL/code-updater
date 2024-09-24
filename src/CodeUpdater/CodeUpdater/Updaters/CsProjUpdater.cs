using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;

public class CsProjUpdater(ILogger Logger, CommandOptions CommandOptions)
{
    public CsProjUpdateResult UpdateCsProjPropertyValues(string csProjFilePath)
    {
        var csProjXmlDoc = XDocument.Load(csProjFilePath, LoadOptions.PreserveWhitespace);

        var propertyGroups = csProjXmlDoc.Descendants("PropertyGroup").ToList();

        TargetFrameworkUpdateType targetFrameworkUpdate = TargetFrameworkUpdateType.NotFound;
        LangVersionUpdateType langVersionUpdateType;

        var langUpdateValues = new List<LangVersionUpdateType>();
        foreach (var propertyGroupElement in propertyGroups)
        {
            var thisTargetFrameworkUpdateType = UpdateTargetFramework(propertyGroupElement);
            if (thisTargetFrameworkUpdateType != TargetFrameworkUpdateType.NotFound)
            {
                targetFrameworkUpdate = thisTargetFrameworkUpdateType;
            }

            var updateLangResult = UpdateLangVersion(propertyGroupElement);
            langUpdateValues.Add(updateLangResult);
        }

        //If no LangVersion elements were found, add one
        if (langUpdateValues.All(x => x == LangVersionUpdateType.NotFound))
        {
            langVersionUpdateType = LangVersionUpdateType.AddedElement;
            AddLangVersionElement(CommandOptions.DotNetLangVersion, csProjXmlDoc);
        }
        else
        {
            langVersionUpdateType = langUpdateValues.First(x => x != LangVersionUpdateType.NotFound);
        }

        //Write the file back out
        //Note: Use File.WriteAllText instead of Save() because calling XDocument.ToString() doesn't include the xml header
        File.WriteAllText(csProjFilePath, csProjXmlDoc.ToString(), Encoding.UTF8);

        return new CsProjUpdateResult(csProjFilePath, langVersionUpdateType, targetFrameworkUpdate);
    }

    private TargetFrameworkUpdateType UpdateTargetFramework(XElement childElm)
    {
        var targetFrameworkElm = childElm.Element("TargetFramework");
        if (string.IsNullOrEmpty(targetFrameworkElm?.Value))
        {
            return TargetFrameworkUpdateType.NotFound;
        }

        if (string.Equals(targetFrameworkElm.Value, CommandOptions.DotNetTargetFramework))
        {
            return TargetFrameworkUpdateType.AlreadyHasCorrectValue;
        }

        Logger.Information($"Updating target framework from '{targetFrameworkElm.Value}' to '{CommandOptions.DotNetTargetFramework}'");
        targetFrameworkElm.Value = CommandOptions.DotNetTargetFramework;

        return TargetFrameworkUpdateType.Updated;
    }

    private LangVersionUpdateType UpdateLangVersion(XElement childElm)
    {
        var langVersionElm = childElm.Element("LangVersion");
        if (langVersionElm is null)
        {
            return LangVersionUpdateType.NotFound;
        }

        if (string.Equals(langVersionElm.Value, CommandOptions.DotNetLangVersion))
        {
            return LangVersionUpdateType.AlreadyHasCorrectValue;
        }

        Logger.Information($"Updating language version from '{langVersionElm.Value}' to '{CommandOptions.DotNetLangVersion}'");
        langVersionElm.Value = CommandOptions.DotNetLangVersion;
        return LangVersionUpdateType.Updated;
    }

    private void AddLangVersionElement(string LangVersionValue, XDocument csProjXmlDoc)
    {
        var existingPropertyGroup = csProjXmlDoc.Descendants("PropertyGroup").FirstOrDefault();
        if (existingPropertyGroup is object)
        {
            Logger.Information("Adding LangVersion element to first PropertyGroup element");
            var newElement = new XElement("LangVersion", LangVersionValue);
            existingPropertyGroup.Add(newElement);
        }
        else
        {
            Logger.Information("Adding LangVersion element to a new PropertyGroup element");
            var newPropertyGroup = new XElement("PropertyGroup",
                                    new XElement("LangVersion", LangVersionValue));
            csProjXmlDoc.Add(newPropertyGroup);
        }
    }
}
