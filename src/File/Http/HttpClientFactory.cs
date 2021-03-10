﻿using System.Net;
using System.Net.Http;
using Devlooped.Http;

namespace Devlooped
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
#if NETCOREAPP2_1
                                        DecompressionMethods.GZip
#else
                                        DecompressionMethods.Brotli | DecompressionMethods.GZip
#endif

                                })))));
    }
}
