using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColoredConsole;
using Devlooped;
using DotNetConfig;
using Mono.Options;

var help = false;
string? changelog = null;
string? workingDir = null;
var options = new OptionSet
        {
            { "?|h|help", "Display this help", h => help = h != null },
            { "c|changelog:", "Write a changelog", c => changelog = string.IsNullOrEmpty(c) ? "dotnet-file.md" : c },
            { "w|workingDir:", "Working directory", w => workingDir = string.IsNullOrEmpty(w) ? null : w },
        };

var extraArgs = options.Parse(args);
if ((args.Length == 1 && help) || extraArgs.Count == 0)
    return ShowHelp();

if (workingDir != null)
    Directory.SetCurrentDirectory(workingDir);

var commandName = extraArgs[0].ToLowerInvariant();
extraArgs.RemoveAt(0);

var config = Config.Build();

// Handle --init [url] as a cross-cutting pre-processing step: download a remote
// .netconfig and merge its skip=true entries into the local config before any command
// runs, so upstream skips are honored regardless of which command is executed.
string? initUrl = null;
if (extraArgs.FindIndex(a => a == "--init") is var initIndex && initIndex >= 0 &&
    initIndex + 1 < extraArgs.Count && !extraArgs[initIndex + 1].StartsWith('-'))
{
    initUrl = extraArgs[initIndex + 1];
    extraArgs.RemoveRange(initIndex, 2);

    var tempConfig = Path.GetTempFileName();
    var tempFile = Path.GetTempFileName();
    try
    {
        var addCmd = new AddCommand(Config.Build(tempConfig));
        addCmd.Files.Add(new FileSpec(tempFile, new Uri(initUrl)));

        ColorConsole.WriteLine("Downloading init config file(s)...".Yellow());
        await addCmd.ExecuteAsync();

        var remoteConfig = Config.Build(tempFile);
        foreach (var group in remoteConfig
            .Where(x => x.Level == null && x.Section == "file" && x.Subsection != null)
            .GroupBy(x => x.Subsection!))
        {
            var path = group.Key;
            if (remoteConfig.GetBoolean("file", path, "skip") != true)
                continue;

            // Local entry takes precedence — only add skip if no local entry exists
            if (config.Where(x => x.Section == "file" && x.Subsection == path).Any())
                continue;

            var url = remoteConfig.GetString("file", path, "url");
            if (url != null)
                config = config.SetString("file", path, "url", url);

            config = config.SetBoolean("file", path, "skip", true);
        }
    }
    finally
    {
        File.Delete(tempConfig);
        File.Delete(tempFile);
    }
}

var command = commandName switch
{
    "add" => new AddCommand(config),
    "changes" => new ChangesCommand(config),
    "delete" => new DeleteCommand(config),
    "init" => new InitCommand(config),
    "list" => new ListCommand(config),
    "sync" => new SyncCommand(config),
    "update" => new UpdateCommand(config),
    _ => Devlooped.Command.NullCommand,
};

if (command == Devlooped.Command.NullCommand)
    return ShowHelp();

// Add remainder arguments as if they were just files or urls provided 
// to the command. Allows skipping the -f|-u switches.
var skip = false;
var files = new List<FileSpec>();
List<FileSpec>? configured = default;

for (var i = 0; i < extraArgs.Count; i++)
{
    if (skip)
    {
        skip = false;
        continue;
    }

    // Try to pair Uri+File to allow intuitive download>path mapping, such as 
    // https://gitub.com/org/repo/docs/file.md > docs/file.md
    if (Uri.TryCreate(extraArgs[i], UriKind.Absolute, out var uri))
    {
        var next = i + 1;
        // If the next arg is not a URI, use that as the file path for the uri
        if (next < extraArgs.Count && !Uri.TryCreate(extraArgs[next], UriKind.Absolute, out _))
        {
            files.Add(FileSpec.WithPath(extraArgs[next], uri));
            skip = true;
        }
        else
        {
            // If the next file is a URI, then no path has been specified. 
            // We should attempt to recreate the same path structure locally, 
            // which is the most intuitive default. If users don't want that, 
            // they can specify '.' to get the old behavior.
            files.Add(FileSpec.WithDefaultPath(uri));
        }
    }
    else
    {
        // Attempt to match a simple filename to a configured one
        configured ??= [.. command.GetConfiguredFiles()];
        if (configured.FirstOrDefault(x => x.Path == extraArgs[i]) is { } spec)
            files.Add(spec);
    }
}

command.Files.AddRange(files);

var result = await command.ExecuteAsync();

// If there were changes and a changelog was requested, emit it 
// to a file.
if (changelog != null &&
    command is AddCommand add &&
    add.Changes.Count > 0 &&
    Devlooped.GitHub.IsInstalled)
{
    Devlooped.GitHub.WriteChanges(changelog!, add.Changes);
}

return result;

static int ShowHelp()
{
    Console.WriteLine(File.ReadAllText(
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Help.txt")));

    return 0;
}
