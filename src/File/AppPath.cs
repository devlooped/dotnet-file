using System;
using GitCredentialManager;

namespace Devlooped;

static class AppPath
{
    public static string Default { get; } = CommandContext.GetEntryApplicationPath();
}
