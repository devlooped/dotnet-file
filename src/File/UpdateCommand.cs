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
            configured = GetConfiguredFiles().ToList();
        }
        else
        {
            // Switch to the configured versions to get url and etag
            configured = GetConfiguredFiles().Intersect(Files, FileSpecComparer.Default).ToList();
        }

        // Add the new ones that are just passed in as URLs
        configured = Files
            .Except(configured, FileSpecComparer.Default)
            .Concat(configured)
            .ToList();

        Files.Clear();
        Files.AddRange(configured);

        return base.ExecuteAsync();
    }

    protected override AddCommand Clone() => new UpdateCommand(Configuration);
}
