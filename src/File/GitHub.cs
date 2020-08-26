using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet
{
    public static class GitHub
    {
        public static bool IsInstalled() => Process.TryExecute("gh", "--version", out var version) && version.StartsWith("gh version");

        public static bool TryGetFiles(FileSpec spec, out List<FileSpec> result)
        {
            var files = result = new List<FileSpec>();
            if (spec.Uri == null)
                return false;

            // GH CLI is installed, try fetching via API.
            var parts = spec.Uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/');
            var baseDir = spec.IsDefaultPath ? "" : spec.Path;

            // We can't determine org/repo from URI.
            if (parts.Length < 2)
            {
                Console.Error.WriteLine("GitHub URL must be of the form: 'github.com/{org}/{repo}' (with an optional '/{path}').");
                return false;
            }

            var owner = parts[0];
            var repo = parts[1];
            string? branch = default;
            string? repoDir = default;
            if (parts.Length > 3)
            {
                if (parts[2] == "tree")
                {
                    // tree urls contain branch and optionally 
                    branch = parts[3];
                    if (parts.Length >= 4)
                        repoDir = string.Join('/', parts[4..]);
                }
                else if (parts[2] == "blob")
                {
                    // Blob urls point to actual files, so we 
                    // don't do any GH CLI processing for them.
                    return false;
                }
            }

            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents";
            var apiPath = repoDir == null ? "" : ("/" + repoDir);
            var apiQuery = branch == null ? "" : "?ref=" + branch;

            Console.Write("=> fetching via gh cli");

            if (Process.TryExecute("gh", "api " + apiUrl + apiPath + apiQuery, out var data) &&
                JsonConvert.DeserializeObject<JToken>(data) is JArray array)
            {
                Action<string>? getFiles = default;
                void addFiles(JArray array)
                {
                    foreach (var item in array)
                    {
                        // Write poor man's progress
                        Console.Write(".");
                        if ("file".Equals(item["type"]?.ToString(), StringComparison.Ordinal))
                        {
                            var itemPath = item["path"]!.ToString();
                            // In case the target path was specified as '.', don't recreate the full 
                            // repo directory structure and just start from the base repoDir instead
                            if (baseDir == "." && repoDir != null && itemPath.StartsWith(repoDir))
                                itemPath = itemPath.Substring(repoDir.Length);
                            // Special case to avoid duplicate dirs following the base dir, such as "docs/docs/design/..."
                            else if (baseDir.Length > 0 && itemPath.StartsWith(baseDir))
                                itemPath = itemPath.Substring(baseDir.Length);

                            files.Add(new FileSpec(
                                Path.Combine(baseDir == "." ? "" : baseDir, itemPath.TrimStart('/'))
                                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                // We use the html url since our Http handler knows how to turn those into
                                // raw URLs, and it's nicer to keep the browsable URL instead so users 
                                // can easily click on that and browse the content on the web.
                                new Uri(item["html_url"]!.ToString())));
                        }
                        else if ("dir".Equals(item["type"]?.ToString(), StringComparison.Ordinal))
                        {
                            getFiles!(item["path"]!.ToString());
                        }
                    }
                }
                getFiles = path =>
                {
                    if (Process.TryExecute("gh", "api " + apiUrl + path + apiQuery, out var data) &&
                        JsonConvert.DeserializeObject<JToken>(data) is JArray array)
                    {
                        addFiles(array);
                    }
                };
                addFiles(array);
                return true;
            }
            else
            {
                Console.Error.WriteLine(data);
                return false;
            }
        }
    }
}
