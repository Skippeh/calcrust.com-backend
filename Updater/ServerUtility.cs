using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PushbulletSharp.Models.Requests;
using Updater.Extensions;

namespace Updater
{
    public static class ServerUtility
    {
        public static Task<bool> RunServerUpdateApi(string serverRootPath)
        {
            return Task.Run(async () =>
            {
                // Copy oxide plugin.
                try
                {
                    string targetDirectory = $"{serverRootPath}server/rustcalc/oxide/plugins/";
                    Directory.CreateDirectory(targetDirectory);
                    File.Copy("./Data/RustExportData.cs", targetDirectory + "RustExportData.cs", true);
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine(ex);
                    return false;
                }

                string filePath = $"{serverRootPath}RustDedicated";

                if (!Program.RunningUnix)
                    filePath += ".exe";

                var process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.Arguments = "-batchmode" +
                                              " +server.ip 127.0.0.1" +
                                              " +server.identity \"rustcalc\"" +
                                              " +server.worldsize 1";
                process.StartInfo.WorkingDirectory = serverRootPath;

                int? processExitCode = await ProcessUtility.StartAndRedirectProcess(process, "[Server] ", 60 * 10 * 1000); // Timeout after 10 minutes.

                if (processExitCode == null)
                {
                    Console.Error.WriteLine("Server still running after 10 minutes, killed. Assuming it's failed.");

                    if (Program.Pushbullet != null)
                    {
                        using (var file = File.OpenRead($"{serverRootPath}server/rustcalc/Log.Error.txt"))
                        {
                            string bodyText = "Server still running after 10 minutes, assuming it's failed.";

                            if (file.Length > 0)
                            {
                                bodyText = "Rust Calculator\n\n" + bodyText;
                                await Program.Pushbullet.SendFileAsync(file, bodyText + "\n\nAttached is the error log.");
                            }
                            else
                            {
                                await Program.Pushbullet.SendNotificationAsync("Rust Calculator", bodyText);
                            }
                        }
                    }

                    return false;
                }

                try
                {
                    string exitData = File.ReadAllText($"{serverRootPath}/server/rustcalc/oxide/data/RustExportData_Exit.json");
                    JObject jExitData = JObject.Parse(exitData);

                    int exitCode = jExitData["exitCode"].Value<int>();
                    string error = jExitData["error"].Value<string>();

                    if (exitCode != 0)
                    {
                        Console.Error.WriteLine("Error server response: " + error);
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to read server exit data.");
                    Console.Error.WriteLine(ex);
                    return false;
                }
            });
        }
    }
}