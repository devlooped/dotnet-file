using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet
{
    class AddCommand : Command
    {
        public AddCommand(Config configuration) : base(configuration) { }

        public override async Task<int> ExecuteAsync()
        {
            var http = HttpClientFactory.Create();
            var result = 0;

            if (Files.Count == 0)
                return 0;

            var length = Files.Select(x => x.Path).Max(x => x.Length) + 1;
            Action<string> write = s => Console.Write(s + new string(' ', length - s.Length));

            // TODO: allow configuration to provide HTTP headers, i.e. auth?
            foreach (var file in Files)
            {
                if (File.Exists(file.Path) && File.GetAttributes(file.Path).HasFlag(FileAttributes.ReadOnly))
                {
                    write(file.Path);
                    Console.WriteLine("? Readonly, skipping");
                    continue;
                }

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
                        write(file.Path);
                        Console.WriteLine("x Unconfigured");
                        continue;
                    }
                }

                var etag = file.ETag ?? Configuration.Get<string?>("file", file.Path, "etag");
                var weak = Configuration.Get<bool?>("file", file.Path, "weak");
                var originalUri = uri;

                write(file.Path);

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
                        Console.WriteLine($"= <- {originalUri}");
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        // The URL might be a directory or repo branch top-level path. If so, we can use the GitHub cli to fetch all files.
                        if (uri.Host.Equals("github.com") &&
                            response.StatusCode == HttpStatusCode.NotFound &&
                            !Path.HasExtension(uri.AbsolutePath))
                        {
                            if (Process.TryExecute("gh", "--version", out var version) && version.StartsWith("gh version"))
                            {
                                Console.Write(" => fetching via gh cli");
                                // GH CLI is installed, try fetching via API.
                                var parts = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/');
                                var baseDir = file.IsDefaultPath ? "" : file.Path;

                                if (parts.Length >= 2)
                                {
                                    var owner = parts[0];
                                    var repo = parts[1];
                                    string? branch = default;
                                    string? dir = default;
                                    if (parts.Length > 3 && parts[2] == "tree")
                                    {
                                        branch = parts[3];
                                        if (parts.Length >= 4)
                                            dir = string.Join('/', parts[4..]);
                                    }

                                    var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents";
                                    var apiPath = dir == null ? "" : "/" + dir;
                                    var apiQuery = branch == null ? "" : "?ref=" + branch;

                                    if (Process.TryExecute("gh", "api " + apiUrl + apiPath + apiQuery, out var data) &&
                                        JsonConvert.DeserializeObject<JToken>(data) is JArray array)
                                    {
                                        var files = new List<FileSpec>();
                                        Action<string>? getFiles = default;
                                        Action<JArray> addFiles = array =>
                                        {
                                            foreach (var item in array)
                                            {
                                                Console.Write(".");
                                                if ("file".Equals(item["type"]?.ToString(), StringComparison.Ordinal))
                                                {
                                                    files.Add(new FileSpec(
                                                        Path.Combine(baseDir, item["path"]!.ToString())
                                                            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                                        new Uri(item["download_url"]!.ToString())));
                                                }
                                                else if ("dir".Equals(item["type"]?.ToString(), StringComparison.Ordinal))
                                                {
                                                    getFiles!(item["path"]!.ToString());
                                                }
                                            }
                                        };
                                        getFiles = path =>
                                        {
                                            if (Process.TryExecute("gh", "api " + apiUrl + path + apiQuery, out var data) &&
                                                JsonConvert.DeserializeObject<JToken>(data) is JArray array)
                                            {
                                                addFiles(array);
                                            }
                                        };

                                        addFiles(array);

                                        // Run again with the fetched files.
                                        var command = new AddCommand(Configuration);
                                        command.Files.AddRange(files);
                                        Console.WriteLine();
                                        return await command.ExecuteAsync();
                                    }
                                    else
                                    {
                                        Console.WriteLine(data);
                                        return -1;
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"x <- {originalUri}");
                                Console.WriteLine($"{new string(' ', length + 5)}NotSupported: Install the GitHub CLI to add directories");
                                continue;
                            }
                        }

                        Console.WriteLine($"x <- {originalUri}");
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

                    Configuration.Set("file", file.Path, "url", originalUri);

                    if (etag == null)
                        Configuration.Unset("file", file.Path, "etag");
                    else
                        Configuration.Set("file", file.Path, "etag", etag);

                    if (response.Headers.ETag?.IsWeak == true)
                        Configuration.Set("file", file.Path, "weak", true);
                    else
                        Configuration.Unset("file", file.Path, "weak");

                    Console.WriteLine($"✓ <- {originalUri}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"x <- {originalUri}");
                    Console.Write(new string(' ', length + 5));
                    Console.WriteLine(e.Message);
                    result = 1;
                }
            }

            return result;
        }
    }
}
