using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped;

class ListCommand(Config configuration) : Command(configuration)
{
    public override Task<int> ExecuteAsync()
    {
        var configured = GetConfiguredFiles().ToArray();
        var length = configured.Select(x => x.Path).Max(x => x.Length) + 1;

        foreach (var file in configured)
        {
            Console.Write(file.Path + new string(' ', length - file.Path.Length));
            if (File.Exists(file.Path))
                Console.Write('✓');
            else
                Console.Write('?');

            Console.Write(" <= ");
            Console.WriteLine(file.Uri?.OriginalString);
        }

        return Task.FromResult(0);
    }
}
