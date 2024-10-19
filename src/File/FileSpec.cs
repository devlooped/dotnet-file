using System;
using System.Diagnostics;
using System.Linq;

namespace Devlooped
{
    [DebuggerDisplay("{Path}")]
    public class FileSpec
    {
        public static FileSpec WithPath(string path, Uri uri)
        {
            // If path == '.', persist directly on the current directory, which matches
            // the old behavior
            if (path == ".")
                return new FileSpec(uri);
            else if (path.Split('/', '\\').LastOrDefault() == ".")
                return WithDefaultPath(uri, path);
            else
                return new FileSpec(path, uri, null, null);
        }

        public static FileSpec WithDefaultPath(Uri uri, string baseDir = "")
            => WithGitHubUri(baseDir, uri,
                raw => WithGitHubRawUri(baseDir, raw,
                    fallback => new FileSpec(fallback)));

        static FileSpec WithGitHubUri(string baseDir, Uri uri, Func<Uri, FileSpec> next)
        {
            var parts = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var flatten = baseDir.Split('/', '\\').LastOrDefault() == ".";

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
                return new FileSpec(flatten
                    ? baseDir // Flattened folder structure, just use base dir as target path
                    : baseDir.Length == 0 // Otherwise, perform some heuristics to determine the target path
                        ? uri.PathAndQuery.EndsWith('/')
                            // A URI ending in / has a similar effect to a target path ending in /, meaning 
                            // Copy the whole directory structure to the target directory from that path onwards, 
                            // without prepending the source (web) directory structure.
                            ? ""
                            // Otherwise, just like previous behavior, prepend upstream dir structure.
                            : string.Join('/', parts.Skip(4))
                        : System.IO.Path.Combine(baseDir, string.Join('/', parts.Skip(4))).Replace('\\', '/') + "/",
                    uri, finalPath: true);
            }
            else if (parts[2] == "blob" || parts[2] == "raw")
            {
                // This is a specific file URL, so use that as the target path.
                // Note we skip 4 parts since those are org/repo/[blob/raw]/branch.
                return new FileSpec(
                        flatten ?
                        System.IO.Path.Combine(baseDir[..^1], parts[^1]) :
                        baseDir.Length == 0 ? string.Join('/', parts.Skip(4)) :
                        System.IO.Path.Combine(baseDir, string.Join('/', parts.Skip(4))).Replace('\\', '/'),
                    uri, finalPath: true);
            }
            else
            {
                // Couldn't be figure it out, just add with no smarts
                return next(uri);
            }
        }

        static FileSpec WithGitHubRawUri(string baseDir, Uri uri, Func<Uri, FileSpec> next)
        {
            var parts = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var flatten = baseDir.Split('/', '\\').LastOrDefault() == ".";

            // https://raw.githubusercontent.com/kzu/dotnet-file/main/README.md
            // Raw always points to a specific file URL, so use that as the target path.
            // Note we skip 4 parts since those are org/repo/[blob/raw]/branch.
            if (uri.Host == "raw.githubusercontent.com" && parts.Length > 3)
                return new FileSpec(
                        flatten ?
                        System.IO.Path.Combine(baseDir[..^1], parts[^1]) :
                        baseDir.Length == 0 ? string.Join('/', parts.Skip(3)) :
                        System.IO.Path.Combine(baseDir, string.Join('/', parts.Skip(3))).Replace('\\', '/'),
                    uri, finalPath: true);

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

            if (!finalPath && uri != null && !uri.AbsolutePath.EndsWith('/') &&
                (path.EndsWith('\\') || path.EndsWith('/')))
            {
                path = System.IO.Path.Combine(path, WithDefaultPath(uri!).Path);
            }

            // This will also normalize double slashes.
            var parts = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0] == ".")
                Path = string.Join('/', parts.Skip(1));
            else
                Path = string.Join('/', parts);

            // Preserve trailing slash if present, for consistency with url ending in /.
            if (path.EndsWith('/') || path.EndsWith("\\"))
                Path += "/";
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
