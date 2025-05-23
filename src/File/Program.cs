﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetConfig;
using Mono.Options;

namespace Devlooped;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var help = false;
        string? changelog = null;
        var options = new OptionSet
        {
            { "?|h|help", "Display this help", h => help = h != null },
            { "c|changelog:", "Write a changelog", c => changelog = string.IsNullOrEmpty(c) ? "dotnet-file.md" : c },
        };

        var extraArgs = options.Parse(args);
        if ((args.Length == 1 && help) || extraArgs.Count == 0)
            return ShowHelp();

        var config = Config.Build();
        var command = extraArgs[0].ToLowerInvariant() switch
        {
            "add" => new AddCommand(config),
            "changes" => new ChangesCommand(config),
            "delete" => new DeleteCommand(config),
            "init" => new InitCommand(config),
            "list" => new ListCommand(config),
            "sync" => new SyncCommand(config),
            "update" => new UpdateCommand(config),
            _ => Command.NullCommand,
        };

        if (command == Command.NullCommand)
            return ShowHelp();

        // Remove first arg which is the command to use.
        extraArgs.RemoveAt(0);

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
                configured ??= command.GetConfiguredFiles().ToList();
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
            GitHub.IsInstalled)
        {
            GitHub.WriteChanges(changelog!, add.Changes);
        }

        return result;
    }

    static int ShowHelp()
    {
        Console.WriteLine(File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Help.txt")));

        return 0;
    }
}
