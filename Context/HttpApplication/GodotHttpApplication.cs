using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;
using Grapevine;

namespace GodotServiceFramework.Context.HttpApplication;

/// <summary>
/// 这里的作用是,
/// </summary>
[InjectService]
public partial class AutoGodotHttpApplication : IDisposable
{
    private readonly IRestServer? _restServer;

    public AutoGodotHttpApplication()
    {
        Log.Info("初始化http server");

        try
        {
            _restServer = RestServerBuilder
                .From<HttpStartup>()
                .Build();
            _restServer.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"无法创建HttpServer,请确认软件权限 {e}");
        }
    }


    public void Dispose()
    {
        _restServer?.Stop();
    }
}