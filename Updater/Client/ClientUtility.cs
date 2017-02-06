using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using Renci.SshNet;

namespace Updater.Client
{
    public static class ClientUtility
    {
        public static Task<bool> UploadImages(string clientRootPath)
        {
            if (clientRootPath == null) throw new ArgumentNullException(nameof(clientRootPath));

            return Task.Run(async () =>
            {
                var fileTransferer = new SSHFileTransferer();

                if (!await fileTransferer.Connect())
                {
                    return false;
                }

                string[] imagePaths = Directory.GetFiles($"{clientRootPath}Bundles/items/", "*.png");
                string targetDir = Program.LaunchArguments.ImagesPath;

                Console.WriteLine("Compressing images...");
                
                using (var fileStream = File.Create("images.temp.zip"))
                {
                    using (var zipFile = new ZipFile())
                    {
                        foreach (var filePath in imagePaths)
                        {
                            zipFile.AddFile(filePath, string.Empty);
                        }

                        zipFile.CompressionLevel = CompressionLevel.BestCompression;
                        zipFile.CompressionMethod = CompressionMethod.BZip2;
                        zipFile.Save(fileStream);
                    }

                    Console.WriteLine("Size: " + (fileStream.Length / 1024 / 1024) + " mb");
                    Console.WriteLine("Uploading archive...");

                    fileStream.Seek(0, SeekOrigin.Begin);
                    await fileTransferer.UploadFile($"{targetDir}images.zip", fileStream);
                }

                File.Delete("images.temp.zip");

                using (var sshClient = new SshClient(Program.LaunchArguments.SSHHost, Program.LaunchArguments.SSHHostPort,
                                                     Program.LaunchArguments.SSHUsername,
                                                     new PrivateKeyFile(Program.LaunchArguments.SSHPrivateKey, Program.LaunchArguments.SSHKeyPass)))
                {
                    sshClient.Connect();

                    Console.WriteLine("Unzipping...");
                    var command = sshClient.RunCommand($"unzip -o \"{targetDir}images.zip\" -d \"{targetDir}\"");

                    if (command.ExitStatus != 0)
                    {
                        Console.Error.WriteLine("Failed to unzip: " + command.Error);
                        return false;
                    }
                    
                    var removeCmd = sshClient.RunCommand($"rm \"{targetDir}images.zip\"");

                    if (!string.IsNullOrEmpty(removeCmd.Error))
                        Console.Error.WriteLine(removeCmd.Error);
                }

                fileTransferer.Disconnect();
                return true;
            });
        }
    }
}