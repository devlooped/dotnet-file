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

            var length = Files.Select(x => x.Path).Max(x => x.Length) + 1;
            Action<string> writefixed = s => Console.Write(s + new string(' ', length - s.Length));

            // TODO: allow configuration to provide HTTP headers, i.e. auth?
            foreach (var file in Files)
            {
                var etag = Configuration.Get<string?>("file", file.Path, "etag");
                var weak = Configuration.Get<bool?>("file", file.Path, "weak");

                writefixed(file.Path);

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, file.Uri);
                    if (etag != null)
                    {
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"" + etag + "\"", weak.GetValueOrDefault()));
                    }

                    var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        // No need to download
                        Console.WriteLine($"✓ <= {file.Uri}");
                        continue;
                    }

                    etag = response.Headers.ETag?.Tag?.Trim('"');
                    var path = file.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (var stream = File.Open(path, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(stream);
                    }

                    Configuration.Set("file", file.Path, "url", file.Uri!.OriginalString);
                    
                    if (etag == null)
                        Configuration.Unset("file", file.Path, "etag");
                    else
                        Configuration.Set("file", file.Path, "etag", etag);

                    if (response.Headers.ETag?.IsWeak == true)
                        Configuration.Set("file", file.Path, "weak", true);
                    else
                        Configuration.Unset("file", file.Path, "weak");

                    Console.WriteLine($"v <= {file.Uri}");
                }
                //catch (HttpRequestException re)
                //{
                //    Console.WriteLine($"x <= {file.Uri?.OriginalString}");
                //    Console.WriteLine($"{new string(' ', length)} {re.Message}");
                //}
                catch (Exception e)
                {
                    Console.WriteLine($"x <= {file.Uri?.OriginalString}");
                    Console.Write(new string(' ', length + 5));
                    Console.WriteLine(e.Message);
                    result = 1;
                }
            }

            return result;
        }
    }
}
