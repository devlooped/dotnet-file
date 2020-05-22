using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    /// <summary>
    /// Converts URLs for github.com files into raw.githubusercontent.com.
    /// </summary>
    class GitHubRawHandler : DelegatingHandler
    {
        public GitHubRawHandler(HttpMessageHandler inner) : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // For raw downloads, we need to use raw.githubusercontent.com instead. For example:
            if (request.RequestUri.Host.Equals("github.com"))
            {
                // https://github.com/kzu/dotnet-file/raw/master/README.md
                // https://github.com/kzu/dotnet-file/blob/master/README.md
                // => 
                // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md

                var parts = request.RequestUri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
                request.RequestUri = new Uri(new Uri("https://raw.githubusercontent.com/"), string.Join('/',
                    parts.Take(2).Concat(parts.Skip(3))));
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
