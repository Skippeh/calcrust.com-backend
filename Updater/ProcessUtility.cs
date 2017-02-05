using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Updater.Extensions;

namespace Updater
{
    public static class ProcessUtility
    {
        /// <summary>Starts the process with the specified arguments and then redirects the output to the console with the specified prefix. Returns the exit code, or null if process time exceeded timeout.</summary>
        /// <param name="timeout">Max milliseconds to wait before forcefully closing process and failing.</param>
        public static Task<int?> StartAndRedirectProcess(string fileName, string outputPrefix = "-", int timeout = -1, params string[] arguments)
        {
            for (int i = 0; i < arguments.Length; ++i)
            {
                if (arguments[i].Contains(" "))
                {
                    arguments[i] = "\"" + arguments[i] + "\"";
                }
            }

            string strArguments = string.Join(" ", arguments);

            var startInfo = new ProcessStartInfo();
            var process = new Process();
            process.StartInfo = startInfo;

            startInfo.FileName = fileName;
            startInfo.Arguments = strArguments;
            startInfo.UseMonoIfUnix();

            return StartAndRedirectProcess(process, outputPrefix);
        }

        /// <summary>Starts the specified process and then redirects the output to the console with the specified prefix. Returns the exit code, or null if process time exceeded timeout.</summary>
        /// <param name="timeout">Max milliseconds to wait before forcefully closing process and failing.</param>
        public static Task<int?> StartAndRedirectProcess(Process process, string outputPrefix = "-", int timeout = -1)
        {
            return Task.Run<int?>(() =>
            {
                var startInfo = process.StartInfo;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                DataReceivedEventHandler onOutput = (sender, args) =>
                {
                    Console.WriteLine(outputPrefix + args.Data);
                };

                DataReceivedEventHandler onError = (sender, args) =>
                {
                    Console.Error.WriteLine(outputPrefix + args.Data);
                };

                process.OutputDataReceived += onOutput;
                process.ErrorDataReceived += onError;
                
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                if (!process.WaitForExit(timeout))
                {
                    process.Kill();
                    return null;
                }

                return process.ExitCode;
            });
        } 
    }
}