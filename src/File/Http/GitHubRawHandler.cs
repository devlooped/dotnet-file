using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

            // Ensure we only process URIs that have at least org/repo
            if (parts.Length < 2)
                return await base.SendAsync(request, cancellationToken);

            // https://github.com/kzu/dotnet-file/raw/master/README.md
            // https://github.com/kzu/dotnet-file/blob/master/README.md
            // => 
            // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md

            // NOTE: we WILL make a raw URL for top-level org/repo URLs too, causing a 
            // BadRequest or NotFound which is REQUIRED for AddCommand to detect and 
            // fallback to a gh CLI call, so DO NOT change that behavior here.

            request.RequestUri = new Uri(new Uri("https://raw.githubusercontent.com/"), string.Join('/',
                parts.Take(2).Concat(parts.Skip(3))));

            var response = await base.SendAsync(request, cancellationToken);

            // Try to retrieve the commit for the entry
            // Some day it may be available in the response headers directly: https://support.github.com/ticket/personal/0/1035411
            if (response.IsSuccessStatusCode &&
                parts.Length > 2 &&
                GitHub.IsInstalled &&
                GitHub.TryApi($"repos/{parts[0]}/{parts[1]}/commits?per_page=1&path={string.Join('/', parts.Skip(4))}", out var json) &&
                json is JArray commits &&
                commits[0] is JObject obj &&
                obj.Property("sha") is JProperty prop &&
                prop != null &&
                prop.Value.Type == JTokenType.String)
            {
                response.Headers.TryAddWithoutValidation("X-Sha", prop.Value.ToObject<string>());
            }

            return response;
        }
    }
}
