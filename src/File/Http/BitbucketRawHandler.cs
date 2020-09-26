using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    /// <summary>
    /// Converts URLs for github.com files into raw.githubusercontent.com.
    /// </summary>
    class BitbucketRawHandler : DelegatingHandler
    {
        public BitbucketRawHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // For raw downloads, we need to use raw.githubusercontent.com instead. For example:
            if (request.RequestUri.Host.Equals("bitbucket.com"))
            {
                // https://bitbucket.org/kzu/public/src/master/README.md
                // => 
                // https://bitbucket.org/kzu/public/raw/master/README.md

                var parts = request.RequestUri
                    .GetComponents(UriComponents.Path, UriFormat.Unescaped)
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 3 && parts[2].Equals("src", StringComparison.OrdinalIgnoreCase))
                {
                    parts[2] = "raw";
                    var builder = new UriBuilder(request.RequestUri);
                    builder.Path = string.Join('/', parts);
                    request.RequestUri = builder.Uri;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
