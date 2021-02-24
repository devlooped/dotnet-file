using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped
{
    abstract class Command
    {
        public List<FileSpec> Files { get; } = new List<FileSpec>();

        protected Command(Config configuration) => Configuration = configuration;

        public Config Configuration { get; }

        public abstract Task<int> ExecuteAsync();

        protected internal IEnumerable<FileSpec> GetConfiguredFiles()
        {
            foreach (var file in Configuration.Where(x => x.Section == "file").GroupBy(x => x.Subsection))
            {
                if (Configuration.GetBoolean("file", file.Key, "skip") == true)
                    continue;

                // If no subsection exists, this is a glob-like URL (like a repo root or dir from GH)
                // In this case, there can be many URLs, but there will never be an etag
                if (file.Key == null)
                {
                    foreach (var entry in Configuration.GetAll("file", "url").Where(x => x.RawValue != null))
                    {
                        yield return new FileSpec(new Uri(entry.GetString()));
                    }
                }
                else
                {
                    // If there is a subsection, we might still get multiple URLs for the case where 
                    // the subsection is actually a target folder where multiple glob URLs are to be 
                    // downloaded (i.e. "docs" with multiple GH URLs for docs subdirs)
                    var urls = Configuration
                        .GetAll("file", file.Key, "url")
                        .Where(x => x.RawValue != null)
                        .Select(x => x.GetString())
                        .ToArray();

                    if (urls.Length > 1)
                    {
                        foreach (var url in urls)
                        {
                            yield return new FileSpec(file.Key, new Uri(url));
                        }
                    }
                    else if (urls.Length == 1)
                    {
                        // Default case is there's a single URL, so we might have an etag in that case, 
                        // and optionally a SHA too.
                        yield return new FileSpec(file.Key,
                            new Uri(urls[0]),
                            Configuration.GetString("file", file.Key, "etag"),
                            Configuration.GetString("file", file.Key, "sha"));
                    }
                }
            }
        }

        public static Command NullCommand { get; } =
            new NoOpCommand(Config.Build(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

        class NoOpCommand : Command
        {
            public NoOpCommand(Config configuration) : base(configuration) { }

            public override Task<int> ExecuteAsync() => Task.FromResult(0);
        }
    }
}
