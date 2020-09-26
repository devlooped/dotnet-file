using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Http
{
    /// <summary>
    /// Converts URLs for dev.azure.com files into the corresponding API call.
    /// </summary>
    class AzureRepoRawHandler : DelegatingHandler
    {
        public AzureRepoRawHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // For raw downloads, we need to use raw.githubusercontent.com instead. For example:
            if (request.RequestUri.Host.Equals("dev.azure.com"))
            {
                // https://dev.azure.com/kzu/spikes/_git/private?path=%2FREADME.md&version=GBmaster&_a=preview
                // => 
                // https://dev.azure.com/kzu/spikes/_apis/git/repositories/private/items?path=/README.md&versionDescriptor[versionOptions]=0&versionDescriptor[versionType]=0&versionDescriptor[version]=dev&
                var uri = request.RequestUri;

                var paths = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (paths.Length == 4)
                {
                    var builder = new StringBuilder();
                    builder.Append($"https://dev.azure.com/{paths[0]}/{paths[1]}/_apis/git/repositories/{paths[3]}/items?");

                    var args = uri.GetComponents(UriComponents.Query, UriFormat.Unescaped)
                        .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Split('='))
                        .Where(x => x.Length == 2)
                        .ToDictionary(x => x[0], x => x[1]);

                    // &versionDescriptor[versionOptions]=0&versionDescriptor[versionType]=0&resolveLfs=true&$format=octetStream&api-version=5.0&download=true
                    if (args.TryGetValue("path", out var path))
                    {
                        builder.Append("path=" + path);
                        if (args.TryGetValue("version", out var branch))
                        {
                            builder.Append("&versionDescriptor[version]=")
                                .Append(branch.StartsWith("GB") ? branch.Substring(2) : branch);
                        }

                        builder.Append("&versionDescriptor[versionOptions]=0&versionDescriptor[versionType]=0&resolveLfs=true&$format=octetStream&api-version=5.0&downloadtrue");
                        request.RequestUri = new Uri(builder.ToString());
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
