using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    abstract class Command
    {
        public List<FileSpec> Files { get; } = new List<FileSpec>();

        protected Command(Config configuration) => Configuration = configuration;

        public Config Configuration { get; }

        public abstract Task<int> ExecuteAsync();

        protected IEnumerable<FileSpec> GetConfiguredFiles()
        {
            // Treat the github-based repo urls as filespecs too.
            foreach (var file in Configuration.Where(x => x.Section == "file.github" && x.Name == "url" && !string.IsNullOrEmpty(x.Value)))
            {
                if (file.Subsection == null)
                    yield return new FileSpec(new Uri(file.Value!));
                else
                    new FileSpec(file.Subsection, new Uri(file.Value!));
            }

            foreach (var file in Configuration.Where(x => x.Section == "file" && x.Subsection != null).GroupBy(x => x.Subsection))
            {
                var url = Configuration.GetString("file", file.Key, "url");
                yield return new FileSpec(file.Key!,
                    url == null ? null : new Uri(url),
                    Configuration.GetString("file", file.Key, "etag"));
            }
        }

        public static Command NullCommand { get; } =
            new NoOpCommand(Config.FromFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

        class NoOpCommand : Command
        {
            public NoOpCommand(Config configuration) : base(configuration) { }

            public override Task<int> ExecuteAsync() => Task.FromResult(0);
        }
    }
}
