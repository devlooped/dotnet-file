using System;
using System.IO;
using System.Linq;
using DotNetConfig;
using Xunit;

namespace Devlooped;

public class ConfiguredPathTests : IDisposable
{
    const string PropsUrl = "https://github.com/devlooped/oss/blob/main/src/Directory.Build.props";
    const string SnkUrl = "https://github.com/devlooped/oss/blob/main/src/kzu.snk";

    readonly string localPath = Path.GetTempFileName();
    readonly string remotePath = Path.GetTempFileName();

    public void Dispose()
    {
        File.Delete(localPath);
        File.Delete(remotePath);
    }

    [Fact]
    public void RepoFilePreservesMovedPath()
    {
        var config = Config.Build(localPath).SetString("file", "dotnet/Directory.Build.props", "url", PropsUrl);
        var command = new SyncCommand(config);

        var files = command.WithConfiguredPaths(
        [
            new FileSpec("src/Directory.Build.props", new Uri(PropsUrl)),
            new FileSpec("src/nuget.config", new Uri("https://github.com/devlooped/oss/blob/main/src/nuget.config")),
        ]).ToList();

        Assert.Equal(["dotnet/Directory.Build.props", "src/nuget.config"], files.Select(x => x.Path));
        Assert.Equal(PropsUrl, files[0].Uri?.ToString());
    }

    [Fact]
    public void InitSkipNotAddedForMovedPath()
    {
        var local = Config.Build(localPath)
            .SetString("file", "dotnet/kzu.snk", "url", SnkUrl)
            .SetBoolean("file", "dotnet/kzu.snk", "skip", true);
        var remote = Config.Build(remotePath)
            .SetString("file", "src/kzu.snk", "url", SnkUrl)
            .SetBoolean("file", "src/kzu.snk", "skip", true);

        var merged = InitCommand.MergeSkips(local, remote);

        Assert.DoesNotContain(merged, x => x.Section == "file" && x.Subsection == "src/kzu.snk");
    }

    [Fact]
    public void InitSkipAddedForUnconfiguredUrl()
    {
        var local = Config.Build(localPath);
        var remote = Config.Build(remotePath)
            .SetString("file", "src/kzu.snk", "url", SnkUrl)
            .SetBoolean("file", "src/kzu.snk", "skip", true);

        var merged = InitCommand.MergeSkips(local, remote);

        Assert.True(merged.GetBoolean("file", "src/kzu.snk", "skip"));
        Assert.Equal(SnkUrl, merged.GetString("file", "src/kzu.snk", "url"));
    }
}
