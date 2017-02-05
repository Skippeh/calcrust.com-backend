using System.IO;
using System.Threading.Tasks;

namespace Updater.Steam
{
    public static class DepotUtility
    {
        const string FilePath = "./ThirdParty/DepotDownloader/DepotDownloader.exe";

        public static Task<bool> DownloadAppAsync(uint appId, string branch)
        {
            return Task.Run<bool>(async () =>
            {
                string installDir = $"{Program.LaunchArguments.InstallPath}{appId}-{branch}/";
                Directory.CreateDirectory(installDir);

                int exitCode = await ProcessUtility.StartAndRedirectProcess(FilePath, $"[{appId}/{branch}] ",
                                                                            "-app", appId.ToString(),
                                                                            "-beta", branch,
                                                                            "-dir", installDir);

                return exitCode == 0;
            });
        }
    }
}