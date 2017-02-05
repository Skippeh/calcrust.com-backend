using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Updater.Steam
{
    public static class DepotUtility
    {
        const string FilePath = "./ThirdParty/DepotDownloader/DepotDownloader.exe";

        public static Task<bool> DownloadAppAsync(uint appId, string branch, string fileList = null, string username = null, string password = null)
        {
            return Task.Run<bool>(async () =>
            {
                string installDir = $"{Program.LaunchArguments.InstallPath}{appId}-{branch}/";
                Directory.CreateDirectory(installDir);

                List<string> arguments = new List<string>
                {
                    { "-app" }, { appId.ToString() },
                    { "-beta" }, { branch },
                    { "-dir" }, { "\"" + installDir + "\"" }
                };

                if (fileList != null)
                {
                    arguments.Add($"-filelist {fileList}");
                }

                if (username != null)
                {
                    arguments.Add($"-username {username}");
                    arguments.Add($"-password {password}");
                }

                int? exitCode = await ProcessUtility.StartAndRedirectProcess(FilePath, $"[{appId}/{branch}] ", -1, arguments.ToArray());

                return exitCode == 0;
            });
        }
    }
}