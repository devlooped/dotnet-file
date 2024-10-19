extern alias Devlooped;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitHub;

namespace Devlooped.Http;

class GitHubAuthHandler(HttpMessageHandler inner) : AuthHandler(inner)
{
    ICredential? credential;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
            return await base.SendAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound &&
            request.RequestUri.Host.StartsWith("raw") &&
            (request.RequestUri.PathAndQuery.Contains("/tree/") || request.Headers.Referrer?.PathAndQuery.Contains("/tree/") == true))
            // These will always fail, don't try to auth in particular.
            return response;

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var creds = await GetCredentialAsync(request.RequestUri);
            if (creds == null)
                return response;

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

        return response;
    }

    async Task<ICredential?> GetCredentialAsync(Uri? uri)
    {
        if (credential != null)
            return credential;

        if (uri != null &&
            uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries) is { Length: >= 2 } parts)
        {
            // Try using GCM via API first to retrieve creds
            var store = Devlooped::GitCredentialManager.CredentialManager.Create();
            if (GetCredential(store, $"https://github.com/{parts[0]}/{parts[1]}") is { } repo)
                return repo;
            else if (GetCredential(store, $"https://github.com/{parts[0]}") is { } owner)
                return owner;
            else if (GetCredential(store, "https://github.com") is { } global)
                return global;
        }

        var input = new InputArguments(new Dictionary<string, string>
        {
            ["protocol"] = "https",
            ["host"] = "github.com",
        });

        var provider = new GitHubHostProvider(new CommandContext());

        try
        {
            credential = await provider.GetCredentialAsync(input);
            return credential;
        }
        catch (Exception)
        {
            return null;
        }
    }

    ICredential? GetCredential(Devlooped::GitCredentialManager.ICredentialStore store, string url)
    {
        var accounts = store.GetAccounts(url);
        if (accounts.Count == 1)
        {
            var creds = store.Get(url, accounts[0]);
            if (creds != null)
            {
                credential = new Credential(accounts[0], creds.Password);
                return credential;
            }
        }
        return default;
    }

    record Credential(string Account, string Password) : ICredential;
}
