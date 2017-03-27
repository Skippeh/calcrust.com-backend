using System;
using System.Linq;
using Nancy;
using RustCalc.Common.Models;

namespace RustCalc.Api
{
    public abstract class RustCalcModule : NancyModule
    {
        protected ExportData Data { get; private set; }

        public RustCalcModule(string modulePath) : base(modulePath)
        {
            Before.AddItemToEndOfPipeline(context =>
            {
                string branch = context.Request.Headers["branch"].FirstOrDefault() ?? "public";

                GameBranch gameBranch;
                if (!Enum.TryParse(branch, true, out gameBranch))
                {
                    return new Response
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ContentType = "application/json",
                        ReasonPhrase = "Branch Not Found"
                    };
                }

                if (!Program.Data.ContainsKey(gameBranch))
                {
                    return new Response
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        ContentType = "application/json",
                        ReasonPhrase = "Branch Not Loaded"
                    };
                }

                Data = Program.Data[gameBranch];
                return null;
            });
        }
    }
}