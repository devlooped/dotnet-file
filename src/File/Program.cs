using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var result = ProgramArguments.CreateParser()
                                         .WithVersion(ThisAssembly.Info.Version)
                                         .Parse(args);

            return await result.Match(RunAsync,
                                      r => PrintAsync(Console.Out, r.Help.Replace("dotnet-file", "dotnet file")),
                                      r => PrintAsync(Console.Out, r.Version),
                                      r => PrintAsync(Console.Error, r.Usage));

            static Task<int> PrintAsync(TextWriter writer, string message, int exitCode = 0)
            {
                writer.WriteLine(message);
                return Task.FromResult(exitCode);
            }
        }

        static async Task<int> RunAsync(ProgramArguments args)
        {
            var config = Config.Build();
            var command = args switch
            {
                { CmdAdd    : true } => new AddCommand(config),
                { CmdChanges: true } => new ChangesCommand(config),
                { CmdDelete : true } => new DeleteCommand(config),
                { CmdInit   : true } => new InitCommand(config),
                { CmdList   : true } => new ListCommand(config),
                { CmdSync   : true } => new SyncCommand(config),
                { CmdUpdate : true } => new UpdateCommand(config),
                _ => Command.NullCommand,
            };

            if (command == Command.NullCommand)
            {
                Console.Error.WriteLine(ProgramArguments.Usage);
                return 1;
            }

            // Add remainder arguments as if they were just files or urls provided 
            // to the command. Allows skipping the -f|-u switches.
            var files = new List<FileSpec>();
            using (var fileOrUrl = args.ArgFileOrUrl.GetEnumerator())
            {
                // Try to pair Uri+File to allow intuitive download>path mapping, such as 
                // https://gitub.com/org/repo/docs/file.md docs/file.md
                if (Uri.TryCreate(fileOrUrl.Current, UriKind.Absolute, out var uri))
                {
                    // If the next arg is not a URI, use that as the file path for the uri
                    if (fileOrUrl.MoveNext() && !Uri.TryCreate(fileOrUrl.Current, UriKind.Absolute, out _))
                    {
                        files.Add(FileSpec.WithPath(fileOrUrl.Current, uri));
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
            }

            command.Files.AddRange(files);

            var result = await command.ExecuteAsync();

            // If there were changes and a changelog was requested, emit it
            // to a file.
            if (args.OptChangelog is var changelog &&
                command is AddCommand add &&
                add.Changes.Count > 0 &&
                GitHub.IsInstalled)
            {
                GitHub.WriteChanges(changelog, add.Changes);
            }

            return result;
        }
    }
}
