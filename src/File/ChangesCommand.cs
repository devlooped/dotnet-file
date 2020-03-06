using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    class ChangesCommand : Command
    {
        public ChangesCommand(Config configuration) : base(configuration) { }

        public override async Task<int> ExecuteAsync()
        {
            var http = new HttpClient();

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
                    Console.Write('?');
                    Console.Write(" <= ");
                    Console.WriteLine(file.Uri?.OriginalString);
                    continue;
                }

                var request = new HttpRequestMessage(HttpMethod.Head, file.Uri);
                var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    if (File.Exists(file.Path))
                    {
                        if (response.Headers.ETag?.Tag?.Trim('"') != file.ETag)
                            Console.Write('^');
                        else
                            Console.Write('✓');
                    }
                    else
                    {
                        Console.Write('?');
                    }

                    Console.WriteLine($" <= {file.Uri?.OriginalString}");
                }
                else
                {
                    Console.WriteLine($"x <= {file.Uri?.OriginalString}");
                    Console.WriteLine($"{new string(' ', length + 5)}{(int)response.StatusCode}: {response.ReasonPhrase}");
                }
            }

            foreach (var unknown in Files.Except(configured, new FileSpecComparer()))
            {
                writefixed(unknown.Path);
                Console.WriteLine("x Unconfigured");
            }

            return 0;
        }
    }
}
