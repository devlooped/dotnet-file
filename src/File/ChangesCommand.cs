using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ColoredConsole;

namespace Microsoft.DotNet
{
    class ChangesCommand : Command
    {
        public ChangesCommand(Config configuration) : base(configuration) { }

        public override async Task<int> ExecuteAsync()
        {
            var http = HttpClientFactory.Create();

            var configured = Files;
            if (Files.Count == 0)
            {
                // Implicitly, running with no files means listing all
                configured = GetConfiguredFiles().ToList();
            }
            else
            {
                // Otherwise, switch to the configured versions to get url and etag
                configured = GetConfiguredFiles().Intersect(Files, new FileSpecComparer()).ToList();
            }

            var length = configured.Select(x => x.Path).Max(x => x.Length) + 1;
            Action<string> writefixed = s => Console.Write(s + new string(' ', length - s.Length));

            foreach (var file in configured)
            {
                writefixed(file.Path);

                if (!File.Exists(file.Path))
                {
                    ColorConsole.Write("?".Yellow());
                    Console.WriteLine($" <- {file.Uri?.OriginalString}");
                    continue;
                }

                var request = new HttpRequestMessage(HttpMethod.Head, file.Uri);
                var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    if (File.Exists(file.Path))
                    {
                        if (response.Headers.ETag?.Tag?.Trim('"') != file.ETag)
                            ColorConsole.Write("^".Green());
                        else
                            ColorConsole.Write("=".DarkGray());
                    }
                    else
                    {
                        ColorConsole.Write("?".Yellow());
                    }

                    Console.WriteLine($" <- {file.Uri?.OriginalString}");
                }
                else
                {
                    ColorConsole.WriteLine("x".Red(), $" <- {file.Uri?.OriginalString}");
                    ColorConsole.WriteLine($"{new string(' ', length + 5)}{(int)response.StatusCode}: {response.ReasonPhrase}".Red());
                }
            }

            foreach (var unknown in Files.Except(configured, new FileSpecComparer()))
            {
                writefixed(unknown.Path);
                ColorConsole.WriteLine("x Unconfigured".Yellow());
            }

            return 0;
        }
    }
}
