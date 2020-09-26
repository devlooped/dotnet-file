using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket;
using Microsoft.Git.CredentialManager;

namespace Microsoft.DotNet
{
    class BitbucketAuthHandler : AuthHandler
    {
        ICredential? credential;

        public BitbucketAuthHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var creds = await GetCredentialAsync(request.RequestUri);
            var builder = new UriBuilder(request.RequestUri);
            builder.UserName = "x-token-auth";
            builder.Password = creds.Password;

            // Reissue the request
            var retry = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            foreach (var etag in request.Headers.IfNoneMatch)
            {
                retry.Headers.IfNoneMatch.Add(etag);
            }

            return await base.SendAsync(retry, cancellationToken);
        }

        async Task<ICredential> GetCredentialAsync(Uri uri)
        {
            if (credential != null)
                return credential;

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "bitbucket.org",
                ["path"] = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped),
            });

            var provider = new BitbucketHostProvider(new CommandContext());

            credential = await provider.GetCredentialAsync(input);
            return credential;
        }
    }
}
