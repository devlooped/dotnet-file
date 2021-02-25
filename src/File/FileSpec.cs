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
            => (Path, Uri, ETag, Sha, NewSha)
            = (path, uri, etag, sha, sha);

        public string Path { get; }

        public Uri? Uri { get; }

        public string? ETag { get; }

        public string? Sha { get; }

        public string? NewSha { get; set; }

        internal bool IsDefaultPath { get; }

        public override int GetHashCode() => Path.GetHashCode();

        public override bool Equals(object? obj) => obj is FileSpec other && other.Path.Equals(Path);
    }
}
