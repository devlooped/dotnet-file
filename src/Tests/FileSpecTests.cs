using System;
using Xunit;

namespace Devlooped;

public class FileSpecTests
{
    [Theory]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/src/Directory.props", "src/Directory.props")]
    [InlineData("https://github.com/devlooped/dotnet-file/raw/main/src/Directory.props", "src/Directory.props")]
    [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md", "README.md")]
    [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/docs/img/icon.png", "docs/img/icon.png")]
    [InlineData("https://github.com/devlooped/dotnet-file/tree/main/src", "src/")]
    public void CalculateDefaultPath(string url, string path)
    {
        var spec = FileSpec.WithDefaultPath(new Uri(url));

        Assert.Equal(spec.Path, path.TrimEnd('/'));
    }

    [Theory]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/src/Directory.props", "docs/", "docs/src/Directory.props")]
    [InlineData("https://github.com/devlooped/dotnet-file/raw/main/src/Directory.props", "external/docs/", "external/docs/src/Directory.props")]
    [InlineData("https://github.com/devlooped/dotnet-file/raw/main/src/Directory.props", "./", "src/Directory.props")]
    [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md", "docs/src/", "docs/src/README.md")]
    [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/docs/img/icon.png", "external/", "external/docs/img/icon.png")]
    [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/docs/img/icon.png", "./external/", "external/docs/img/icon.png")]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/.editorconfig", "./", ".editorconfig")]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/.editorconfig", "./src/", "src/.editorconfig")]
    [InlineData("https://github.com/devlooped/dotnet-file/tree/main/src//docs", "./common/", "common/src/docs")]
    [InlineData("https://github.com/devlooped/dotnet-file/tree/main/src", "external/", "external/src")]
    public void WhenPathIsDirAppendsDefaultPath(string url, string path, string expected)
    {
        var spec = new FileSpec(path, new Uri(url));

        Assert.Equal(expected, spec.Path);
    }

    [Fact]
    public void WhenSourceUriEndsInSlathThenTargetPathIsBaseDir()
    {
        var spec = new FileSpec("docs", new Uri("https://github.com/devlooped/dotnet-file/tree/main/src/api/documentation/"));

        // NOTE: the relative `src/api/documentation` is not appended to the file path.
        Assert.Equal("docs", spec.Path);
    }

    [Fact]
    public void WhenSpecAndSourceUriEndInSlathThenPreservesBaseDirSlash()
    {
        var spec = new FileSpec("docs/", new Uri("https://github.com/devlooped/dotnet-file/tree/main/src/api/documentation/"));

        // NOTE: the relative `src/api/documentation` is not appended to the file path.
        Assert.Equal("docs/", spec.Path);
    }

    [Theory]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/src/Foo.cs", ".", "Foo.cs")]
    [InlineData("https://github.com/devlooped/dotnet-file/blob/main/src/Foo.cs", "src/External/.", "src/External/Foo.cs")]
    [InlineData("https://github.com/devlooped/dotnet-file/tree/main/src", "src/External/.", "src/External/.")]
    public void WhenPathHasDot(string url, string path, string expected)
    {
        var spec = FileSpec.WithPath(path, new Uri(url));

        Assert.Equal(expected, spec.Path);
    }

    [Fact]
    public void WhenPathIsFinalDoesNotReplaceDot()
    {
        var spec = new FileSpec(new Uri("https://github.com/devlooped/oss/blob/main/.editorconfig"));

        Assert.Equal(".editorconfig", spec.Path);
    }
}
