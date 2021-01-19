using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Devlooped
{
    class FileSpecComparer : IEqualityComparer<FileSpec>
    {
        public bool Equals([AllowNull] FileSpec x, [AllowNull] FileSpec y)
            => Equals(x?.Path, y?.Path);

        public int GetHashCode([DisallowNull] FileSpec obj) => obj.Path.GetHashCode();
    }
}
