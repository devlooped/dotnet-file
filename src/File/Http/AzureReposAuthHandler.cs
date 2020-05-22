using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub;
using Microsoft.AzureRepos;
using Microsoft.Git.CredentialManager;

namespace Microsoft.DotNet
{
    class AzureReposAuthHandler : AuthHandler
    {
        ICredential? credential;

        public AzureReposAuthHandler(HttpMessageHandler inner) : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var creds = await GetCredentialAsync(request.RequestUri);
            //var builder = new UriBuilder(request.RequestUri);
            //builder.UserName = "x-auth-basic";
            //builder.Password = creds.Password;

            // retry the request
            var retry = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
            retry.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(creds.Password)));
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
                ["host"] = uri.Host,
                ["path"] = string.Join('/', uri
                    .GetComponents(UriComponents.Path, UriFormat.Unescaped)
                    .Split('/', StringSplitOptions.RemoveEmptyEntries).Take(2)),
            });

            var provider = new AzureReposHostProvider(new CommandContext());
            await provider.StoreCredentialAsync(input);

            credential = await provider.GetCredentialAsync(input);
            return credential;
        }
    }
}
