using System;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Restore;
using System.Threading;
using System.Diagnostics;
using Cake.Common.Tools.DotNet.Test;
using Cake.Common.Tools.DotNet.Build;
using System.IO;
using System.Xml.Linq;
using System.Linq;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public const string BuildConfiguration = "Release";

    public string Target { get; }
    public string SrcDirectoryPath { get; }
    public string NugetVersion { get; }
    public bool PushNuget { get; }
    public string NuGetPushToken { get; }
    public ProjectPaths ProjectPaths { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Target = context.Argument("target", "Default");
        SrcDirectoryPath = LoadParameter(context, "srcDirectoryPath");
        NugetVersion = LoadParameter(context, "nugetVersion");
        NuGetPushToken = LoadParameter(context, "nuGetPushToken");
        PushNuget = context.Argument<bool>("pushNuget", false);

        ProjectPaths = ProjectPaths.LoadFromContext(context, BuildConfiguration, SrcDirectoryPath, NugetVersion);
    }

    private string LoadParameter(ICakeContext context, string parameterName)
    {
        return context.Arguments.GetArgument(parameterName) ?? throw new Exception($"Missing parameter '{parameterName}'");
    }
}

[TaskName(nameof(OutputParametersTask))]
public sealed class OutputParametersTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information($"INFO: Current Working Directory: {context.Environment.WorkingDirectory}");

        context.Log.Information($"INFO: {nameof(context.SrcDirectoryPath)}: {context.SrcDirectoryPath}");
        context.Log.Information($"INFO: {nameof(context.ProjectPaths)}.{nameof(context.ProjectPaths.ProjectName)}: {context.ProjectPaths.ProjectName}");
    }
}

[IsDependentOn(typeof(OutputParametersTask))]
[TaskName(nameof(UpdateCsProjVersionTask))]
public sealed class UpdateCsProjVersionTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var filePath = context.ProjectPaths.CsprojFile;
        var xml = XElement.Load(filePath);
        var versionElements = xml
            .Elements("PropertyGroup")
            .SelectMany(x => x.Elements())
            .Where(x => string.Equals(x.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase));
        foreach (var versionElement in versionElements)
        {
            versionElement.Value = context.NugetVersion;
        }

        var csprojString = xml.ToString();
        context.Log.Information($"Writing csproj file to '{filePath}' with content: {Environment.NewLine}{csprojString}");
        File.WriteAllText(filePath, csprojString);
    }
}

[IsDependentOn(typeof(UpdateCsProjVersionTask))]
[TaskName(nameof(BuildTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectory(context.ProjectPaths.OutDir);

        BuildDotnetApp(context, context.ProjectPaths.PathToSln);
        //No unit tests yet
        //TestDotnetApp(context, context.ProjectPaths.UnitTestProj);
        PackNugetPackage(context, context.ProjectPaths.OutDir, context.ProjectPaths.CsprojFile);
    }

    private void BuildDotnetApp(BuildContext context, string pathToSln)
    {
        context.DotNetRestore(pathToSln, new DotNetRestoreSettings { });

        context.DotNetBuild(pathToSln, new DotNetBuildSettings
        {
            NoRestore = true,
            Configuration = BuildContext.BuildConfiguration
        });
    }

    private void TestDotnetApp(BuildContext context, string pathToUnitTestProj)
    {
        var testSettings = new DotNetTestSettings()
        {
            Configuration = BuildContext.BuildConfiguration,
            NoBuild = true,
            ArgumentCustomization = (args) => args.Append("/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura --logger trx")
        };

        context.DotNetTest(pathToUnitTestProj, testSettings);
    }

    private void PackNugetPackage(BuildContext context, string outDir, string csprojFile)
    {
        context.DotNetPack(csprojFile, new Cake.Common.Tools.DotNet.Pack.DotNetPackSettings
        {
            IncludeSymbols = false,
            IncludeSource = false,
            NoBuild = true,
            Configuration = BuildContext.BuildConfiguration,
            OutputDirectory = outDir,
            VersionSuffix = context.NugetVersion,
            ArgumentCustomization = (args) => args.Append($"-p:PackageVersion={context.NugetVersion}")
        });
    }
}

[IsDependentOn(typeof(BuildTask))]
[TaskName(nameof(NugetPushTask))]
public sealed class NugetPushTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (!context.PushNuget)
        {
            return;
        }

        context.DotNetNuGetPush(context.ProjectPaths.NuGetFilePath, new Cake.Common.Tools.DotNet.NuGet.Push.DotNetNuGetPushSettings
        {
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = context.NuGetPushToken
        });
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(NugetPushTask))]
public class DefaultTask : FrostingTask
{
}
