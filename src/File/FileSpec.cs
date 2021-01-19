using System;

namespace Devlooped
{
    public class FileSpec
    {
        public FileSpec(Uri uri)
            : this(System.IO.Path.GetFileName(uri.LocalPath), uri)
        {
            IsDefaultPath = true;
        }

        public FileSpec(string path, Uri? uri = null, string? etag = null)
        {
            Path = path;
            Uri = uri;
            ETag = etag;
        }

        public string Path { get; }

        public Uri? Uri { get; }

        public string? ETag { get; }

        internal bool IsDefaultPath { get; }
    }
}
