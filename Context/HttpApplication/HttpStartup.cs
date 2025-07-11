using System.Runtime.InteropServices;
using GodotServiceFramework.Config;
using GodotServiceFramework.Util;
using Grapevine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotServiceFramework.Context.HttpApplication;

public class HttpStartup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; private set; } = configuration;

    // private readonly string _serverPort = PortFinder.FindNextLocalOpenPort(10234);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => { loggingBuilder.ClearProviders(); });
    }

    public void ConfigureServer(IRestServer server)
    {
        //这里是避免抢占程序集默认接口, 单独创建的httpServer, 都不允许从程序集中获取
        // server.RouteScanner.AddIgnoredAssembly("GodotServiceFramework");


        if (!ConfigStore.TryGet<int>("server.port", out var port))
        {
            port = 10234;
        }


        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsUtils.OpenFirewallPort(port);
        }

        server.Prefixes.Add($"http://+:{PortFinder.FindNextLocalOpenPort(port)}/");

        /* Configure server to auto parse application/x-www-for-urlencoded data*/
        server.AutoParseFormUrlEncodedData();

        /* Configure Router Options (if supported by your router implementation) */
        server.Router.Options.SendExceptionMessages = true;
    }
}