using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Ionic.Zip;
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

                List<string> patchedFiles = new List<string>();

                using (var webClient = new WebClient())
                {
                    try
                    {
                        string json = webClient.DownloadString("https://github.com/OxideMod/Oxide/raw/develop/Games/Unity/Oxide.Game.Rust/Rust.opj");

                        JObject jObject = JObject.Parse(json);

                        // Change the target directory.
                        jObject["TargetDirectory"] = Path.GetFullPath(serverRootPath + "RustDedicated_Data/Managed");

                        // Save assembly files that will be patched
                        foreach (JToken jManifest in jObject["Manifests"].Value<JArray>())
                        {
                            string assemblyName = jManifest["AssemblyName"].Value<string>();
                            Console.WriteLine("Adding patched file: " + assemblyName);
                            patchedFiles.Add(assemblyName);
                        }

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

                return await InstallOxide(serverRootPath, patchedFiles.ToArray());
            });
        }

        private static async Task<bool> InstallOxide(string serverRootPath, string[] patchedFiles)
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    var lockObject = new object();
                    webClient.DownloadProgressChanged += (sender, args) =>
                    {
                        lock (lockObject)
                        {
                            Console.CursorTop -= 1;
                            Console.WriteLine(args.ProgressPercentage + "%");
                        }
                    };

                    Console.WriteLine("Download Oxide for Rust...");
                    Console.WriteLine("0%");
                    await webClient.DownloadFileTaskAsync("https://github.com/OxideMod/Snapshots/raw/master/Oxide-Rust.zip", $"{serverRootPath}oxide.zip");
                }
                catch (WebException ex)
                {
                    Console.Error.WriteLine(ex);
                    return false;
                }
            }
            
            await Task.Run(() =>
            {
                using (ZipFile zipFile = ZipFile.Read($"{serverRootPath}oxide.zip"))
                {
                    // Remove patched files
                    foreach (string assemblyFile in patchedFiles)
                    {
                        Console.WriteLine($"Removing {assemblyFile} from zip file.");
                        zipFile.RemoveSelectedEntries(assemblyFile, "RustDedicated_Data/Managed/");
                    }

                    Console.WriteLine("Extracting Oxide.");
                    zipFile.ExtractAll(serverRootPath, ExtractExistingFileAction.OverwriteSilently);
                }
            });

            // Cleanup files
            try
            {
                File.Delete($"{serverRootPath}oxide.zip");
                File.Delete($"{serverRootPath}patch.opj");
            }
            catch (IOException) { /* ignore */ }

            return true;
        }
    }
}