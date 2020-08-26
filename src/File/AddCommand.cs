using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ColoredConsole;

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

            var processed = new HashSet<string>();

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
                    var url = Configuration.GetString("file", file.Path, "url");
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

                if (processed.Contains(uri.ToString()))
                    continue;

                var etag = file.ETag ?? Configuration.GetString("file", file.Path, "etag");
                var weak = Configuration.GetBoolean("file", file.Path, "weak");
                var originalUri = uri;

                write(file.Path);

                try
                {
                    processed.Add(uri.ToString());
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    if (etag != null && File.Exists(file.Path))
                    {
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"" + etag + "\"", weak.GetValueOrDefault()));
                    }

                    var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        // No need to download
                        Console.WriteLine($"= <- {originalUri}");
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        if (uri.Host.Equals("github.com") && !GitHub.IsInstalled())
                        {
                            ColorConsole.WriteLine("=> ", "the GitHub CLI is required for this URL".Red());
                            ColorConsole.WriteLine("See https://cli.github.com/manual/installation".Yellow());
                            System.Diagnostics.Process.Start(new ProcessStartInfo("https://cli.github.com/manual/installation") { UseShellExecute = true });
                            return -1;
                        }

                        // The URL might be a directory or repo branch top-level path. If so, we can use the GitHub cli to fetch all files.
                        if (uri.Host.Equals("github.com") &&
                            (response.StatusCode == HttpStatusCode.NotFound ||
                            // BadRequest from our conversion to raw URLs in the HttpClient handler
                            response.StatusCode == HttpStatusCode.BadRequest))
                        {
                            if (GitHub.TryGetFiles(file, out var repoFiles))
                            {
                                var targetDir = file.IsDefaultPath ? null : file.Path;
                                // Store the URL for later updates
                                if (!Configuration.GetAll("file", targetDir, "url").Any(entry => uri.ToString().Equals(entry.RawValue, StringComparison.OrdinalIgnoreCase)))
                                    Configuration.AddString("file", targetDir, "url", uri.ToString());

                                // Run again with the fetched files.
                                var command = new AddCommand(Configuration);
                                command.Files.AddRange(repoFiles);
                                Console.WriteLine();

                                // Track all files as already processed to skip duplicate processing from 
                                // existing expanded list.
                                foreach (var repoFile in repoFiles)
                                {
                                    processed.Add(repoFile.Uri!.ToString());
                                }

                                result = await command.ExecuteAsync();
                                continue;
                            }
                            else
                            {
                                return -1;
                            }
                        }

                        ColorConsole.WriteLine($"x <- {originalUri}".Yellow());

                        if (response.StatusCode != HttpStatusCode.NotFound ||
                            !OnDeleteNotFound(file))
                        {
                            // Only show as error if we haven't deleted the file as part of a 
                            // sync operation, or if the error is not 404.
                            ColorConsole.WriteLine(new string(' ', length + 5), $"{(int)response.StatusCode}: {response.ReasonPhrase}".Red());
                        }

                        continue;
                    }

                    etag = response.Headers.ETag?.Tag?.Trim('"');

                    var path = file.Path.IndexOf(Path.AltDirectorySeparatorChar) != -1
                        ? file.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                        : file.Path;
                    // Ensure target directory exists.
                    if (Path.GetDirectoryName(path)?.Length > 0)
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    try
                    {
                        using var stream = File.Open(tempPath, FileMode.Create);
                        await response.Content.CopyToAsync(stream);
                    }
                    catch (Exception) // Delete temp file on error
                    {
                        File.Delete(tempPath);
                        throw;
                    }
                    File.Move(tempPath, path, overwrite: true);

                    Configuration.SetString("file", file.Path, "url", originalUri.ToString());

                    if (etag == null)
                        Configuration.Unset("file", file.Path, "etag");
                    else
                        Configuration.SetString("file", file.Path, "etag", etag);

                    if (response.Headers.ETag?.IsWeak == true)
                        Configuration.SetBoolean("file", file.Path, "weak", true);
                    else
                        Configuration.Unset("file", file.Path, "weak");

                    Console.WriteLine($"✓ <- {originalUri}");
                }
                catch (Exception e)
                {
                    ColorConsole.WriteLine($"x <- {originalUri}".Yellow());
                    Console.Write(new string(' ', length + 5));
                    ColorConsole.WriteLine(e.Message.Red());
                    result = 1;
                }
            }

            return result;
        }

        protected virtual bool OnDeleteNotFound(FileSpec spec) => false;
    }
}
