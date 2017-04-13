using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Updater.Steam
{
    public static class DepotUtility
    {
        const string FilePath = "./SteamCMD/steamcmd.exe";

        public static Task<bool> DownloadAppAsync(uint appId, string branch, string username = null, string password = null)
        {
            return Task.Run<bool>(async () =>
            {
                string installDir = $"{Program.LaunchArguments.InstallPath}{appId}-{branch}/";
                Directory.CreateDirectory(installDir);

                List<string> arguments = new List<string>();

                if (username != null)
                {
                    arguments.Add($"+login {username} {password}");
                }
                else
                {
                    arguments.Add("+login anonymous");
                }

                arguments.AddRange(new[]
                {
                    $"+force_install_dir \"../{installDir}\"",
                    $"+app_update {appId}",
                    "validate",
                    $"-beta {branch}",
                    "+exit"
                });

                int? exitCode = await ProcessUtility.StartAndRedirectProcess(FilePath, $"[{appId}/{branch}] ", -1, arguments.ToArray());

                return exitCode == 0;
            });
        }
    }
}