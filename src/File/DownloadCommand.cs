using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    class DownloadCommand : Command
    {
        public DownloadCommand(Config configuration) : base(configuration) { }

        public override async Task<int> ExecuteAsync()
        {
            var http = new HttpClient();
            var result = 0;

            if (Files.Count == 0)
                return 0;

            var length = Files.Select(x => x.Path).Max(x => x.Length) + 1;
            Action<string> writefixed = s => Console.Write(s + new string(' ', length - s.Length));

            // TODO: allow configuration to provide HTTP headers, i.e. auth?
            foreach (var file in Files)
            {
                var uri = file.Uri;
                if (uri == null)
                {
                    var url = Configuration.Get<string?>("file", file.Path, "url");
                    if (url != null)
                    {
                        uri = new Uri(url);
                    }
                    else
                    {
                        writefixed(file.Path);
                        Console.WriteLine("x Unconfigured");
                        continue;
                    }
                }

                var etag = file.ETag ?? Configuration.Get<string?>("file", file.Path, "etag");
                var weak = Configuration.Get<bool?>("file", file.Path, "weak");

                writefixed(file.Path);

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    if (etag != null && File.Exists(file.Path))
                    {
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"" + etag + "\"", weak.GetValueOrDefault()));
                    }

                    var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    // TODO: we might want to check more info about the file, such as length, MD5 (if the header is present 
                    // in the response), etc. In those cases we might still want ot fetch the new file if it doesn't 
                    // match with what's locally.
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        // No need to download
                        Console.WriteLine($"= <= {uri}");
                        continue;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"x <= {uri}");
                        Console.WriteLine($"{new string(' ', length + 5)}{(int)response.StatusCode}: {response.ReasonPhrase}");
                        continue;
                    }

                    etag = response.Headers.ETag?.Tag?.Trim('"');

                    var path = file.Path.IndexOf(Path.AltDirectorySeparatorChar) != -1 
                        ? file.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                        : file.Path;                    
                    // Ensure target directory exists.
                    if (Path.GetDirectoryName(path)?.Length > 0)
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (var stream = File.Open(path, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(stream);
                    }

                    Configuration.Set("file", file.Path, "url", uri);
                    
                    if (etag == null)
                        Configuration.Unset("file", file.Path, "etag");
                    else
                        Configuration.Set("file", file.Path, "etag", etag);

                    if (response.Headers.ETag?.IsWeak == true)
                        Configuration.Set("file", file.Path, "weak", true);
                    else
                        Configuration.Unset("file", file.Path, "weak");

                    Console.WriteLine($"✓ <= {uri}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"x <= {uri}");
                    Console.Write(new string(' ', length + 5));
                    Console.WriteLine(e.Message);
                    result = 1;
                }
            }

            return result;
        }
    }
}
