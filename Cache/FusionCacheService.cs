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
    }


    // public new void Dispose()
    // {
    //     Instance?.Dispose();
    // }
    // public override void Dispose()
    // {
    //     Instance?.Dispose();
    // }
    public override void _ExitTree()
    {
        Instance.Dispose();
    }
}