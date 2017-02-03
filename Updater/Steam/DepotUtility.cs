using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Updater.Steam
{
    public static class DepotUtility
    {
        const string FilePath = "./ThirdParty/DepotDownloader/DepotDownloader.exe";

        public static Task<bool> DownloadAppAsync(int appId, string branch)
        {
            return Task.Run<bool>(() =>
            {
                string installDir = $"{Program.LaunchArguments.InstallPath}{appId}-{branch}/";

                Directory.CreateDirectory(installDir);

                var startInfo = new ProcessStartInfo(FilePath, $"-app {appId} -beta {branch} -dir {installDir}");
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                var process = new Process();

                DataReceivedEventHandler onOutput = (sender, args) =>
                {
                    Console.WriteLine("-" + args.Data);
                };

                DataReceivedEventHandler onError = (sender, args) =>
                {
                    Console.Error.WriteLine("-" + args.Data);
                };

                process.OutputDataReceived += onOutput;
                process.ErrorDataReceived += onError;

                process.StartInfo = startInfo;
                process.Start();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
            });
        }
    }
}