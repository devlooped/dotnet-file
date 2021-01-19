using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped
{
    interface IAuthHandler
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    abstract class AuthHandler : DelegatingHandler, IAuthHandler
    {
        public AuthHandler(HttpMessageHandler inner) : base(inner)
        {
        }

        Task<HttpResponseMessage> IAuthHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendAsync(request, cancellationToken);
    }
}
