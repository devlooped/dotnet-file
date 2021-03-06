﻿using System;
using System.Linq;

namespace Devlooped
{
    public class FileSpec
    {
        public static FileSpec WithDefaultPath(Uri uri)
            => WithGitHubUri(uri,
                raw => WithGitHubRawUri(raw,
                    fallback => new FileSpec(fallback)));

        static FileSpec WithGitHubUri(Uri uri, Func<Uri, FileSpec> next)
        {
            var parts = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (uri.Host != "github.com" || parts.Length <= 4)
                return next(uri);

            // https://github.com/devlooped/dotnet-file/tree/main/src
            // https://github.com/devlooped/dotnet-file/blob/main/src/Directory.props
            // https://github.com/devlooped/dotnet-file/raw/main/src/Directory.props
            if (parts[2] == "tree")
            {
                // This is a whole directory URL, so use that as the base dir,
                // denoted by the ending in a path separator. Note we skip 4 parts 
                // since those are org/repo/tree/branch, then comes the actual dir.
                return new FileSpec(string.Join('/', parts.Skip(4)) + "/", uri, finalPath: true);
            }
            else if (parts[2] == "blob" || parts[2] == "raw")
            {
                // This is a specific file URL, so use that as the target path.
                // Note we skip 4 parts since those are org/repo/[blob/raw]/branch.
                return new FileSpec(string.Join('/', parts.Skip(4)), uri, finalPath: true);
            }
            else
            {
                // Couldn't be figure it out, just add with no smarts
                return next(uri);
            }
        }

        static FileSpec WithGitHubRawUri(Uri uri, Func<Uri, FileSpec> next)
        {
            var parts = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // https://raw.githubusercontent.com/kzu/dotnet-file/master/README.md
            // Raw always points to a specific file URL, so use that as the target path.
            // Note we skip 4 parts since those are org/repo/[blob/raw]/branch.
            if (uri.Host == "raw.githubusercontent.com" && parts.Length > 3)
                return new FileSpec(string.Join('/', parts.Skip(3)), uri, finalPath: true);

            return next(uri);
        }

        public FileSpec(Uri uri)
            : this(System.IO.Path.GetFileName(uri.LocalPath), uri, finalPath: true)
        {
            IsDefaultPath = true;
        }

        public FileSpec(string path, Uri? uri = null, string? etag = null, string? sha = null)
            : this(path, uri, etag, sha, false) { }

        FileSpec(string path, Uri? uri = null, string? etag = null, string? sha = null, bool finalPath = false)
        {
            Uri = uri;
            ETag = etag;
            Sha = sha;
            NewSha = sha;

            if (!finalPath && uri != null &&
                (path.EndsWith('\\') || path.EndsWith('/')))
            {
                path = System.IO.Path.Combine(path, WithDefaultPath(uri!).Path);
            }

            // This will also normalize double slashes.
            var parts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0] == ".")
                Path = string.Join('/', parts.Skip(1));
            else
                Path = string.Join('/', parts);
        }

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
