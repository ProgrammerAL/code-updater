using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammerAL.Tools.CodeUpdater;

public record CSharpUpdateResult(
    string CsprojFile, 
    NugetUpdateResults NugetUpdates, 
    CsprojValueUpdateResultType LangVersionUpdate, 
    CsprojValueUpdateResultType TargetFrameworkUpdate,
    DotnetFormatResult DotnetFormatUpdate);

public record NugetUpdateResults(bool RetrievedPackageListSuccessfully, ImmutableArray<NugetUpdateResult> Updates);
public record NugetUpdateResult(string CsProjFile, string PackageId, bool UpdatedSuccessfully);

public record CsProjUpdateResult(string CsProjFile, CsprojValueUpdateResultType LangVersionUpdate, CsprojValueUpdateResultType TargetFrameworkUpdate);
public record DotnetFormatResult(string CsProjFile, DotnetFormatResultType Result);

public record NpmUpdates(ImmutableArray<string> NpmDirectories);

public record CompileResults(CompileCsProjResults CompileCsProjResults, CompileNpmDirectoryResults CompileNpmDirectoryResults);

public record CompileCsProjResults(ImmutableArray<CompileCsProjResult> Results);
public record CompileCsProjResult(string CsProjFile, CompileResultType BuildResult);

public record CompileNpmDirectoryResults(ImmutableArray<CompileNpmDirectoryResult> Results);
public record CompileNpmDirectoryResult(string Directory, CompileResultType BuildResult);

public enum CompileResultType
{
    Success,
    ProcessDidNotStart,
    BuildTimeout,
    BuildErrors
}

public enum CsprojValueUpdateResultType
{
    Unknown,
    NotFound,
    AlreadyHasCorrectValue,
    HasSkipValue,
    Updated,
    AddedElement
}

public enum DotnetFormatResultType
{
    Unknown,
    DidNotRun,
    RanSuccessfully,
    Erroreded
}