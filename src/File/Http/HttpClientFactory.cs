using System.Net.Http;

namespace Microsoft.DotNet
{
    static class HttpClientFactory
    {
        public static HttpClient Create() 
            => new HttpClient(
                new GitAuthHandler(
                    new GitHubRawHandler(
                        new HttpClientHandler { AllowAutoRedirect = false })));
    }
}
