using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitHub;

namespace Devlooped
{
    class GitHubAuthHandler : AuthHandler
    {
        ICredential? credential;

        public GitHubAuthHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null)
                return await base.SendAsync(request, cancellationToken);

            var creds = await GetCredentialAsync();
            var builder = new UriBuilder(request.RequestUri)
            {
                UserName = creds.Password,
                Password = "x-auth-basic"
            };

            // retry the request
            var retry = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            retry.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(creds.Password)));
            foreach (var etag in request.Headers.IfNoneMatch)
            {
                retry.Headers.IfNoneMatch.Add(etag);
            }

            return await base.SendAsync(retry, cancellationToken);
        }

        async Task<ICredential> GetCredentialAsync()
        {
            if (credential != null)
                return credential;

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new CommandContext());

            credential = await provider.GetCredentialAsync(input);
            return credential;
        }
    }
}
