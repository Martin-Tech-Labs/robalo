#!/usr/bin/env dotnet
#:sdk Cake.Sdk@6.3.0
#:package Spectre.Console@0.57.2

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Build")
.IsDependentOn("UnitTests")
.Does(() =>
{
    var solutionFile = GetFiles("*.sln*").SingleOrDefault() ?? throw new Exception("Expected a single solution file to build");
    AnsiConsole.MarkupLine($"[blue]Building solution:[/] [yellow]{solutionFile.GetFilename().ToString().ToUpperInvariant()}[/]");

    DotNetToolRestore();

    DotNetToolExecute("GitVersion.Tool", new DotNetToolExecuteSettings
    {

    });

    DotNetBuild(solutionFile.FullPath, new DotNetBuildSettings
    {
        Configuration = configuration
    });
});

Task("IntegrationTests")
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



static void GetVersion()
{
    var exitCode = StartProcess(
    "dotnet",
    new ProcessSettings
    {
        Arguments = "tool run GitVersion.Tool /output json",
        RedirectStandardOutput = true
    },
    out IEnumerable<string> output);

    if (exitCode != 0)
    {
        throw new Exception($"GitVersion failed with exit code {exitCode}");
    }

    var json = string.Join(Environment.NewLine, output);
}

record GitVersion(string InformationalVersion);
