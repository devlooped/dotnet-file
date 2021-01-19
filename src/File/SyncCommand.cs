using System.IO;
using System.Linq;
using DotNetConfig;

namespace Devlooped
{
    class SyncCommand : UpdateCommand
    {
        public SyncCommand(Config configuration) : base(configuration) { }

        protected override bool OnRemoteUrlMissing(FileSpec spec)
        {
            // If the file exists locally, delete it. Remove the config entry.
            if (File.Exists(spec.Path))
                File.Delete(spec.Path);

            // Clear empty directories
            var dir = new FileInfo(spec.Path).DirectoryName;
            if (dir != null && !Directory.EnumerateFiles(dir).Any())
                Directory.Delete(dir);

            Configuration.RemoveSection("file", spec.Path);

            return true;
        }
    }
}
