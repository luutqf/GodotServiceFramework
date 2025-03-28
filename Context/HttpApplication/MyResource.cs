using Godot;
using Godot.Collections;
using GodotServiceFramework.Util;
using Grapevine;
using Newtonsoft.Json;

namespace GodotServiceFramework.Context.HttpApplication;

[RestResource]
public class MyResource
{
    [RestRoute("Any", @"^.*$")]
    public async Task Any(IHttpContext context)
    {
        await HttpInvoke(context);
    }

    private static async Task HttpInvoke(IHttpContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream);

        var text = await reader.ReadToEndAsync();

        Logger.Info(context.Request.Endpoint);

        var segments = context.Request.Endpoint.Split('/');
        if (segments.Length <= 3)
        {
            context.Response.StatusCode = 400;
        }
        else
        {
            var control = segments[2];
            var resource = string.Join("/", segments.Skip(3));
            Logger.Info($"control: {control}");
            Logger.Info($"resource: {resource}");
            try
            {
                var result =
                    Controller.Controllers.InvokeVariant(control, resource, text, context.Request.HttpMethod.Method);
                context.Response.Headers.Add("Content-Type", "application/json");

                await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                await context.Response.SendResponseAsync(
                    HttpStatusCode.InternalServerError, e.Message);
            }
        }


        //
        await context.Response.SendResponseAsync(HttpStatusCode.NotFound, "notfound");
    }
}