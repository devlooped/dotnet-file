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
    class AzureReposRawHandler : DelegatingHandler
    {
        public AzureReposRawHandler(HttpMessageHandler inner) : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // For raw downloads, we need to use raw.githubusercontent.com instead. For example:
            if (request.RequestUri.Host == "dev.azure.com" || request.RequestUri.Host.EndsWith("visualstudio.com"))
            {
                var parts = request.RequestUri
                    .GetComponents(UriComponents.Path, UriFormat.Unescaped)
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                string organization;
                string project;
                if (request.RequestUri.Host.EndsWith("visualstudio.com"))
                {
                    organization = request.RequestUri.Host.Substring(0, request.RequestUri.Host.IndexOf('.'));
                    project = parts[0];
                }
                else
                {
                    organization = parts[0];
                    project = parts[1];
                }

                var queryString = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var path = queryString["path"];
                var version = queryString["version"];
                if (string.IsNullOrEmpty(version))
                    version = "master";
                else
                    version = version.Substring(2); // Skip GB (git branch) and GC (git commit) prefix.

                var repository = parts.SkipWhile(x => x != "_git").Skip(1).FirstOrDefault();

                var url = $"https://dev.azure.com/{organization}/{project}/_apis/sourceProviders/Git/filecontents?&repository={repository}&commitOrBranch={version}&path={path}&api-version=5.1-preview.1";

                request.RequestUri = new Uri(url);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
