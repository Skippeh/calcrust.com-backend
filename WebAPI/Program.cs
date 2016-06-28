using System;
using Mono.Unix;
using Mono.Unix.Native;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebAPI
{
    public class Program
    {
        static void Main(string[] args)
        {
            DataManager.Start("data/rust.json");

            using (var host = new NancyHost(new ApiBootstrapper(), new HostConfiguration() { UrlReservations = new UrlReservations { CreateAutomatically = true } }, new Uri("http://localhost:7545")))
            {
                host.Start();

                Console.WriteLine("Starting api server on port 7545...");

                if (Type.GetType("Mono.Runtime") != null)
                {
                    Console.WriteLine("Mono detected, press CTRL+C to stop the server.");
                    UnixSignal.WaitAny(new[]
                    {
                        new UnixSignal(Signum.SIGINT),
                        new UnixSignal(Signum.SIGTERM),
                        new UnixSignal(Signum.SIGQUIT),
                        new UnixSignal(Signum.SIGHUP)
                    });
                }
                else
                {
                    Console.WriteLine("Press CTRL+Q to stop the server.");
                    ConsoleKeyInfo consoleKeyInfo;
                    while ((consoleKeyInfo = Console.ReadKey(true)).Key != ConsoleKey.Q && consoleKeyInfo.Modifiers != ConsoleModifiers.Control)
                        continue;
                }

                Console.WriteLine("Stopping api server...");
                host.Stop();
            }

            DataManager.Stop();
        }
    }

    internal sealed class ApiBootstrapper : DefaultNancyBootstrapper
    {
        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            // Disable /_nancy diagnostics page.
            DiagnosticsHook.Disable(pipelines);

            StaticConfiguration.DisableErrorTraces = false;

            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                            .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                            .WithHeader("Access-Control-Allow-Headers", "Accept,Origin,Content-Type");
            });
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<JsonSerializer, ApiJsonSerializer>();
        }
    }

    internal sealed class ApiJsonSerializer : JsonSerializer
    {
        public ApiJsonSerializer()
        {
            Formatting = Formatting.None;
            ContractResolver = new CamelCasePropertyNamesContractResolver();
        }
    }
}