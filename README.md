# Code Updater

The purpose of this project is to update all code versioning and dependencies below a directory. This is useful when you have a large number of projects that you want to update all at once. This application is designed to be run on a local machine, and then the changes are committed to source control. This allows for a more controlled update process, as you can manually check the changes before committing them.

## What this Updates

- Updates .NET *.csproj files to use a specified C# Language Version
- Updates .NET *.csproj files to use a specified .NET SDK Version (TargetFramework)
- Updates all NuGet packages in all *.csproj files to the latest version
- Updates all NPM packages in all package.json files to the latest version

## How to Use

Remember, the purpose of this is to update code. It is assumed, and recommended, a developer runs this locally and verifies the changes before comitting to source control. Below are the steps to take.

There are 2 ways to run this. As a .NET Tool installed on your machine, or downloading the repository and running the code yourself.

1. Install the application
  - Install the tool globally by running `todo`
  - Clone this repository locally
2. Run the application
  - If you installed the tool, run it with the command: `code-updater --config-file "C:/my-repos/my-app-1"`
  - If you downloaded the code, open a terminal to the `~/src/CodeUpdater/CodeUpdater` directory and run the application using dotnet run while passing in the required arguments. Example: `dotnet run -- --config-file "C:/my-repos/my-app-1"`
3. Wait for the application to finish. It will output the number of projects updated, and the number of projects that failed to update.
4. Manually check a diff of all the projects to ensure everything is as you expect
5. Commit the code changes to source control. Wait for a CI/CD pipeline to run and ensure everything is still working as expected.

## CLI Options

- --config-file, -d
	- Required
	- Path to the file to use for config values when updating code

## Config File

The config file holds all values to determine what changes to make to code files. The reason this is separate from CLI input arguments is to let a developer store this config in different repos but have this .NET Tool installed globally on their machine. That makes it easy to let other developers run this tool with specific settings for each repository, while only needing to provide a single CLI input argument.

Below are the list of properties in the config file. All fields are required.

- RootDirectory
	- Root directory to run from. Code Updater will search all child directories within this for projects to update.
- IgnorePatterns
	- String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \ as the path separator for Windows, and / as the path separator everywhere else. Eg: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. Note this example will only happen on Windows because that uses backslashes for path separators.- NpmBuildCommand
- NpmBuildCommand
	- After upgrading all of the code, this application will attempt to build all applications it updated. This option sets the npm command to run to do the build.
	- Npm command to run to "compile" the npm directory. Default value is `publish`. Format run is: npm run <npmBuildCommand>.
- DotNetTargetFramework
	- Target Framework to set in all *.CsProj files
- DotNetLangVersion
	- C# language version to set in all *.CsProj files
- EnableNetAnalyzers
	- Boolean value to set the `EnableNetAnalyzers` csproj element to. If the `EnableNetAnalyzers` element does not exist in the project file, it will be added.
- EnforceCodeStyleInBuild
	- Boolean value to set the `EnforceCodeStyleInBuild` csproj element to. If the `EnableNetAnalyzers` element does not exist in the project file, it will be added.

### Example Config File

```json
{
  "RootDirectory": "C:/my-repos/my-app-1",
  "IgnorePatterns": [],
  "NpmBuildCommand": "publish",
  "DotNetTargetFramework": "net8.0",
  "DotNetLangVersion": "latest",
  "EnableNetAnalyzers": true,
  "EnforceCodeStyleInBuild": true
}
```
	 
## Ignore Patterns

The Code Updater application has a default set of paths to ignore. The list is below. Note that all paths are in the list using both forwardslashes and backslashes. These are in addition to any skip paths passed in with the `IgnorePatterns` config file property. As of right now, there is no way to remove these.

Ignore all obj and bin folders:
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

## .NET Standard Projects

When updating *.csproj files to a specific `TargetFramework` version, the project is skipped if using .NET Standard. Those are usually set for a specific level of API compatibility so we don't want to mess with those.


