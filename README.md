# Code Updater

The purpose of this project is to update specific aspects of code below a directory. This is useful when you have a large number of projects that you want to update all at once. This application is assumed to run on a local machine, and then the changes are manually committed to source control. This allows for a more controlled update process, because a developer is meant to manually check the changes before committing them.

It would be great to get this to work for all kinds of languages/frameworks someday, but for now it's just .NET and a little bit for NPM.

## What Updates Can Be Done

- Update `*.csproj` files to use a specified C# Language Version
- Update `*.csproj` files to use a specified .NET SDK Version (AKA TargetFramework element)
- Enable/Disable .NET Analyzers in all `*.csproj` files
- Run `dotnet format` command on all `*.csproj` files
- Update all NuGet packages in all *`.csproj` files to the latest version
- Add Nuget auditing properties to all `*.csproj` files 
- Update all NPM packages in all `package.json` files to the latest version
- Search all included files for a regex pattern

## How to Use It

Remember, the purpose of this is to update code and dependencies. It is assumed, and recommended, a developer runs this locally and verifies the changes before comitting the changes to source control. Below are the assumed steps a user would follow.

1. Install the Tool
  - Install the tool globally by running `dotnet tool install --global ProgrammerAL.Tools.CodeUpdater --version <<version number>>`
2. Create an Options File
  - The options file specifys what updates to run on the code
  - Instructions on how to create the file are below. See the `Options File` and `Example Options File` sections
3. Run the Tool
  - Run it with the command: `code-updater --options "C:/my-repos/my-app-1/my-options-file.json"`
4. Wait for the application to finish. It will output the number of projects updated, and the number of projects that failed to update.
5. Manually check a diff of all the file changes to ensure everything is as you expect
  - Make any manual changes you feel you need to
  - If there were any build failures that were caused by the updates, fix those
6. Commit the code changes to source control. Wait for a CI/CD pipeline to run and ensure everything is still working as expected. You do have a CI/CD pipeline, right?

## CLI Options

- `-o|--options`
	- Path to the options file to use for updating code
- `-h|--help`
  - Outputs CLI help

## Update Options File

This is a config file used by the app to determine what updates to run. It is composed of different objects which enable certain update features. Setting an object means that feature will run. Omitting it from the file will disable that update feature.

Below is a list of the required and optional propeties for the Update Options JSON. There is also a JSON Schema for this file at the root of this repository titled `UpdateOptionsSchema.json` which you can use to validate before running, if it helps.

- UpdatePathOptions
  - Required
  - This object determines what will be updated based on file paths
  - Properties:
    - RootDirectory
      - Root directory to run from. Code Updater will search all child directories within this for projects to update.
    - IgnorePatterns
    	- Strings to ignore within file paths when looking for projects to update. This is OS sensitive, so use \ as the path separator for Windows, and / as the path separator everywhere else. Ex: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. Note this example will only work on Windows because that uses backslashes for path separators.
- LoggingOptions
  - Optional
  - Options for output logging of the tool
  - Properties:
    - OutputFile
      - Optional
      - Path to a file to write logs to. Note that logs are always written to the console, even if you choose to output to a file.
    - LogLevel
      - Optional
      - Default Value: verbose
      - Verbosity level to log. Valid values are: Verbose, Info, Warn, Error
- CSharpOptions
  - Optional
  - This stores different objects, each are a set of options for what updates to perform on C# code and csproj files
  - Child Objects:
    - CsProjVersioningOptions
      - Optional
      - Versioning options for `*.csproj` files
      - Required Properties:
        - TargetFramework
          - String. Target Framework to set in all `*.csproj` files
        - LangVersion
          - String. C# language version to set in all `*.csproj` files
        - TreatWarningsAsErrors
          - Boolean. The value to set for the TreatWarningsAsErrors flag in all `*.csproj` files
    - CsProjDotNetAnalyzerOptions
      - Optional
      - .NET First Party Analyzer settings to set in all `*.csproj` files. You can read more at https://learn.microsoft.com/en-us/visualstudio/code-quality/install-net-analyzers?view=vs-2022
      - Required Properties:
        - EnableNetAnalyzers
          - Boolean. Value to set the `EnableNetAnalyzers` csproj value to
        - EnforceCodeStyleInBuild
          - Boolean. Value to set the `EnforceCodeStyleInBuild` csproj value to
    - CSharpStyleOptions
      - Optional
      - Options for any code styling updates that will be performed over C# code
      - Required Properties:
        - RunDotnetFormat
          - Boolean. True to run the `dotnet format` command
    - NugetOptions
      - Optional
      - Options for updating Nuget packages in csproj files
      - Properties:
        - AuditOptions
          - Optional
          - Settings to use for configuring Nuget Audit settings in csproj files. You can read more at https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
          - Required Properties:
            - NuGetAudit
              - Boolean. What value to set for the `NuGetAudit` property in the csproj file.
            - AuditMode
              - String. What value to set for the `NuGetAuditMode` property in the csproj file. Valid values are `direct` and `all`.
            - AuditLevel
              - String. What value to set for the `NuGetAuditLevel` property in the csproj file. Valid values are: `low`, `moderate`, `high`, and `critical`
        - UpdateOptions
          - Optional
          - Settings to use for updating NuGet packages in csproj files
          - Required Properties:
            - UpdateTopLevelNugetsInCsProj
              - Boolean. True to updates all referenced nugets to the latest version. These are the references in the csproj files.
            - UpdateTopLevelNugetsNotInCsProj
              - Boolean. True to updates all indirect nugets to the latest version. These are the nugets that are referenced automatically based on SDK chosen or something like that.
- NpmOptions
  - Optional
  - Options for updating Npm packages. If this is not set, NPM packages will not be updated
  - Properties:
    - NpmCompileOptions
      - Optional
      - Options for compiling Npm packages after updates. Note if this is not set, but the parent NpmOptions is set, NPM Packages will be updated but not tested with a compile.
      - Required Properties:
        - NpmBuildCommand
          - NpmBuildCommand
            - String. Npm command to "compile" the npm directory. The CLI command that will be run is: `npm run {{NpmBuildCommand}}`
- RegexSearchOptions
  - Optional
  - Regex to search for specific string. Handy for finding things you need to manually update, that this tool can't easily do. For example, use this to search for the hard coded .NET version in a YAML file for a CI/CD Pipeline so you know where to manually update it
  - Required Properties:
    - Searches:
      - Collection of searches to make in all files that are not ignored
        - Object Required Properties:
          - Search Regex:
            - String. Regex to search for in all files that are not ignored.
          - Description:
            - String. Description to show in the output.


### Example Options File

```json
{
  "updatePathOptions": {
    "rootDirectory": "./",
    "ignorePatterns": [
      "/samples/",
      "\\samples\\"
    ]
  },
  "loggingOptions": {
    "logLevel": "Verbose",
    "outputFile": "./code-updater-output.txt"
  },
  "cSharpOptions": {
    "csProjVersioningOptions": {
      "treatWarningsAsErrors": true,
      "targetFramework": "net8.0",
      "langVersion": "latest"
    },
    "csProjDotNetAnalyzerOptions": {
      "enableNetAnalyzers": true,
      "enforceCodeStyleInBuild": true
    },
    "cSharpStyleOptions": {
      "runDotnetFormat": true
    },
    "nugetOptions": {
      "auditOptions": {
        "nuGetAudit": true,
        "auditMode": "all",
        "auditLevel": "low"
      },
      "updateOptions": {
        "updateTopLevelNugetsInCsProj": true,
        "updateTopLevelNugetsNotInCsProj": true
      }
    }
  },
  "npmOptions": {
    "compileOptions": {
      "buildCommand": "publish"
    }
  },
  "regexSearchOptions": {
    "searches": [
      {
        "searchRegex": "[0-9]{1,2}\\..+\\.x",
        "description": "YAML Dotnet Version"
      }
    ]
  }
}
```

## Ignore Patterns

The Code Updater application has a default set of paths to ignore. The list is below. Note that all paths are in the list using both forwardslashes and backslashes. These are in addition to any skip paths passed in with the `IgnorePatterns` config file property. There is no way to remove these. Think of these like an ignore file, but no wildcard syntax. it's just basic string matching.

Ignore all C# `obj` and `bin` folders:
- /obj/Debug/
- /obj/Release/
- /bin/Debug/
- /bin/Release/
- \obj\Debug\
- \obj\Release\
- \bin\Debug\
- \bin\Release\

Ignore packages inside node_modules folder:
- /node_modules/
- \node_modules\

## Installing Locally vs Downloading the Code

The tool can be very opinionated. For example, when updating packages it will only update to the latest version. If the tool does some things you can't use for your projects, you can download the code, make changes, and keep that for yourself (OSS FTW). If this is something you have to do, feel free to file an issue in the repo and and we can discuss it. Maybe a change you need can be incorporated into the project.

## Required 3rd Party Software

In order to run the tool you need the following software installed on your local machine.

- .NET CLI
  - So you can install .NET Tool from nuget.org
- PowerShell
  - Quick Reminder: PowerShell is cross platform, you can run it on Linux and MacOS, not just Windows

PowerShell is required as a workaround. The NPM executable on Windows doesn't run like other applications, and as a result, on Windows, it doesn't exit like a normal process. I don't know why, I never spent the time figuring it out. The workaround makes PowerShell the host application so it exits like you would expect, when the process is done. For this reason, whenever an external process must be run, it's run through PowerShell. It's a hack, but it works well enough.

## .NET Standard Projects

When updating csproj files to a specific `TargetFramework` version, the project is skipped if using .NET Standard. Those are usually set for a specific level of API compatibility so we don't want to mess with those.

## Update Script Sample

You can create a script to make it easy to run Code Updater on a regular basis. A sample PowerShell script to do that is in the `/code-update-runner-sample` directory of this repo. It has the below files:

- `code-updater-config.json`
  - Example config file to use when running Code Updater
- `run-code-updater.ps1`
  - PowerShell script that runs Code Updater one directory up in the tree, using the given `code-updater-config.json` file as config
 
Feel free to use those files as a base, and modify them for your repositories as needed. For extra points, commit them to your repository to make it easy to use in the future.

