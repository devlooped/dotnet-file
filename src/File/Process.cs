using System;
using System.Diagnostics;

namespace Microsoft.DotNet
{
    class Process
    {
        public static bool TryExecute(string program, string arguments, out string output)
        {
            var info = new ProcessStartInfo(program, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                var proc = System.Diagnostics.Process.Start(info);
                var gotError = false;
                proc.ErrorDataReceived += (_, __) => gotError = true;

                output = proc.StandardOutput.ReadToEnd();

                return !gotError && proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                output = ex.Message;
                return false;
            }
        }
    }
}
