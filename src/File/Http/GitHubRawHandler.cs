using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped
{
    /// <summary>
    /// Converts URLs for github.com files into raw.githubusercontent.com.
    /// </summary>
    class GitHubRawHandler : DelegatingHandler
    {
        public GitHubRawHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host.Equals("github.com") != true)
                return await base.SendAsync(request, cancellationToken);

            var parts = request.RequestUri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Ensure we only process URIs that have more than just org/repo
            if (parts.Length <= 2)
                return await base.SendAsync(request, cancellationToken);

            // Try to retrieve the commit for the entry
            // Some day it may be available in the response headers directly: https://support.github.com/ticket/personal/0/1035411
            if (GitHub.IsInstalled(out var _) &&
                GitHub.TryApi($"repos/{parts[0]}/{parts[1]}/contents/{string.Join('/', parts.Skip(4))}", out var json) &&
                    json != null)
            {
                dynamic metadata = json;
                if (metadata.type == "file")
                {
                    string sha = metadata.sha;
                    string url = metadata.download_url;

                    request.RequestUri = new Uri(url);

                    var result = await base.SendAsync(request, cancellationToken);

                    result.Headers.TryAddWithoutValidation("X-Sha", sha);

                    return result;
                }
            }
            else
            {
                // https://github.com/kzu/dotnet-file/raw/master/README.md
                // https://github.com/kzu/dotnet-file/blob/master/README.md
                // => 
                // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md

                request.RequestUri = new Uri(new Uri("https://raw.githubusercontent.com/"), string.Join('/',
                    parts.Take(2).Concat(parts.Skip(3))));
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
