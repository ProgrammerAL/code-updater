# Code Updater

The purpose of this project is to update specific aspects of code below a directory. This is useful when you have a large number of projects that you want to update all at once. This application is assumed to run on a local machine, and then the changes are manually committed to source control. This allows for a more controlled update process, because a developer is meant to manually check the changes before committing them.

It would be great to get this to work for all kinds of languages/frameworks someday, but for now it's just .NET and a little bit for NPM.

## What Updates Can Be Done

- Update .NET `*.csproj` files to use a specified C# Language Version
- Update .NET `*.csproj` files to use a specified .NET SDK Version (AKA TargetFramework element)
- Enable/Disable .NET Analyzers in all `*.csproj` files
- Run `dotnet format` command on all `*.csproj` files
- Update all NuGet packages in all *`.csproj` files to the latest version
- Add Nuget auditing properties to all `*.csproj` files 
- Update all NPM packages in all package.json files to the latest version

## How to Use It

Remember, the purpose of this is to update code. It is assumed, and recommended, a developer runs this locally and verifies the changes before comitting to source control. Below are the assumed steps a user would follow.

There are 2 ways to run this. As a .NET Tool installed on your machine, or downloading the repository and running the code yourself.

1. Install the application. Choose one:
  - Install the tool globally by running `dotnet tool install --global ProgrammerAL.Tools.CodeUpdater --version <<version number>>`
  - Or clone this repository locally
2. Run the application
  - If you installed the tool, run it with the command: `code-updater --config "C:/my-repos/my-app-1"`
  - If you downloaded the code, open a terminal to the `~/src/CodeUpdater/CodeUpdater` directory and run the application using dotnet run while passing in the required arguments. Example: `dotnet run -- --config "C:/my-repos/my-app-1"`
3. Wait for the application to finish. It will output the number of projects updated, and the number of projects that failed to update.
4. Manually check a diff of all the file changes to ensure everything is as you expect
  - Make any manual changes you feel you need to
  - If there were any build failures that were caused by the updates, fix those
5. Commit the code changes to source control. Wait for a CI/CD pipeline to run and ensure everything is still working as expected.

## CLI Options

- `-o|--options`
	- Required
	- Path to the file to use for config values when updating code
- `-h|--help`
  - Outputs CLI help

## Options File

This is a config file used by the app to determine what updates to run. It is composed of different objects which enable update features. Setting an object means that feature will run, leaving it null will disable that update feature. 


- UpdatePathOptions
  - Required
  - This object holds settings for what to update
  - Properties:
    - RootDirectory
      - Root directory to run from. Code Updater will search all child directories within this for projects to update.
    - IgnorePatterns
    	- String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \ as the path separator for Windows, and / as the path separator everywhere else. Eg: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. Note this example will only happen on Windows because that uses backslashes for path separators.- NpmBuildCommand
- LoggingOptions
  - Optional
  - Options for output logging of the operation
  - Required Properties:
    - OutputFile
      - If this is set, it will be the file to write logs to, in addition to the console
    - LogLevel
      - Verbosity level to log. Valid values are: Verbose, Info, Warn, Error. Default value: verbose.
- CSharpOptions
  - Optional
  - This stores different options for what updates to perform on C# code
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
      - .NET Analyzer settings to set in all `*.csproj` files
      - Required Properties:
        - EnableNetAnalyzers
          - Boolean. True to set the `EnableNetAnalyzers` csproj value to true, false to set it to false
        - EnforceCodeStyleInBuild
          - Boolean. True to set the `EnforceCodeStyleInBuild` csproj value to true, false to set it to false
    - CSharpStyleOptions
      - Options for any code styling updates that will be performed over C# code
      - Required Properties:
        - RunDotnetFormat
          - Boolean. True to run the `dotnet format` command
    - NugetAuditOptions
      - Settings to use for configuring Nuget Audit settings in csproj files. You can read more at https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
      - Required Properties:
        - NuGetAudit
          - Boolean. What value to set for the `NuGetAudit` property in the csproj file.
        - AuditMode
          - String. What value to set for the `NuGetAuditMode` property in the csproj file. Valid values are `direct` and `all`.
        - AuditLevel
          - String. What value to set for the `NuGetAuditLevel` property in the csproj file. Valid values are: `low`, `moderate`, `high`, and `critical`
    - NuGetUpdateOptions
      - Settings to use for updating NuGet packages in csproj files
      - Required Properties:
        - UpdateTopLevelNugetsInCsProj
          - Boolean. True to updates all referenced nugets to the latest version. These are the references in the csproj files.
        - UpdateTopLevelNugetsNotInCsProj
          - Boolean. True to updates all indirect nugets to the latest version. These are the nugets that are referenced automatically based on SDK chosen or something like that.
- NpmOptions
  - Optional
  - Options for updating Npm packages
  - Required Properties:
    - NpmBuildCommand
      - NpmBuildCommand
        - String. Npm command to \"compile\" the npm directory. Format of the command run is: `npm run {{NpmBuildCommand}}`


### Example Options File

```json
{
  "UpdatePathOptions": {
    "RootDirectory": "C:/my-repos/my-app-1",
    "IgnorePatterns": [
      "/samples/",
      "\\samples\\"
    ]
  },
  "LoggingOptions": {
    "LogLevel": "Verbose",
    "OutputFile": "./code-updater-output.txt"
  },
  "CSharpOptions": {
    "CsProjVersioningOptions": {
      "TreatWarningsAsErrors": true,
      "TargetFramework": "net8.0",
      "LangVersion": "latest"
    },
    "CsProjDotNetAnalyzerOptions": {
      "EnableNetAnalyzers": true,
      "EnforceCodeStyleInBuild": true
    },
    "CSharpStyleOptions": {
      "RunDotnetFormat": true
    },
    "NugetAuditOptions": {
      "NuGetAudit": true,
      "AuditMode": "all",
      "AuditLevel": "low"
    },
    "NuGetUpdateOptions": {
      "UpdateTopLevelNugetsInCsProj": true,
      "UpdateTopLevelNugetsNotInCsProj": true
    }
  },
  "NpmOptions": {
    "NpmBuildCommand": "publish"
  }
}
```
	 
## Ignore Patterns

The Code Updater application has a default set of paths to ignore. The list is below. Note that all paths are in the list using both forwardslashes and backslashes. These are in addition to any skip paths passed in with the `IgnorePatterns` config file property. As of right now, there is no way to remove these.

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

The tool can be very opinionated. When updating packages it will only update to the latest version. If the tool does some things you can't use for your projects, you can download the code, make changes, and keep that for yourself (OSS FTW). If this is something you have to do, feel free to file an issue in the repo and and we can discuss it.

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

