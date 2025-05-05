using System.Net;
using System.Text;

namespace GodotServiceFramework.Util;

public class HttpUtils
{
    public static async Task ReturnResp(HttpListenerContext context, string responseBody)
    {
        // 返回响应
        // string responseString = $"Received body: {requestBody}";

        try
        {
            var buffer = Encoding.UTF8.GetBytes(responseBody);

            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
        }
        catch (Exception e)
        {
            Log.Warn(e);
        }
    }
}