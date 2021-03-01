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
using DotNetConfig;

namespace Devlooped
{
    class AddCommand : Command
    {
        public AddCommand(Config configuration) : base(configuration) { }

        public ISet<FileSpec> Changes { get; } = new HashSet<FileSpec>(FileSpecComparer.Default);

        public override async Task<int> ExecuteAsync()
        {
            var http = HttpClientFactory.Create();
            var result = 0;

            if (Files.Count == 0)
                return 0;

            var length = Files.Select(x => x.Path).Max(x => x.Length) + 1;
            void Write(string s) => Console.Write(s + new string(' ', length - s.Length));

            var processed = new HashSet<string>();

            // TODO: allow configuration to provide HTTP headers, i.e. auth?
            foreach (var file in Files)
            {
                var section = Configuration.GetSection("file", file.Path);

                if (section.GetBoolean("skip") == true)
                    continue;

                if (File.Exists(file.Path) && File.GetAttributes(file.Path).HasFlag(FileAttributes.ReadOnly))
                {
                    Write(file.Path);
                    Console.WriteLine("? Readonly, skipping");
                    continue;
                }

                var uri = file.Uri;
                if (uri == null)
                {
                    var url = section.GetString("url");
                    if (url != null)
                    {
                        uri = new Uri(url);
                    }
                    else
                    {
                        Write(file.Path);
                        Console.WriteLine("x Unconfigured");
                        continue;
                    }
                }

                if (processed.Contains(uri.ToString()))
                    continue;

                var etag = file.ETag ?? section.GetString("etag");
                var weak = section.GetBoolean("weak");
                var sha = section.GetString("sha");
                var originalUri = uri;

                try
                {
                    processed.Add(uri.ToString());
                    var request = new HttpRequestMessage(DryRun ? HttpMethod.Head : HttpMethod.Get, uri);
                    // Propagate previous values, used in GitHubRawHandler to optimize SHA retrieval
                    if (etag != null)
                        request.Headers.TryAddWithoutValidation("X-ETag", etag);
                    if (sha != null)
                        request.Headers.TryAddWithoutValidation("X-Sha", sha);

                    if (etag != null && File.Exists(file.Path))
                    {
                        // Try HEAD and skip file if same etag
                        var headReq = new HttpRequestMessage(HttpMethod.Head, uri);
                        // Propagate previous values, used in GitHubRawHandler to optimize SHA retrieval
                        if (etag != null)
                            headReq.Headers.TryAddWithoutValidation("X-ETag", etag);
                        if (sha != null)
                            headReq.Headers.TryAddWithoutValidation("X-Sha", sha);

                        var headResp = await http.SendAsync(headReq);

                        if (headResp.IsSuccessStatusCode &&
                            headResp.Headers.ETag?.Tag?.Trim('"') == etag)
                        {
                            // To keep "noise" from unchanged files to a minimum, when 
                            // doing a dry run we only list actual changes.
                            if (!DryRun)
                            {
                                // No need to download
                                Write(file.Path);
                                ColorConsole.Write("=".DarkGray());
                                Console.WriteLine($" <- {originalUri}");

                                // For backs compat, set the sha if found and not already present.
                                if (sha == null &&
                                    headResp.Headers.TryGetValues("X-Sha", out var headShas) &&
                                    headShas.FirstOrDefault() is string headSha &&
                                    !string.IsNullOrEmpty(headSha))
                                    section.SetString("sha", headSha);
                            }

                            continue;
                        }

                        // NOTE: this code alone didn't work consistently:
                        // For some reason, GH would still give us the full response, 
                        // even with a different etag from the previous request.
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"" + etag + "\"", weak.GetValueOrDefault()));
                    }

                    Write(file.Path);

                    var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (response.Headers.TryGetValues("X-Sha", out var values) &&
                        values.FirstOrDefault() is string newSha &&
                        !string.IsNullOrEmpty(newSha))
                        file.NewSha = newSha;
                    else
                        file.NewSha = null;

                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        // No need to download
                        ColorConsole.Write("=".DarkGray());
                        Console.WriteLine($" <- {originalUri}");

                        // For backs compat, set the sha if found and not already present.
                        if (section.GetString("sha") == null &&
                            file.NewSha != null)
                            section.SetString("sha", file.NewSha);

                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        if (uri.Host.Equals("github.com") && !GitHub.TryIsInstalled(out var output))
                        {
                            ColorConsole.WriteLine("=> ", "the GitHub CLI is required for this URL".Red());
                            ColorConsole.WriteLine(output.Yellow());
                            ColorConsole.WriteLine("See https://cli.github.com/manual/installation".Yellow());
                            System.Diagnostics.Process.Start(new ProcessStartInfo("https://cli.github.com/manual/installation") { UseShellExecute = true });
                            return -1;
                        }

                        // The URL might be a directory or repo branch top-level path. If so, we can use the GitHub cli to fetch all files.
                        if (uri.Host.Equals("github.com") &&
                            (response.StatusCode == HttpStatusCode.NotFound ||
                            // BadRequest from our conversion to raw URLs in GitHubRawHandler
                            response.StatusCode == HttpStatusCode.BadRequest))
                        {
                            var gh = GitHub.TryGetFiles(file, out var repoFiles);
                            switch (gh)
                            {
                                case GitHubResult.Success:
                                    var targetDir = file.IsDefaultPath ? null : file.Path;
                                    // Store the URL for later updates
                                    if (!Configuration.GetAll("file", targetDir, "url").Any(entry => uri.ToString().Equals(entry.RawValue, StringComparison.OrdinalIgnoreCase)))
                                        Configuration.AddString("file", targetDir, "url", uri.ToString());

                                    // Run again with the fetched files.
                                    var command = Clone();
                                    command.Files.AddRange(repoFiles);
                                    Console.WriteLine();

                                    // Track all files as already processed to skip duplicate processing from 
                                    // existing expanded list.
                                    foreach (var repoFile in repoFiles)
                                        processed.Add(repoFile.Uri!.ToString());

                                    result = await command.ExecuteAsync();

                                    foreach (var change in command.Changes)
                                        Changes.Add(change);

                                    continue;
                                case GitHubResult.Failure:
                                    return -1;
                                case GitHubResult.Skip:
                                    break;
                                default:
                                    break;
                            }
                        }

                        ColorConsole.WriteLine($"x <- {originalUri}".Yellow());

                        if (response.StatusCode != HttpStatusCode.NotFound ||
                            !OnRemoteUrlMissing(file))
                        {
                            // Only show as error if we haven't deleted the file as part of a 
                            // sync operation, or if the error is not 404.
                            ColorConsole.WriteLine(new string(' ', length + 5), $"{(int)response.StatusCode}: {response.ReasonPhrase}".Red());
                        }

                        continue;
                    }

                    if (!DryRun)
                    {
                        etag = response.Headers.ETag?.Tag?.Trim('"');

                        var path = file.Path.IndexOf(Path.AltDirectorySeparatorChar) != -1
                            ? file.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                            : file.Path;
                        // Ensure target directory exists.
                        if (Path.GetDirectoryName(path)?.Length > 0)
                            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

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
#if NETCOREAPP21
                        if (File.Exists(path))
                            File.Delete(path);

                        File.Move(tempPath, path);
#else
                        File.Move(tempPath, path, overwrite: true);
#endif

                        section.SetString("url", originalUri.ToString());

                        if (file.NewSha != null)
                            section.SetString("sha", file.NewSha);
                        else
                            section.Unset("sha");

                        if (etag == null)
                            section.Unset("etag");
                        else
                            section.SetString("etag", etag);

                        if (response.Headers.ETag?.IsWeak == true)
                            section.SetBoolean("weak", true);
                        else
                            section.Unset("weak");
                    }

                    Changes.Add(file);

                    ColorConsole.Write("✓".Green());
                    Console.WriteLine($" <- {originalUri}");
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

        /// <summary>
        /// Whether to just print what would have been done, but not actually 
        /// downloading anything.
        /// </summary>
        protected virtual bool DryRun => false;

        /// <summary>
        /// Invoked when the URL for a file returns a 404.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the entry is removed automatically in 
        /// this case.
        /// </returns>
        protected virtual bool OnRemoteUrlMissing(FileSpec spec) => false;

        /// <summary>
        /// Creates a clone of the current instance.
        /// </summary>
        protected virtual AddCommand Clone() => new AddCommand(Configuration);
    }
}
