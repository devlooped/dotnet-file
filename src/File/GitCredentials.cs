using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GitHub;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.DotNet
{
    class GitCredentials
    {
        Dictionary<string, ICredential> credentials = new Dictionary<string, ICredential>();

        public Task<ICredential> GetCredentials(Uri uri)
        {
            if (credentials.TryGetValue(uri.Host, out var creds))
                return Task.FromResult(creds);

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = uri.Host,
            });

            var store = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                (ICredentialStore)WindowsCredentialManager.Open() :
                (ICredentialStore)MacOSKeychain.Open();

            var provider = new GitHubHostProvider(new Context(store));

            return provider.GetCredentialAsync(input);
        }

        class Context : ICommandContext
        {
            public Context(ICredentialStore credentialStore) => CredentialStore = credentialStore;

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

                public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

                public void WriteDictionarySecrets<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey[] secretKeys, IEqualityComparer<TKey> keyComparer = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

                public void WriteException(Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

                public void WriteLine(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }

                public void WriteLineSecrets(string format, object[] secrets, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "") { }
            }
        }
    }
}
