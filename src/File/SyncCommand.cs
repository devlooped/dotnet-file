using System;
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
            DeleteEmptyDirectories(dir);

            Configuration.RemoveSection("file", spec.Path);

            return true;
        }

        void DeleteEmptyDirectories(string? dir)
        {
            if (dir != null && !Directory.EnumerateFiles(dir).Any() && !Directory.EnumerateDirectories(dir).Any())
            {
                var parent = new DirectoryInfo(dir).Parent?.FullName;
                Directory.Delete(dir);
                DeleteEmptyDirectories(parent);
            }
        }
    }
}
