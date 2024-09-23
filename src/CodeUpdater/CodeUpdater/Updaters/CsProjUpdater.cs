using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Serilog;

namespace ProgrammerAL.CodeUpdater.Updaters;

public class CsProjUpdater(ILogger Logger)
{
    public const string TargetFrameworkValue = "net8.0";
    public const string LangVersionValue = "latest";

    public void UpdateCsProjPropertyValues(string csProjFilePath)
    {
        var csProjXmlDoc = XDocument.Load(csProjFilePath, LoadOptions.PreserveWhitespace);

        var propertyGroups = csProjXmlDoc.Descendants("PropertyGroup").ToList();

        var langUpdateValues = new List<LangVersionUpdateType>();
        foreach (var propertyGroupElement in propertyGroups)
        {
            UpdateTargetFramework(propertyGroupElement);
            var updateLangResult = UpdateLangVersion(propertyGroupElement);
            langUpdateValues.Add(updateLangResult);
        }

        //If no LangVersion elements were found, add one
        if (langUpdateValues.All(x => x == LangVersionUpdateType.NotFound))
        {
            AddLangVersion(LangVersionValue, csProjXmlDoc);
        }

        //Write the file back out
        //Note: Use File.WriteAllText instead of Save() because calling XDocument.ToString() doesn't include the xml header
        File.WriteAllText(csProjFilePath, csProjXmlDoc.ToString(), Encoding.UTF8);
    }

    private void UpdateTargetFramework(XElement childElm)
    {
        var targetFrameworkElm = childElm.Element("TargetFramework");
        if (!string.IsNullOrEmpty(targetFrameworkElm?.Value)
            && !string.Equals(targetFrameworkElm.Value, TargetFrameworkValue))
        {
            Logger.Information($"Updating target framework from '{targetFrameworkElm.Value}' to '{TargetFrameworkValue}'");
            targetFrameworkElm.Value = TargetFrameworkValue;
        }
    }

    private LangVersionUpdateType UpdateLangVersion(XElement childElm)
    {
        var langVersionElm = childElm.Element("LangVersion");
        if (langVersionElm is object)
        {
            if (string.Equals(langVersionElm.Value, LangVersionValue))
            {
                return LangVersionUpdateType.AlreadyHasCorrectValue;
            }

            Logger.Information($"Updating language version from '{langVersionElm.Value}' to '{LangVersionValue}'");
            langVersionElm.Value = LangVersionValue;
            return LangVersionUpdateType.Updated;
        }

        return LangVersionUpdateType.NotFound;
    }

    private void AddLangVersion(string LangVersionValue, XDocument csProjXmlDoc)
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

    public enum LangVersionUpdateType
    {
        Updated,
        AlreadyHasCorrectValue,
        NotFound
    }
}
