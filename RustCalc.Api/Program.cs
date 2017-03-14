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
            Exception loadException = LoadBranch("public");

            if (loadException != null)
            {
                Console.Error.WriteLine("Failed to load public branch:");
                Console.Error.WriteLine(loadException.ToString());
            }
        }

        private static Exception LoadBranch(string branchName)
        {
            try
            {
                using (var fs = File.OpenRead($"data/{branchName}/rustcalc-export.bin"))
                {
                    var reader = new BinaryReader(fs);
                    var data = ExportManager.DeserializeData(reader);
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