using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColoredConsole;
using DotNetConfig;

namespace Microsoft.DotNet
{
    class InitCommand : Command
    {
        public InitCommand(Config configuration) : base(configuration) { }

        public override async Task<int> ExecuteAsync()
        {
            if (Files.Count == 0)
            {
                ColorConsole.Write("Init requires at least one URI to fetch initial .netconfig from.");
                return 0;
            }

            var configs = new List<string>();
            var result = 0;

            // First download all the temp configs
            foreach (var spec in Files)
            {
                var tempConfig = Path.GetTempFileName();
                var tempFile = Path.GetTempFileName();
                var command = new AddCommand(Config.Build(tempConfig));
                command.Files.Add(new FileSpec(tempFile, spec.Uri));

                ColorConsole.WriteLine("Downloading seed config file(s)...".Yellow());
                if (await command.ExecuteAsync() != 0)
                    result = -1;

                configs.Add(tempFile);
            }

            // Then merge with the current config
            foreach (var entry in configs.SelectMany(x => Config.Build(x)).Where(x => x.Level == null))
            {
                // Internally, setting a null string is actually valid. Maybe reflect that in the API too?
                Configuration.SetString(entry.Section, entry.Subsection, entry.Variable, entry.RawValue!);
            }

            foreach (var config in configs.Select(x => Config.Build(x)))
            {
                // Process each downloaded .netconfig as a source of files 
                var files = new UpdateCommand(config).GetConfiguredFiles();
                // And update them against the current dir config.
                var update = new UpdateCommand(Config.Build());
                update.Files.AddRange(files);

                if (await update.ExecuteAsync() != 0)
                    result = -1;
            }

            return result;
        }
    }
}
