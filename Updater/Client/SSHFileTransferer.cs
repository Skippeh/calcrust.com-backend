using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Updater.Client
{
    public class SSHFileTransferer : IDisposable
    {
        private SftpClient client;
        
        public Task<bool> Connect()
        {
            return Task.Run(() =>
            {
                try
                {
                    client = new SftpClient(Program.LaunchArguments.SSHHost, Program.LaunchArguments.SSHHostPort, Program.LaunchArguments.SSHUsername, new PrivateKeyFile(Program.LaunchArguments.SSHPrivateKey, Program.LaunchArguments.SSHKeyPass));
                    client.Connect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }

                return true;
            });
        }

        public void Disconnect()
        {
            client.Dispose();
            client = null;
        }
        
        public Task<bool> UploadFile(string filePath, Stream fileStream)
        {
            return Task.Run<bool>(async () =>
            {
                string directory = Path.GetDirectoryName(filePath).Replace("\\", "/");
                string fileName = Path.GetFileName(filePath);
                
                try
                {
                    if (!DirectoryExists(directory))
                    {
                        client.CreateDirectory(directory);
                    }

                    var task = client.BeginUploadFile(fileStream, filePath, true, null, fileStream);
                    await Task.Factory.FromAsync(task, client.EndUploadFile);
                    
                    Console.WriteLine($"Uploaded \"{fileName}\".");
                }
                catch (SftpPathNotFoundException ex)
                {
                    Console.Error.WriteLine($"Specified path could not be found: \"{directory}\"");
                    return false;
                }
                catch (SshException ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }

                return false;
            });
        }

        private bool DirectoryExists(string directory)
        {
            string workingDirectory = client.WorkingDirectory;

            try
            {
                client.ChangeDirectory(directory);
                return true;
            }
            catch (SftpPathNotFoundException)
            {
                return false;
            }
            finally
            {
                client.ChangeDirectory(workingDirectory);
            }
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
