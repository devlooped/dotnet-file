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
            string? path = default;

            var options = new OptionSet
            {
                { "p|path:", "path of file to download, update or delete", p => { path = p; files.Add(new FileSpec(p)); } },
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
                "download" => new DownloadCommand(config),
                "update" => new UpdateCommand(config),
                "delete" => new DeleteCommand(config),
                "list" => new ListCommand(config),
                _ => Command.NullCommand,
            };

            command.Files.AddRange(files);

            switch (command)
            {
                case DownloadCommand download:
                    if (path != null && files.Count == 1)
                    {
                        files[0] = new FileSpec(path, files[0].Uri);
                    }
                    else if (path != null && files.Count > 1)
                    {
                        Console.WriteLine("Cannot provide path when downloading multiple files.");
                        return 1;
                    }
                    else if (extraArgs.Count > 1 && Uri.TryCreate(extraArgs[1], UriKind.Absolute, out var uri))
                    {
                        download.Files.Add(new FileSpec(uri));
                    }
                    return await download.ExecuteAsync();
                default:
                    return await command.ExecuteAsync();
            }
        }

        static int ShowHelp(OptionSet options)
        {
            Console.WriteLine($"Usage: {ThisAssembly.Metadata.AssemblyName} [download|update|delete|list] [options]");
            options.WriteOptionDescriptions(Console.Out);
            return 0;
        }
    }
}
