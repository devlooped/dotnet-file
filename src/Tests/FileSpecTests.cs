using System;
using Xunit;

namespace Devlooped
{
    public class FileSpecTests
    {
        [Theory]
        [InlineData("https://github.com/devlooped/dotnet-file/tree/main/src", "src/")]
        [InlineData("https://github.com/devlooped/dotnet-file/blob/main/src/Directory.props", "src/Directory.props")]
        [InlineData("https://github.com/devlooped/dotnet-file/raw/main/src/Directory.props", "src/Directory.props")]
        [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md", "README.md")]
        [InlineData("https://raw.githubusercontent.com/kzu/dotnet-file/master/docs/img/icon.png", "docs/img/icon.png")]
        public void CalculateDefaultPath(string url, string path)
        {
            var spec = FileSpec.WithDefaultPath(new Uri(url));

            Assert.Equal(spec.Path, path);
        }
    }
}
