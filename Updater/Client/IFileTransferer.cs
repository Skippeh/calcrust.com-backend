using System.Threading.Tasks;

namespace Updater.Client
{
    public interface IFileTransferer
    {
        Task<bool> Connect();
        void Disconnect();
        Task<bool> UploadFile(string path);
    }
}