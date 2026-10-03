#!/usr/bin/env dotnet
#:sdk Cake.Sdk@6.3.0
#:package Spectre.Console@0.57.2
#:package Cake.Docker@1.5.0
#:package Cake.Npm@5.1.0

var target = Argument("target", "UnitTests");
var configuration = Argument("configuration", "Release");

Lazy<GitVersionOutput> lazyGitVersionOutput = new(GetGitVersion);


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Build")
.Does(() =>
{
    var solutionFile = GetFiles("*.sln*").SingleOrDefault() ?? throw new Exception("Expected a single solution file to build");
    AnsiConsole.MarkupLine($"[blue]Building solution:[/] [yellow]{solutionFile.GetFilename().ToString().ToUpperInvariant()}[/]");

    var gitVersionOutput = lazyGitVersionOutput.Value;

    DotNetBuild(solutionFile.FullPath, new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version", gitVersionOutput.SemVer)
                .WithProperty("InformationalVersion", gitVersionOutput.InformationalVersion)
                .WithProperty("IncludeSourceRevisionInInformationalVersion", "false")
    });
});

Task("ScanContainerController")
.Does(() =>
{
    var gitVersionOutput = lazyGitVersionOutput.Value;

    var exitCode = StartProcess("trivy", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
        .Append("image")
        .Append("--scanners vuln")
        .Append("--exit-code 0")
        .Append("--format sarif")
        .Append("--output trivy-results.sarif")
        .AppendQuoted("/robalo-controller:{gitVersionOutput.SemVer}")
    });

    if (exitCode != 0)
        throw new Exception("Trivy scan could not complete.");
});

Task("BuildContainerController")
.Does(() =>
{
    var gitVersionOutput = lazyGitVersionOutput.Value;

    DockerBuildXBuild(new DockerBuildXBuildSettings
    {
        File = "src/Robalo.Controller.Api/Dockerfile",
        Load = true,

        Annotation = [
            $"org.opencontainers.image.revision={gitVersionOutput.Sha}"
        ],

        Tag = [
            $"robalo-controller:{gitVersionOutput.SemVer}",
            $"robalo-controller:latest"]
    }, ".");
});

Task("DockerLogin")
.Does(() =>
{
    var startInfo = new System.Diagnostics.ProcessStartInfo("docker")
    {
        UseShellExecute = false,
        RedirectStandardInput = true
    };

    startInfo.ArgumentList.Add("login");
    startInfo.ArgumentList.Add("--username");
    startInfo.ArgumentList.Add(EnvironmentVariable("DOCKERHUB_USERNAME"));
    startInfo.ArgumentList.Add("--password-stdin");

    using var process = System.Diagnostics.Process.Start(startInfo)
        ?? throw new Exception("Could not start Docker.");

    process.StandardInput.WriteLine(EnvironmentVariable("DOCKERHUB_TOKEN"));
    process.StandardInput.Close();
    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new Exception("Docker login failed.");
});

Task("BuildAndPushContainerController")
.IsDependentOn("BuildContainerController")
.IsDependentOn("PushContainerController");

Task("PushContainerController")
.IsDependentOn("DockerLogin")
.Does(() =>
{
    var gitVersionOutput = lazyGitVersionOutput.Value;

    var tags = new[]
    {
        $"{EnvironmentVariable("DOCKERHUB_USERNAME")}/robalo-controller:{gitVersionOutput.SemVer}",
        $"{EnvironmentVariable("DOCKERHUB_USERNAME")}/robalo-controller:latest"
    };

    DockerTag($"robalo-controller:{gitVersionOutput.SemVer}", tags.First());
    DockerTag($"robalo-controller:{gitVersionOutput.SemVer}", tags.Last());

    // Check permissions by checking out an existing image with tag last
    if (!DockerBuildXImageToolsInspect(tags.Last()).Any())
    {
        throw new Exception("Cannot an image with tag latest in the Registry");
    }

    IEnumerable<string>? result = null;
    try
    {
        result = DockerBuildXImageToolsInspect(tags.First());
    }
    // Expected to throw if an image is not found (and this is exactly what we are checking)
    catch
    {

    }

    // Check permissions by checking out an existing image with tag last
    if (result?.Any() == true)
    {
        throw new Exception($"Image with tag {tags.First()} already exists in the Repository.");
    }

    foreach (var tag in tags)
    {
        DockerPush(tag);
    }
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
            AnsiConsole.MarkupLine($"[blue]Running unit tests for project:[/] [yellow]{project.GetFilename().ToString().ToUpperInvariant()}[/]");
            DotNetTest(project.FullPath, new DotNetTestSettings
            {
                Configuration = configuration,
                ArgumentCustomization = args => args
                    .Append("--report-xunit-trx")
                    .Append("--report-xunit-trx-filename")
                    .AppendQuoted($"{project.GetFilenameWithoutExtension()}.trx")
                    .Append("--coverage")
                    .Append("--coverage-output-format cobertura")
                    .Append("--coverage-output")
                    .AppendQuoted($"{project.GetFilenameWithoutExtension()}.cobertura.xml")
                    .Append("--no-artifact-post-processing")
            });
        }
    });

Task("UnitTests")
.IsDependentOn("Build")
.Does(() =>
{
    foreach (var project in GetFiles("./**/*UnitTests.csproj"))
    {
        AnsiConsole.MarkupLine($"[blue]Running unit tests for project:[/] [yellow]{project.GetFilename().ToString().ToUpperInvariant()}[/]");
        DotNetTest(project.FullPath, new DotNetTestSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args
                   .Append("--report-xunit-trx")
                   .Append("--report-xunit-trx-filename")
                   .AppendQuoted($"{project.GetFilenameWithoutExtension()}.trx")
                   .Append("--coverage")
                   .Append("--coverage-output-format cobertura")
                   .Append("--coverage-output")
                   .AppendQuoted($"{project.GetFilenameWithoutExtension()}.cobertura.xml")
                   .Append("--no-artifact-post-processing")
        });
    }
});

Task("Test")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests");

Task("AcceptanceTests")
.Does(() =>
{
    NpmCi();
    NpmRunScript("test");
});

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

    if (string.IsNullOrWhiteSpace(gitVersionOutput?.Sha))
    {
        throw new InvalidOperationException($"GitVersion returned empty {nameof(GitVersionOutput.Sha)}");
    }

    if (string.IsNullOrWhiteSpace(gitVersionOutput?.ShortSha))
    {
        throw new InvalidOperationException($"GitVersion returned empty {nameof(GitVersionOutput.ShortSha)}");
    }

    return new(
        InformationalVersion: gitVersionOutput.InformationalVersion,
        SemVer: gitVersionOutput.SemVer,
        Sha: gitVersionOutput.Sha,
        ShortSha: gitVersionOutput.ShortSha);
}

record GitVersionOutput(
    string InformationalVersion,
    string SemVer,
    string Sha,
    string ShortSha);