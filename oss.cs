// First run: dnx runfile https://github.com/devlooped/oss/blob/main/oss.cs --yes --alias oss
// Subsequently: dnx runfile oss --yes
#:package Spectre.Console@*
#:package CliWrap@*

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliWrap;
using Spectre.Console;

string dotnet = Path.GetFullPath(
    Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet"));

AnsiConsole.Write(new FigletText("devlooped oss").Color(Color.Green));
AnsiConsole.WriteLine();

var projectName = AnsiConsole.Prompt(
    new TextPrompt<string>("[green]Project name[/]:")
        .PromptStyle("yellow")
        .ValidationErrorMessage("[red]Project name cannot be empty[/]")
        .Validate(v => !string.IsNullOrWhiteSpace(v)));

var repoName = AnsiConsole.Prompt(
    new TextPrompt<string>("[green]Repo name[/]:")
        .PromptStyle("yellow")
        .DefaultValue(projectName));

var packageId = AnsiConsole.Prompt(
    new TextPrompt<string>("[green]Package ID[/]:")
        .PromptStyle("yellow")
        .DefaultValue($"Devlooped.{projectName}"));

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[yellow]Setting up OSS project[/]").RuleStyle("grey").LeftJustified());
AnsiConsole.WriteLine();

await RunDotNet("file init https://github.com/devlooped/oss/blob/main/.netconfig", "Initializing dotnet file sync from devlooped/oss");
await RunDotNet($"new classlib -n {projectName} -o src/{projectName} -f net10.0", $"Creating class library src/{projectName}");
await RunDotNet($"new xunit -n Tests -o src/Tests -f net10.0", "Creating xUnit test project src/Tests");
await RunDotNet($"add src/Tests/Tests.csproj reference src/{projectName}/{projectName}.csproj", $"Adding reference from Tests to {projectName}");
await RunDotNet($"new solution -n {projectName}", $"Creating solution {projectName}.slnx");
await RunDotNet($"sln {projectName}.slnx add src/{projectName}/{projectName}.csproj src/Tests/Tests.csproj", $"Adding projects to {projectName}.slnx");

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("green"))
    .StartAsync("Downloading readme.md template...", async ctx =>
    {
        using var http = new HttpClient();
        var readmeContent = await http.GetStringAsync(
            "https://raw.githubusercontent.com/devlooped/oss/main/readme.tmp.md");

        readmeContent = readmeContent
            .Replace("{{PROJECT_NAME}}", projectName)
            .Replace("{{PACKAGE_ID}}", packageId)
            .Replace("{{REPO_NAME}}", repoName);

        await File.WriteAllTextAsync("readme.md", readmeContent);
        ctx.Status("Downloaded and processed readme.md");
    });

AnsiConsole.MarkupLine("[green]✓[/] Created [yellow]readme.md[/]");

await File.WriteAllTextAsync(
    Path.Combine("src", projectName, "readme.md"),
    $"""
    [![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](osmfeula.txt)
    [![OSS](https://img.shields.io/github/license/devlooped/oss.svg?color=blue)](license.txt)
    [![GitHub](https://img.shields.io/badge/-source-181717.svg?logo=GitHub)](https://github.com/devlooped/{repoName})

    <!-- include ../../readme.md#content -->

    <!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->

    <!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->

    <!-- exclude -->
    """);

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[green]Done![/]").RuleStyle("grey").LeftJustified());
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[bold]Project:[/] [yellow]{projectName}[/]");
AnsiConsole.MarkupLine($"[bold]Repo:[/] [yellow]{repoName}[/]");
AnsiConsole.MarkupLine($"[bold]Package ID:[/] [yellow]{packageId}[/]");

async Task RunDotNet(string command, string description)
{
    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(description)}...[/]");
    await Cli.Wrap(dotnet)
        .WithArguments(command)
        .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
        .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
        .ExecuteAsync();
    AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(description)}");
}