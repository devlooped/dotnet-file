using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Options;

namespace Microsoft.DotNet
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var help = false;
            var files = new List<FileSpec>();

            var options = new OptionSet
            {
                { "f|file:", "file to download, update or delete", p => files.Add(new FileSpec(p)) },
                { "u|url:", "url of the remote file", u => files.Add(new FileSpec(new Uri(u))) },

                { "?|h|help", "Display this help", h => help = h != null },
            };

            var extraArgs = options.Parse(args);

            if (args.Length == 1 && help)
                return ShowHelp(options);

            // we never do inherited configs updating since that would be 
            // potentially touching all over the machine. 
            var config = Config.FromFile(Config.FileName);
            var command = extraArgs[0].ToLowerInvariant() switch
            {
                "changes" => new ChangesCommand(config),
                "delete" => new DeleteCommand(config),
                "download" => new DownloadCommand(config),
                "list" => new ListCommand(config),
                "update" => new UpdateCommand(config),
                _ => Command.NullCommand,
            };

            command.Files.AddRange(files);
            // Add remainder arguments as if they were just files or urls provided 
            // to the command. Allows skipping the -f|-u switches.
            command.Files.AddRange(extraArgs.Skip(1).Select(x =>
                Uri.TryCreate(extraArgs[1], UriKind.Absolute, out var uri) ?
                new FileSpec(uri) : new FileSpec(x)));

            return await command.ExecuteAsync();
        }

        static int ShowHelp(OptionSet options)
        {
            Console.WriteLine($"Usage: dotnet {ThisAssembly.Metadata.AssemblyName} [changes|delete|download|list|update] [file|url]* [options]");
            options.WriteOptionDescriptions(Console.Out);
            return 0;
        }
    }
}
