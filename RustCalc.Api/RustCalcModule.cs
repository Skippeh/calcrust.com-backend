using System;
using System.Linq;
using Nancy;
using RustCalc.Common.Models;

namespace RustCalc.Api
{
    public abstract class RustCalcModule : NancyModule
    {
        private ExportData data;
        protected ExportData Data
        {
            get
            {
                if (data != null)
                    return data;

                string branch = Request.Headers["branch"].FirstOrDefault();

                if (branch == null)
                    return data = Program.Data[GameBranch.Public];

                GameBranch branchValue;
                if (Enum.TryParse(branch, out branchValue))
                {
                    return data = Program.Data[branchValue];
                }

                return data = null;
            }
        }

        public RustCalcModule(string modulePath) : base(modulePath)
        {
            
        }
    }
}