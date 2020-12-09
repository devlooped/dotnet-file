using System.Net;
using System.Net.Http;
using Microsoft.DotNet.Http;

namespace Microsoft.DotNet
{
    static class HttpClientFactory
    {
        public static HttpClient Create()
            => new HttpClient(
                new GitAuthHandler(
                    new GitHubRawHandler(
                        new BitbucketRawHandler(
                            new AzureRepoRawHandler(
                                new HttpClientHandler
                                {
                                    AllowAutoRedirect = false,
                                    AutomaticDecompression =
#if NETCOREAPP21
                                        DecompressionMethods.GZip
#else
                                        DecompressionMethods.Brotli | DecompressionMethods.GZip
#endif

                                })))));
    }
}
