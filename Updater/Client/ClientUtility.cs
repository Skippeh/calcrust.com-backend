using System;
using System.IO;
using System.Threading.Tasks;

namespace Updater.Client
{
    public static class ClientUtility
    {
        public static Task<bool> UploadImages(string clientRootPath, IFileTransferer fileTransferer)
        {
            if (clientRootPath == null) throw new ArgumentNullException(nameof(clientRootPath));
            if (fileTransferer == null) throw new ArgumentNullException(nameof(fileTransferer));

            return Task.Run(async () =>
            {
                if (!await fileTransferer.Connect())
                {
                    return false;
                }

                string[] imagePaths = Directory.GetFiles($"{clientRootPath}Bundles/items/", "*.png", SearchOption.AllDirectories);

                foreach (string imagePath in imagePaths)
                {
                    Console.WriteLine(imagePath);
                }

                fileTransferer.Disconnect();
                return true;
            });
        }
    }
}