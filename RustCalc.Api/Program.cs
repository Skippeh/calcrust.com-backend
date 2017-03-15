using System;
using System.Collections.Generic;
using System.IO;
using RustCalc.Common.Exporting;
using RustCalc.Common.Models;

namespace RustCalc.Api
{
    public static class Program
    {
        public static Dictionary<GameBranch, ExportData> Data { get; private set; }

        static Program()
        {
            Data = new Dictionary<GameBranch, ExportData>();
        }

        static void Main(string[] args)
        {
            foreach (GameBranch branch in Enum.GetValues(typeof (GameBranch)))
            {
                Exception loadException = LoadBranch(branch);

                if (loadException != null)
                {
                    Console.Error.WriteLine($"Failed to load branch {branch}:");

                    if (!(loadException is FileNotFoundException) && !(loadException is DirectoryNotFoundException))
                        Console.Error.WriteLine(loadException.ToString());
                    else
                    {
                        Console.Error.WriteLine(loadException.Message);
                    }
                }
            }
        }

        private static Exception LoadBranch(GameBranch branch)
        {
            try
            {
                using (var fs = File.OpenRead($"data/{branch}/rustcalc-export.bin"))
                {
                    var reader = new BinaryReader(fs);
                    var data = ExportManager.DeserializeData(reader);
                    Data.Add(branch, data);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}