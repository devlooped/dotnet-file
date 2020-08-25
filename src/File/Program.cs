using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mono.Options;

namespace Microsoft.DotNet
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var help = false;
            var options = new OptionSet
            {
                { "?|h|help", "Display this help", h => help = h != null },
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
                "list" => new ListCommand(config),
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
            for (int i = 0; i < extraArgs.Count; i++)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                // Try to pair Uri+File to allow intuitive download>path mapping, such as 
                // https://gitub.com/org/repo/docs/file.md docs/file.md
                if (Uri.TryCreate(extraArgs[i], UriKind.Absolute, out var uri))
                {
                    var next = i + 1;
                    // If the next arg is not a URI, use that as the file path for the uri
                    if (next < extraArgs.Count && !Uri.TryCreate(extraArgs[next], UriKind.Absolute, out _))
                    {
                        files.Add(new FileSpec(extraArgs[next], uri));
                        skip = true;
                    }
                    else
                    {
                        files.Add(new FileSpec(uri));
                    }
                }
            }

            command.Files.AddRange(files);

            return await command.ExecuteAsync();
        }

        static int ShowHelp()
        {
            Console.WriteLine($"Usage: dotnet {ThisAssembly.Metadata.AssemblyName} [add|changes|delete|list|update] [file or url]*");
            Console.WriteLine($"  = <- [url]        remote file equals local file");
            Console.WriteLine($"  ✓ <- [url]        local file updated with remote file");
            Console.WriteLine($"  ^ <- [url]        remote file is newer (ETags mismatch)");
            Console.WriteLine($"  ? <- [url]        local file not found for remote file");
            Console.WriteLine($"  x <- [url]        error processing the entry");

            return 0;
        }
    }
}
