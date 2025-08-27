using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Devlooped.Http;

/// <summary>
/// Converts URLs for github.com files into raw.githubusercontent.com.
/// </summary>
class GitHubRawHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host.Equals("github.com") != true)
            return await base.SendAsync(request, cancellationToken);

        var parts = request.RequestUri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Ensure we only process URIs that have at least org/repo
        if (parts.Length < 2)
            return await base.SendAsync(request, cancellationToken);

        // https://github.com/kzu/dotnet-file/raw/master/README.md
        // https://github.com/kzu/dotnet-file/blob/master/README.md
        // => 
        // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md

        // NOTE: we WILL make a raw URL for top-level org/repo URLs too, causing a 
        // BadRequest or NotFound which is REQUIRED for AddCommand to detect and 
        // fallback to a gh CLI call, so DO NOT change that behavior here.
        var rawUri = new Uri(new Uri("https://raw.githubusercontent.com/"), string.Join('/',
            parts.Take(2).Concat(parts.Skip(3))));

        // Move the original to the referer, so it can be used by other handlers.
        request.Headers.Referrer = request.RequestUri;
        request.RequestUri = rawUri;

        var response = await base.SendAsync(request, cancellationToken);

        var originalEtag = request.Headers.TryGetValues("X-ETag", out var etags) ? etags.FirstOrDefault() : null;
        var originalSha = request.Headers.TryGetValues("X-Sha", out var shas) ? shas.FirstOrDefault() : null;

        var newEtag = response.Headers.ETag?.Tag?.Trim('"');
        // Some day we may get the X-Sha directly from the response, see https://support.github.com/ticket/personal/0/1035411
        var newSha = response.Headers.TryGetValues("X-Sha", out shas) ? shas.FirstOrDefault() : null;

        // Try to retrieve the commit for the entry
        if (newSha == null &&
            response.IsSuccessStatusCode &&
            // original ETag might be null, for example
            // but if they are the same (same content therefore), we only request the new
            // sha if there wasn't one already, as an optimization to avoid retrieving it 
            // when we already have it persisted from a previous request
            (originalEtag != newEtag || originalSha == null) &&
            parts.Length > 2 &&
            // Skip raw|blob|tree part. we need at least 2 remaining items: ref+path
            parts[3..] is { Length: >= 2 } rest &&
            GitHub.IsInstalled)
        {
            // branch might have / as a separator
            // Try i parts for ref, remaining for path
            for (var i = 1; i < rest.Length; i++)
            {
                var refCandidate = string.Join('/', rest.Take(i));
                var pathCandidate = string.Join('/', rest.Skip(i));

                // Validate by asking for just 1 commit that touched this path on this ref
                var url = $"repos/{Uri.EscapeDataString(parts[0])}/{Uri.EscapeDataString(parts[1])}/commits" +
                          $"?sha={Uri.EscapeDataString(refCandidate)}" +
                          $"&path={Uri.EscapeDataString(pathCandidate)}" +
                          $"&per_page=1";

                if (GitHub.TryApi(url, out var json) &&
                    json is JArray commits &&
                    commits[0] is JObject obj &&
                    obj.Property("sha") is JProperty prop &&
                    prop != null &&
                    prop.Value.Type == JTokenType.String)
                {
                    newSha = prop.Value.ToObject<string>();
                    break;
                }
            }
        }

        // Just propagate back what we had initially, as an optimization for HEAD and cases 
        // where etags match.
        newSha ??= originalSha;

        if (newSha != null)
            response.Headers.TryAddWithoutValidation("X-Sha", newSha);

        return response;
    }
}
