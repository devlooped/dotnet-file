using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColoredConsole;
using DotNetConfig;

namespace Devlooped;

class SyncCommand(Config configuration) : UpdateCommand(configuration)
{
    /// <summary>
    /// Optional URL to a remote .netconfig whose skip=true entries are merged into
    /// the local config before the sync runs (local entries always take precedence).
    /// </summary>
    public string? InitUrl { get; set; }

    public override async Task<int> ExecuteAsync()
    {
        if (InitUrl != null)
        {
            var tempConfig = Path.GetTempFileName();
            var tempFile = Path.GetTempFileName();
            try
            {
                var addCmd = new AddCommand(Config.Build(tempConfig));
                addCmd.Files.Add(new FileSpec(tempFile, new Uri(InitUrl)));

                ColorConsole.WriteLine("Downloading init config file(s)...".Yellow());
                await addCmd.ExecuteAsync();

                var remoteConfig = Config.Build(tempFile);
                var localConfig = Configuration;

                foreach (var group in remoteConfig
                    .Where(x => x.Level == null && x.Section == "file" && x.Subsection != null)
                    .GroupBy(x => x.Subsection!))
                {
                    var path = group.Key;
                    if (remoteConfig.GetBoolean("file", path, "skip") != true)
                        continue;

                    // Local entry takes precedence — only add skip if no local entry exists
                    if (localConfig.Where(x => x.Section == "file" && x.Subsection == path).Any())
                        continue;

                    var url = remoteConfig.GetString("file", path, "url");
                    if (url != null)
                        localConfig = localConfig.SetString("file", path, "url", url);

                    localConfig = localConfig.SetBoolean("file", path, "skip", true);
                }
            }
            finally
            {
                File.Delete(tempConfig);
                File.Delete(tempFile);
            }
        }

        return await base.ExecuteAsync();
    }

    protected override bool OnRemoteUrlMissing(FileSpec spec)
    {
        // If the file exists locally, delete it. Remove the config entry.
        if (File.Exists(spec.Path))
            File.Delete(spec.Path);

        // Clear empty directories
        var dir = new FileInfo(spec.Path).DirectoryName;
        DeleteEmptyDirectories(dir);

        Configuration.RemoveSection("file", spec.Path);

        return true;
    }

    static void DeleteEmptyDirectories(string? dir)
    {
        if (dir != null && !Directory.EnumerateFiles(dir).Any() && !Directory.EnumerateDirectories(dir).Any())
        {
            var parent = new DirectoryInfo(dir).Parent?.FullName;
            Directory.Delete(dir);
            DeleteEmptyDirectories(parent);
        }
    }
}
