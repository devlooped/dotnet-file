using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped.Http;

/// <summary>
/// Middleware to handle authentication to requested URLs using GCM 
/// automatically for supported URL hosts.
/// </summary>
class GitAuthHandler : DelegatingHandler
{
    readonly Dictionary<string, IAuthHandler> authHandlers;

    public GitAuthHandler(HttpMessageHandler inner) : base(inner)
    {
        // Authentication handlers use a new client handler that does 
        // allow redirection post-auth.
        var handler = new HttpClientHandler();
        authHandlers = new Dictionary<string, IAuthHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "github.com", new GitHubAuthHandler(handler) },
            { "raw.githubusercontent.com", new GitHubAuthHandler(handler) },
            { "bitbucket.org", new BitbucketAuthHandler(handler) },
            { "dev.azure.com", new AzureRepoAuthHandler(handler) },
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && request.RequestUri != null)
        {
            if (!authHandlers.TryGetValue(request.RequestUri.Host, out var handler))
                handler = authHandlers.Where(x => request.RequestUri.Host.EndsWith(x.Key)).Select(x => x.Value).FirstOrDefault();

            if (handler != null)
                return await handler.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
