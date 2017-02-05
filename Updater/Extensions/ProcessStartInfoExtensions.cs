using System.Diagnostics;

namespace Updater.Extensions
{
    public static class ProcessStartInfoExtensions
    {
        /// <summary>Modifies the filename and arguments to target mono instead.</summary>
        public static void UseMonoIfUnix(this ProcessStartInfo startInfo)
        {
            if (Program.RunningUnix)
            {
                startInfo.FileName = "/usr/bin/mono"; // Todo: Don't assume mono location
                startInfo.Arguments = startInfo.FileName + " " + startInfo.Arguments;
            }
        }
    }
}