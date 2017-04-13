using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
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

                foreach (var filePath in Directory.GetFiles($"{clientRootPath}Bundles/items/", "*_small.png"))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (IOException) { /* ignore */ }
                }

                List<string> imagePaths = new List<string>(Directory.GetFiles($"{clientRootPath}Bundles/items/", "*.png"));
                string targetDir = Program.LaunchArguments.ImagesPath;

                Console.WriteLine("Creating thumbnails...");

                foreach (string imagePath in imagePaths.ToList())
                {
                    string directory = Path.GetDirectoryName(imagePath);
                    string fileName = Path.GetFileNameWithoutExtension(imagePath);
                    string extension = Path.GetExtension(imagePath);

                    using (Image originalImage = Image.FromFile(imagePath))
                    {
                        using (var thumbnail = GetThumbnailImage(originalImage, new Size(90, 90)))
                        {
                            string newPath = directory + "/" + fileName + "_small" + extension;
                            thumbnail.Save(newPath);
                            imagePaths.Add(newPath);
                        }
                    }
                }
                
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

        public static Image GetThumbnailImage(Image OriginalImage, Size ThumbSize)
        {
            Int32 thWidth = ThumbSize.Width;
            Int32 thHeight = ThumbSize.Height;
            Image i = OriginalImage;
            Int32 w = i.Width;
            Int32 h = i.Height;
            Int32 th = thWidth;
            Int32 tw = thWidth;
            if (h > w)
            {
                Double ratio = (Double)w / (Double)h;
                th = thHeight < h ? thHeight : h;
                tw = thWidth < w ? (Int32)(ratio * thWidth) : w;
            }
            else
            {
                Double ratio = (Double)h / (Double)w;
                th = thHeight < h ? (Int32)(ratio * thHeight) : h;
                tw = thWidth < w ? thWidth : w;
            }
            Bitmap target = new Bitmap(tw, th);
            Graphics g = Graphics.FromImage(target);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;
            Rectangle rect = new Rectangle(0, 0, tw, th);
            g.DrawImage(i, rect, 0, 0, w, h, GraphicsUnit.Pixel);
            return (Image)target;
        }
    }
}