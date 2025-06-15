using GodotServiceFramework.Context.Service;
using ZiggyCreatures.Caching.Fusion;

namespace GodotServiceFramework.Cache;

/// <summary>
/// 
/// </summary>
public partial class FusionCacheService : AutoGodotService
{
    public readonly FusionCache Instance;

    public FusionCacheService()
    {
        var cache = new FusionCache(new FusionCacheOptions
        {
            CacheName = "default",
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromHours(1),
            }
        });
        Instance = cache;


        // var build = Host.CreateDefaultBuilder()
        //     .ConfigureServices(services =>
        //     {
        //         // 注册FusionCache
        //         services.AddFusionCache()
        //             .WithDefaultEntryOptions(new FusionCacheEntryOptions
        //             {
        //                 Duration = TimeSpan.FromMinutes(10)
        //             });
        //
        //         // 您的其他服务注册
        //         services.AddTransient<TaskWorkflowService>();
        //     })
        //     .Build();
        // build.StartAsync();
    }


    public override void Destroy()
    {
        Instance?.Dispose();
    }
}