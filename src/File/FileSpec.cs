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

        public FileSpec(string path, Uri? uri = null, string? etag = null, string? sha = null)
            => (Path, Uri, ETag, Sha)
            = (path, uri, etag, sha);

        public string Path { get; }

        public Uri? Uri { get; }

        public string? ETag { get; }

        public string? Sha { get; }

        internal bool IsDefaultPath { get; }
    }
}
