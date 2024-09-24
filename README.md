# Code Updater

The purpose of this project is to update all code versioning and dependencies below a directory. This is useful when you have a large number of projects that you want to update all at once. This application is designed to be run on a local machine, and then the changes are committed to source control. This allows for a more controlled update process, as you can manually check the changes before committing them.

## What this Updates

- Updates .NET *.csproj files to use a specified C# Language Version
- Updates .NET *.csproj files to use a specified .NET SDK Version (TargetFramework)
- Updates all NuGet packages in all *.csproj files to the latest version
- Updates all NPM packages in all package.json files to the latest version

## How to Use

Remember, the purpose of this is to update code. It is assumed, and recommended, a developer runs this locally and verifies the changes before comitting to source control. Below are the steps to take.

1. Clone the repository locally
1. Run the application using dotnet run. Pass in the required arguments. Example: `dotnet run -- --directory "C:\my-repos/my-app-1" --ignorePatterns "\ignore-path-1\" --ignorePatterns "\ignore-path-2\"`
1. Wait for the application to finish. It will output the number of projects updated, and the number of projects that failed to update.
1. Manually check a diff of all the projects to ensure everything is as you expect
1. Commit the code changes to source control. Wait for a CI/CD pipeline to run and ensure everything is still working as expected.

## CLI Options

- --directory, -d
	- Required
	- Root directory to search within for all projects to update
- --ignorePatterns, -p
	- Optional
	- Allows Multiples
	- String to ignore within file paths when looking for projects to update. This is OS sensitive, so use \ as the path separator for Windows, and / as the path separator everywhere else. Eg: `\my-skip-path\` will ignore all projects that have the text `\my-skip-path\` within the full path. Note this example will only happen on Windows because that uses backslashes for path separators.
- --npmBuildCommand, -n
	- Optional
	- Default Value: `publish`
	- After upgrading all of the code, this application will attempt to build all applications it updated. This option sets the npm command to run to do the build.
	- Npm command to run to "compile" the npm directory. Default value is `publish`. Format run is: npm run <npmBuildCommand>.
- --dotnetTargetFramework, -t
	- Optional
	- Default Value: `net8.0`
	- Target Framework to set in all *.CsProj files. Default value is `net8.0`
- --dotnetLangVersion, -l
	- Optional
	- Default Value: `latest`
	- C# language version to set in all *.CsProj files. Default value is `latest`
	 
## Ignore Patterns

The Code Updater application has a default set of paths to ignore. The list is below. Note that all paths are in the list using both forwardslashes and backslashes. These are in addition to any skip paths passed in with the `--ignorePatterns` input argument. As of right now, there is no way to remove these.

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


