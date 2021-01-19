using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped
{
    class DeleteCommand : Command
    {
        public DeleteCommand(Config configuration) : base(configuration) { }

        public override Task<int> ExecuteAsync()
        {
            var result = 0;

            var length = Files.Select(x => x.Path).Max(x => x.Length) + 1;
            Action<string> writefixed = s => Console.Write(s + new string(' ', length - s.Length));

            foreach (var file in Files)
            {
                try
                {
                    if (File.Exists(file.Path))
                        File.Delete(file.Path);

                    var url = Configuration.GetString("file", file.Path, "url");
                    if (url != null)
                        Configuration.RemoveSection("file", file.Path);

                    writefixed(file.Path);
                    Console.WriteLine('✓');
                }
                catch (Exception e)
                {
                    writefixed(file.Path);
                    Console.WriteLine("    x - " + e.Message);
                    result = 1;
                }
            }

            return Task.FromResult(result);
        }
    }
}
