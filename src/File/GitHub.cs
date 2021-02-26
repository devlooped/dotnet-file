using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColoredConsole;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Devlooped
{
    public enum GitHubResult
    {
        /// <summary>
        /// We successfully retrieved files from GH CLI.
        /// </summary>
        Success,
        /// <summary>
        /// There was a failure retrieving files from GH CLI.
        /// </summary>
        Failure,
        /// <summary>
        /// The specified file/url was skipped (i.e. a blog URL, does not apply to GH CLI fetching).
        /// </summary>
        Skip,
    }

    public static class GitHub
    {
        public static bool IsInstalled { get; } = TryIsInstalled(out var _);

        public static bool TryIsInstalled(out string output)
            => Process.TryExecute("gh", "--version", out output) && output.StartsWith("gh version");

        public static bool TryApi(string endpoint, out JToken? json)
        {
            json = null;

            return Process.TryExecute("gh", "api " + endpoint, out var data) &&
                (json = JsonConvert.DeserializeObject<JToken>(data)) != null;
        }

        public static GitHubResult TryGetFiles(FileSpec spec, out List<FileSpec> result)
        {
            var files = result = new List<FileSpec>();
            if (spec.Uri == null)
                return GitHubResult.Skip;

            // GH CLI is installed, try fetching via API.
            var parts = spec.Uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/');
            var baseDir = spec.IsDefaultPath ? "" : spec.Path;

            // We can't determine org/repo from URI.
            if (parts.Length < 2)
            {
                Console.Error.WriteLine("GitHub URL must be of the form: 'github.com/{org}/{repo}' (with an optional '/{path}').");
                return GitHubResult.Failure;
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
                    return GitHubResult.Skip;
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
                return GitHubResult.Success;
            }
            else
            {
                var message = JsonConvert.DeserializeObject<JObject>(data)?.Property("message")?.Value.ToString();
                if (message != null)
                    ColorConsole.WriteLine(" => ", message.Red());
                else
                    ColorConsole.WriteLine(Environment.NewLine + "\t => " + data.Red());

                Console.WriteLine("Ensure you can access the given repo by running:");
                ColorConsole.WriteLine($"  gh repo view {owner}/{repo}".Yellow());
                return GitHubResult.Failure;
            }
        }

        public static void WriteChanges(string changelog, ISet<FileSpec> changes)
        {
            var github = changes
                .Where(x => x.Uri != null && x.Uri.Host.EndsWith("github.com") && x.Sha != x.NewSha)
                .GroupBy(x => string.Join('/', x.Uri!.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(2)));

            var output = new StringBuilder();

            Console.WriteLine();
            ColorConsole.WriteLine("Building changelog...".Green());

            foreach (var group in github)
            {
                var commits = new HashSet<(string sha, DateTime date, string message)>();
                var compared = new HashSet<(string? from, string? to)>();

                ColorConsole.WriteLine($"[{group.Key}]".Yellow());

                AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn(),
                    })
                    .Start(ctx =>
                    {
                        var tasks = new Dictionary<FileSpec, ProgressTask>();
                        foreach (var change in group)
                        {
                            var filename = string.Join('/', change.Uri!.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(4));
                            tasks[change] = ctx.AddTask($"[blue]{filename}[/]");
                        }

                        group.AsParallel().ForAll(change =>
                        {
                            var task = tasks[change];
                            if (compared.Contains((change.Sha, change.NewSha)))
                            {
                                task.Increment(100);
                                return;
                            }

                            var filename = string.Join('/', change.Uri!.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(4));

                            if (change.Sha == null)
                            {
                                // Just process the new sha for changelog.
                                if (TryApi($"repos/{group.Key}/commits/{change.NewSha}", out var commitJson) &&
                                    commitJson != null)
                                {
                                    dynamic commit = commitJson;
                                    (string sha, DateTime date, string message) entry = ((string)commit.sha, DateTime.Parse((string)commit.commit.author.date),
                                        // Grab only first line of message
                                        ((string)commit.commit.message).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault().Trim());

                                    // Retrieve the full commit so we can only filter the ones that have
                                    // the current changed file.
                                    commits.Add(entry);
                                }
                            }
                            else if (TryApi($"repos/{group.Key}/compare/{change.Sha}...{change.NewSha}", out var json) &&
                                json is JObject jobj &&
                                jobj.Property("commits")?.Value is JArray array)
                            {
                                var increment = 100d / array.Count;
                                foreach (dynamic commit in array)
                                {
                                    (string sha, DateTime date, string message) entry = ((string)commit.sha, DateTime.Parse((string)commit.commit.author.date),
                                        // Grab only first line of message
                                        ((string)commit.commit.message).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault().Trim());

                                    if (!commits.Contains(entry) &&
                                        TryApi($"repos/{group.Key}/commits/{entry.sha}", out var commitJson) &&
                                        commitJson is JObject commitObj &&
                                        commitObj.Property("files")?.Value is JArray files)
                                    {
                                        if (files.OfType<JObject>().Any(file =>
                                            file.Property("filename")?.Value.ToString() == filename))
                                        {
                                            // Retrieve the full commit so we can only filter the ones that have
                                            // the current changed file.
                                            commits.Add(entry);
                                        }
                                    }

                                    task.Increment(increment);
                                }
                            }

                            task.Increment(100);
                            lock (compared)
                                compared.Add((change.Sha, change.NewSha));
                        });
                    });

                output.AppendLine($"# {group.Key}").AppendLine();

                // GitHub REST API does not seem to handle unicode the same way the website 
                // does. Unicode emoji shows up perfectly fine on the web (see https://github.com/devlooped/oss/commits/main/.github/workflows/build.yml)
                // yet each emoji shows up as multiple separate chars in the responses. We 
                // implement a simple cleanup that works in our tests with devlooped/oss repo. 
                string removeUnicodeEmoji(string message)
                {
                    var result = new StringBuilder(message.Length);
                    var index = 0;
                    while (index < message.Length)
                    {
                        // Consider up to U+036F / 879 char as "regular" text.
                        // This would allow some formatting chars still.
                        // Anything higher, consider as the start of an unicode emoji
                        // symbol comprising more chars until the next high one.
                        if (message[index] > 879)
                        {
                            while (++index <= message.Length && message[index] <= 879 && !char.IsWhiteSpace(message[index]))
                                ;

                            index++;
                        }
                        else
                        {
                            result.Append(message[index]);
                            index++;
                        }
                    }

                    return result.ToString();
                };

                foreach (var commit in commits)
                    output.AppendLine($"- {removeUnicodeEmoji(commit.message).Trim()} https://github.com/{group.Key}/commit/{commit.sha.Substring(0, 7)}");

                output.AppendLine();
            }

            File.WriteAllText(changelog, output.ToString(), Encoding.UTF8);
        }
    }
}
