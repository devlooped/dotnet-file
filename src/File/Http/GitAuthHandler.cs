using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    /// <summary>
    /// Middleware to handle authentication to requested URLs using GCM 
    /// automatically for supported URL hosts.
    /// </summary>
    class GitAuthHandler : DelegatingHandler
    {
        readonly Dictionary<string, IAuthHandler> authHandlers;

        public GitAuthHandler(HttpMessageHandler inner) : base(inner)
        {
            authHandlers = new Dictionary<string, IAuthHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "github.com", new GitHubAuthHandler(InnerHandler) },
                { "raw.githubusercontent.com", new GitHubAuthHandler(InnerHandler) },
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if ((response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden) && 
                authHandlers.TryGetValue(request.RequestUri.Host, out var handler))
            {
                return await handler.SendAsync(request, cancellationToken);
            }

            return response;
        }
    }
}
