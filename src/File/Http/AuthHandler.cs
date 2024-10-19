using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped.Http;

interface IAuthHandler
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

abstract class AuthHandler(HttpMessageHandler inner) : DelegatingHandler(inner), IAuthHandler
{
    Task<HttpResponseMessage> IAuthHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => SendAsync(request, cancellationToken);
}
