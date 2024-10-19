using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using Microsoft.AzureRepos;

namespace Devlooped.Http;

class AzureRepoAuthHandler(HttpMessageHandler inner) : AuthHandler(inner)
{
    ICredential? credential;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
            return await base.SendAsync(request, cancellationToken);

        var creds = await GetCredentialAsync(request.RequestUri);
        if (creds == null)
            return await base.SendAsync(request, cancellationToken);

        var retry = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        retry.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(creds.Password)));
        foreach (var etag in request.Headers.IfNoneMatch)
        {
            retry.Headers.IfNoneMatch.Add(etag);
        }

        return await base.SendAsync(retry, cancellationToken);
    }

    async Task<ICredential?> GetCredentialAsync(Uri uri)
    {
        if (credential != null)
            return credential;

        var input = new InputArguments(new Dictionary<string, string>
        {
            ["protocol"] = "https",
            ["host"] = "dev.azure.com",
            ["path"] = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped),
        });

        var provider = new AzureReposHostProvider(new CommandContext());
        credential = await provider.GetCredentialAsync(input);

        return credential;
    }
}
