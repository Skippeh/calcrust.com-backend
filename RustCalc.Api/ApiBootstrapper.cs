using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RustCalc.Api
{
    public class ApiBootstrapper : DefaultNancyBootstrapper
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

    public sealed class ApiJsonSerializer : JsonSerializer
    {
        public ApiJsonSerializer()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver();
        }
    }
}