#!/usr/bin/env dotnet
#:sdk Cake.Sdk@6.3.0
#:package Spectre.Console@0.57.2

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Build")
.Does(() =>
{
    var solutionFile = GetFiles("*.sln*").SingleOrDefault() ?? throw new Exception("Expected a single solution file to build");
    AnsiConsole.MarkupLine($"[blue]Building solution:[/] [yellow]{solutionFile.GetFilename().ToString().ToUpperInvariant()}[/]");

    var gitVersionOutput = GetGitVersion();

    DotNetBuild(solutionFile.FullPath, new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version", gitVersionOutput.SemVer)
                .WithProperty("InformationalVersion", gitVersionOutput.InformationalVersion)
                .WithProperty("IncludeSourceRevisionInInformationalVersion", "false")
    });
});

Task("PublishController")
.IsDependentOn("Build")
.Does(() =>
{
    DotNetPublish("src/Robalo.Controller.Api/Robalo.Controller.Api.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        NoBuild = true,
        OutputDirectory = "app/publish"
    });
});

Task("IntegrationTests")
.IsDependentOn("Build")
.Does(() =>
    {
        foreach (var project in GetFiles("./**/*IntegrationTests.csproj"))
        {
            DotNetTest(project.FullPath, new DotNetTestSettings
            {
                Configuration = configuration
            });
        }
    });

Task("UnitTests")
.IsDependentOn("Build")
.Does(() =>
{
    foreach (var project in GetFiles("./**/*UnitTests.csproj"))
    {
        DotNetTest(project.FullPath, new DotNetTestSettings
        {
            Configuration = configuration
        });
    }
});

Task("Test")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);


static GitVersionOutput GetGitVersion()
{
    DotNetToolRestore();

    var exitCode = StartProcess(
    "dnx",
    new ProcessSettings
    {
        Arguments = "GitVersion.Tool /output json",
        RedirectStandardOutput = true
    },
    out IEnumerable<string> output);

    if (exitCode != 0)
    {
        throw new Exception($"GitVersion failed with exit code {exitCode}");
    }

    var json = string.Join(Environment.NewLine, output);
    var gitVersionOutput = JsonSerializer.Deserialize<GitVersionOutput>(json);

    var informationalVersion = gitVersionOutput?.InformationalVersion;

    if (string.IsNullOrWhiteSpace(gitVersionOutput?.InformationalVersion))
    {
        throw new InvalidOperationException($"GitVersion returned empty {nameof(GitVersionOutput.InformationalVersion)}");
    }

    if (string.IsNullOrWhiteSpace(gitVersionOutput?.SemVer))
    {
        throw new InvalidOperationException($"GitVersion returned empty {nameof(GitVersionOutput.SemVer)}");
    }

    return new(gitVersionOutput.InformationalVersion, gitVersionOutput.SemVer);
}

record GitVersionOutput(string InformationalVersion, string SemVer);