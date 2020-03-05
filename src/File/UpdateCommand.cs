using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    class UpdateCommand : DownloadCommand
    {
        public UpdateCommand(Config configuration) : base(configuration) { }

        public override Task<int> ExecuteAsync()
        {
            // Implicitly, running update with no files means updating all
            if (Files.Count == 0)
                Files.AddRange(GetConfiguredFiles());

            return base.ExecuteAsync();
        }
    }
}
