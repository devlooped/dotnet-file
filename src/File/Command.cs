using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped;

abstract class Command(Config configuration)
{
    public List<FileSpec> Files { get; } = [];

    public Config Configuration { get; protected set; } = configuration;

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

    /// <summary>
    /// Gets the path of the configured file entry that downloads from the given URL, 
    /// which may differ from the default path the URL maps to when the user moved it.
    /// </summary>
    internal static string? GetConfiguredPath(Config configuration, Uri url)
        => configuration
            .FirstOrDefault(x => x.Section == "file" && x.Subsection != null && x.Variable == "url" &&
                url.ToString().Equals(x.RawValue, StringComparison.OrdinalIgnoreCase))
            ?.Subsection;

    /// <summary>
    /// Replaces the path of each file with the path of the configured entry that 
    /// downloads from the same URL, if any, so its settings (like skip) are honored.
    /// </summary>
    internal IEnumerable<FileSpec> WithConfiguredPaths(IEnumerable<FileSpec> files)
        => files.Select(file =>
            file.Uri != null && GetConfiguredPath(Configuration, file.Uri) is string path && path != file.Path
                ? new FileSpec(path, file.Uri)
                : file);

    public static Command NullCommand { get; } =
        new NoOpCommand(Config.Build(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

    class NoOpCommand(Config configuration) : Command(configuration)
    {
        public override Task<int> ExecuteAsync() => Task.FromResult(0);
    }
}
