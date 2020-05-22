using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Git.CredentialManager;

namespace Microsoft.DotNet
{
    class CommandContext : ICommandContext
    {
        public CommandContext(ICredentialStore credentialStore) => CredentialStore = credentialStore;

        public ICredentialStore CredentialStore { get; set; }

        public ISettings Settings => throw new NotImplementedException();

        public IStandardStreams Streams => throw new NotImplementedException();

        public ITerminal Terminal => throw new NotImplementedException();

        public bool IsDesktopSession => throw new NotImplementedException();

        public ITrace Trace { get; } = new NullTrace();

        public IFileSystem FileSystem => throw new NotImplementedException();

        public IHttpClientFactory HttpClientFactory => throw new NotImplementedException();

        public IGit Git => throw new NotImplementedException();

        public IEnvironment Environment => throw new NotImplementedException();

        public ISystemPrompts SystemPrompts => throw new NotImplementedException();

        public void Dispose() { }

        class NullTrace : ITrace
        {
            public bool HasListeners => false;

            public bool IsSecretTracingEnabled { get; set; }

            public void AddListener(TextWriter listener) { }

            public void Dispose() { }

            public void Flush() { }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
            public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

            public void WriteDictionarySecrets<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey[] secretKeys, IEqualityComparer<TKey>? keyComparer = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

            public void WriteException(Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

            public void WriteLine(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

            public void WriteLineSecrets(string format, object[] secrets, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }
        }
    }
}
