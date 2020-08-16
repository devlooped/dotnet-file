using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.DotNet
{
    class Process
    {
        public static bool TryExecute(string program, string arguments, out string output)
        {
            var info = new ProcessStartInfo(program, arguments);
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            var proc = System.Diagnostics.Process.Start(info);
            var gotError = false;
            proc.ErrorDataReceived += (_, __) => gotError = true;

            output = proc.StandardOutput.ReadToEnd();
            
            return !gotError && proc.ExitCode == 0;
        }
    }
}
