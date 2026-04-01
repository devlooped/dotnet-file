using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;

namespace Devlooped;

class UpdateCommand(Config configuration) : AddCommand(configuration)
{
    public override Task<int> ExecuteAsync()
    {
        var configured = Files;
        if (configured.Count == 0)
        {
            // Implicitly, running update with no files means updating all
            configured = [.. GetConfiguredFiles()];
        }
        else
        {
            // Switch to the configured versions to get url and etag
            configured = [.. GetConfiguredFiles().Intersect(Files, FileSpecComparer.Default)];
        }

        // Add the new ones that are just passed in as URLs
        configured =
        [
            .. Files.Except(configured, FileSpecComparer.Default),
            .. configured,
        ];

        Files.Clear();
        Files.AddRange(configured);

        return base.ExecuteAsync();
    }

    protected override AddCommand Clone() => new UpdateCommand(Configuration);
}
