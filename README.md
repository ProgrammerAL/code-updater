# Code Updater

The purpose of this project is to update specific aspects of code below a directory. This is useful when you have a large number of projects that you want to update all at once. This application is assumed to run on a local machine, and then the changes are manually committed to source control. This allows for a more controlled update process, because a developer is meant to manually check the changes before committing them.

It would be great to get this to work for all kinds of languages/frameworks someday, but for now it's just .NET and a little bit for NPM.

## What Updates Cane Be Done

- Update .NET *.csproj files to use a specified C# Language Version
- Update .NET *.csproj files to use a specified .NET SDK Version (AKA TargetFramework element)
- Enable/Disable .NET Analyzers in all *.csproj files
- Run `dotnet format` command on all *.csproj files
- Update all NuGet packages in all *.csproj files to the latest version
- Add Nuget auditing properties to all *.csproj files 
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

- `-d|--config`
	- Required
	- Path to the file to use for config values when updating code
- `-h|--help`
  - Outputs CLI help

## Config File

The config file holds all values to determine what changes to make. The reason this is separate from CLI input arguments is to let a developer store this config in different repos but have this .NET Tool installed globally on their machine. That makes it easy to let other developers run this tool with specific settings for each repository, while only needing to provide a single CLI input argument.

Below are the list of properties in the config file.

- RootDirectory
  - Required
	- Root directory to run from. Code Updater will search all child directories within this for projects to update.
- IgnorePatterns
  - Required
	- String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \ as the path separator for Windows, and / as the path separator everywhere else. Eg: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. Note this example will only happen on Windows because that uses backslashes for path separators.- NpmBuildCommand
- NpmBuildCommand
  - Required
	- After upgrading all of the code, this application will attempt to build all applications it updated. This option sets the npm command to run to do the build.
	- Npm command to run to "compile" the npm directory. Default value is `publish`. Format run is: npm run <npmBuildCommand>.
- DotNetTargetFramework
  - Required
	- Target Framework to set in all *.CsProj files
- DotNetLangVersion
  - Required
	- C# language version to set in all *.CsProj files
- EnableNetAnalyzers
  - Required
	- Boolean value to set the `EnableNetAnalyzers` csproj element to. If the `EnableNetAnalyzers` element does not exist in the project file, it will be added.
- EnforceCodeStyleInBuild
  - Required
	- Boolean value to set the `EnforceCodeStyleInBuild` csproj element to. If the `EnableNetAnalyzers` element does not exist in the project file, it will be added.
- RunDotnetFormat
  - Required
  - True to run the `dotnet format` command on all csproj files
- NugetAudit
  - Required
  - This is an object with 3 properties, each is required. For more information see the Microsoft documentation at https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
    - NuGetAudit
      - Boolean to enable/disable the NuGet Audit features. Note that even if this is set to false, you still need to provide values for the other 2 properties even though they won't be used.
    - AuditMode
      - String. Which mode to audit, `direct` dependencies, or `all` dependencies.
    - AuditLevel
      - String. Minimum severity level to report. Ex: If you set it to moderate, you get `moderate`, `high`, and `critical` advisories. If you set it to `low`, you get all advisories.
- OutputFile
  - Optional
  - If this is set, it will be the file to write logs to, in addition to the console
- LogLevel
  - Optional
  - Verbosity level to log. Valid values are: Verbose, Info, Warn, Error. Default value: verbose.

### Example Config File

```json
{
  "RootDirectory": "C:\\GitHub\\ProgrammerAl\\ProgrammerAlSite",
  "IgnorePatterns": [],
  "NpmBuildCommand": "publish",
  "DotNetTargetFramework": "net8.0",
  "DotNetLangVersion": "latest",
  "EnableNetAnalyzers": true,
  "EnforceCodeStyleInBuild": true,
  "RunDotnetFormat": true,
  "NugetAudit": {
    "NuGetAudit": true,
    "AuditMode": "all",
    "AuditLevel": "low"
  },
  "OutputFile": "./code-updater-output.txt",
  "LogLevel": "Verbose"
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

The tool is very opinionated. It updates all packages to the latest version, and sets some project level properties. If the tool does some things you can't use for your projects, you can download the code, make changes, and keep thay for yourself. Maybe in the future we can hide certain settings behind a flag. Feel free to file an issue and and we can discuss it.

## Required 3rd Party Software

In order to run the tool you need the following software installed on your local machine.

- .NET CLI
  - So you can install .NET Tool from nuget.org
- PowerShell
  - Quick Reminder: PowerShell is cross platform, you can run it on Linux and MacOS, not just Windows

PowerShell is required as a workaround. The NPM executable on Windows doesn't run like other applications. It doesn't exit like a normal process. I don't know why, I never spent the time figuring it out. The workaround makes PowerShell the host application so it exits like you would expect, when the process is done. For this reason, whenever an external process must be run, it's run through PowerShell. Kind of a hack, but it works well enough.

## .NET Standard Projects

When updating *.csproj files to a specific `TargetFramework` version, the project is skipped if using .NET Standard. Those are usually set for a specific level of API compatibility so we don't want to mess with those.


## Update Script Sample

You can create a script to make it easy to run Code Updater on a regular basis. A sample PowerShell script to do that is in the `/code-update-runner-sample` directory of this repo. It has the below files:

- `code-updater-config.json`
  - Example config file to use when running Code Updater
- `run-code-updater.ps1`
  - PowerShell script that runs Code Updater one directory up in the tree, using the given `code-updater-config.json` file as config
 
Feel free to use those files as a base, and modify them for your repositories as needed. For extra points, commit them to your repository too.

