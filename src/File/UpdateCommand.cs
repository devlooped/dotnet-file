using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    class UpdateCommand : DownloadCommand
    {
        public UpdateCommand(Config configuration) : base(configuration) { }

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
                configured = GetConfiguredFiles().Intersect(Files, new FileSpecComparer()).ToList();
            }

            // Add the new ones that are just passed in as URLs
            configured = Files
                .Except(configured, new FileSpecComparer())
                .Concat(configured).ToList();

            Files.Clear();
            Files.AddRange(configured);

            return base.ExecuteAsync();
        }
    }
}
