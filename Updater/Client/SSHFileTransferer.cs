using System;
using System.IO;
using System.Threading.Tasks;

namespace Updater.Client
{
    public class SSHFileTransferer : IFileTransferer
    {
        public string PublicKeyPath;

        public SSHFileTransferer(string publicKeyPath)
        {
            if (publicKeyPath == null) throw new ArgumentNullException(nameof(publicKeyPath));
            PublicKeyPath = publicKeyPath;
        }

        public async Task<bool> Connect()
        {
            string publicKey;

            try
            {
                using (var reader = File.OpenText(PublicKeyPath))
                {
                    publicKey = await reader.ReadToEndAsync();
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("Failed to read public key: " + ex);
                return false;
            }



            return true;
        }

        public void Disconnect()
        {

        }
        
        public async Task<bool> UploadFile(string path)
        {
            return false;
        }
    }
}
