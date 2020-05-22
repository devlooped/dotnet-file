using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.DotNet
{
    class GitHubAuthHandler : AuthHandler
    {
        ICredential? credential;

        public GitHubAuthHandler(HttpMessageHandler inner) : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            // For raw downloads, we need to use raw.githubusercontent.com instead. For example:
            if (request.RequestUri.Host.Equals("github.com"))
            {
                // https://github.com/kzu/dotnet-file/raw/master/README.md
                // https://github.com/kzu/dotnet-file/blob/master/README.md
                // => 
                // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md

                var parts = request.RequestUri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
                uri = new Uri(new Uri("https://raw.githubusercontent.com/"), string.Join('/',
                    parts.Take(2).Concat(parts.Skip(3))));
            }

            var creds = await GetCredentialAsync();
            var builder = new UriBuilder(uri);
            builder.UserName = creds.Password;
            builder.Password = "x-auth-basic";

            // Reissue the request
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

            var store = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                (ICredentialStore)WindowsCredentialManager.Open() :
                (ICredentialStore)MacOSKeychain.Open();

            var provider = new GitHubHostProvider(new CommandContext(store));

            credential = await provider.GetCredentialAsync(input);
            return credential;
        }
    }
}
