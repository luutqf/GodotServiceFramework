using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;
using Grapevine;
using Newtonsoft.Json;

namespace GodotServiceFramework.Context.HttpApplication;

[RestResource]
public class MyResource
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    [RestRoute("Get", "services")]
    public async Task GetServices(IHttpContext context)
    {
        var content = JsonConvert.SerializeObject(Services.ServiceNames);
        Log.Info($"req: {context.Request.Url} \nrep: {content}", BbColor.Aqua);
        context.Response.Headers.Add("Content-Type", "application/json");
        await context.Response.SendResponseAsync(content);
    }

    [RestRoute("Any", @"^.*/test$")]
    public async Task Test(IHttpContext context)
    {
        Log.Info(context.Request.Headers);

        await context.Response.SendResponseAsync(HttpStatusCode.NotFound, context.Request.Headers.ToString());
    }

    private static async Task HttpInvoke(IHttpContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream);

        var text = await reader.ReadToEndAsync();

        Log.Info(context.Request.Endpoint);

        var segments = context.Request.Endpoint.Split('/');
        if (segments.Length <= 3)
        {
            context.Response.StatusCode = 400;
        }
        else
        {
            var control = segments[2];
            var resource = string.Join("/", segments.Skip(3));
            Log.Info($"control: {control}");
            Log.Info($"resource: {resource}");
            try
            {
                var result =
                    Controller.Controllers.InvokeVariant(control, resource, text, context.Request.HttpMethod.Method);
                context.Response.Headers.Add("Content-Type", "application/json");

                await context.Response.SendResponseAsync(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                Log.Warn(e);
                await context.Response.SendResponseAsync(
                    HttpStatusCode.InternalServerError, e.Message);
            }
        }


        //
        await context.Response.SendResponseAsync(HttpStatusCode.NotFound, "notfound");
    }
}