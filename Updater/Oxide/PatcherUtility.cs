using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Updater.Oxide
{
    public static class PatcherUtility
    {
        public static Task<bool> PatchServer(string serverRootPath)
        {
            return Task.Run(async () =>
            {
                if (!Directory.Exists(serverRootPath))
                {
                    Console.Error.WriteLine("Server path not found: " + serverRootPath);
                    return false;
                }

                using (var webClient = new WebClient())
                {
                    try
                    {
                        string json = webClient.DownloadString("https://github.com/OxideMod/Oxide/raw/develop/Games/Unity/Oxide.Game.Rust/Rust.opj");

                        // Change the target directory.
                        JObject jObject = JObject.Parse(json);
                        jObject["TargetDirectory"] = Path.GetFullPath(serverRootPath + "RustDedicated_Data/Managed");
                        File.WriteAllText(serverRootPath + "patch.opj", JsonConvert.SerializeObject(jObject));

                        int exitCode = await ProcessUtility.StartAndRedirectProcess("./ThirdParty/OxidePatcher/OxidePatcher.exe", "[OxidePatcher] ",
                                                               "-c",
                                                               "-p", $"{serverRootPath}patch.opj");

                        if (exitCode != 0)
                        {
                            Console.WriteLine("OxidePatcher exit code: " + exitCode);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                        return false;
                    }
                }

                return true;
            });
        }
    }
}