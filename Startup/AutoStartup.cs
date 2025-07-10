using System.Reflection;
using Godot;
using GodotServiceFramework.Config;
using GodotServiceFramework.Context.Component;
using GodotServiceFramework.Context.Controller;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Startup;

public static class AutoStartup
{
    private const string Banner =
        """
         _____ _                           
        /  ___(_)                          
        \ `--. _  __ _ _ __ ___  _   _ ___ 
         `--. \ |/ _` | '_ ` _ \| | | / __|
        /\__/ / | (_| | | | | | | |_| \__ \
        \____/|_|\__, |_| |_| |_|\__,_|___/
                  __/ |                    
                 |___/                     
        ================================================================================================================
        """;

    public static void Initialize()
    {
        try
        {
            if (Engine.IsEditorHint()) return;

            //日志和配置中心是第一个加载的
            RegisterService(typeof(MyLogger));
            Log.Info(Banner, BbColor.Aqua);
            RegisterService(typeof(ConfigStore));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();


            List<Type> componentList = [];
            foreach (var assembly in assemblies)
            {
                var godotGameAttribute = assembly.GetCustomAttribute<GodotGameAttribute>();
                if (godotGameAttribute == null) continue;
                Log.Info($"Loading GodotGame Assembly : {assembly.FullName} - {godotGameAttribute.Label}");

                var list = SyncLoadService(assembly);
                componentList.AddRange(list);
            }

            var types = componentList.OrderBy(s =>
                s.GetCustomAttribute<OrderAttribute>() == null
                    ? Constants.DefaultOrderIndex
                    : s.GetCustomAttribute<OrderAttribute>()!.Index);

            foreach (var iService in types)
            {
                RegisterService(iService);
            }

            foreach (var assembly in assemblies)
            {
                AsyncLoadService(assembly);
                AsyncLoadController(assembly);
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
            e.PrintFormatted();
        }
    }


    /// <summary>
    /// controller是属于Controllers的子对象
    /// </summary>
    /// <param name="assembly"></param>
    private static void AsyncLoadController(Assembly assembly)
    {
        _ = Task.Run(() =>
        {
            var controllerTypes = assembly.GetTypes()
                .Where(type =>
                    type.GetCustomAttribute<GodotControllerAttribute>() != null
                );
            var array = controllerTypes.ToArray();
            if (array.Length == 0) return Task.CompletedTask;
            Services.Get<Controllers>()!.Register(array);
            return Task.CompletedTask;
        });
    }


    /// <summary>
    /// 异步加载service
    /// </summary>
    /// <param name="assembly"></param>
    private static void AsyncLoadService(Assembly assembly)
    {
        var componentScanTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AsyncComponentScanAttribute>() != null
                           ||
                           type.GetConstructors().Any(info =>
                               info.GetCustomAttribute<GodotControllerAttribute>() != null)
            );

        foreach (var scanType in componentScanTypes)
        {
            var componentScanAttribute = scanType.GetCustomAttribute<AsyncComponentScanAttribute>();
            if (componentScanAttribute?.Values == null) continue;
            var types = componentScanAttribute.Values;
            _ = Task.Run(() => RegisterServices(types));
        }
    }

    /// <summary>
    /// 注册组件
    /// </summary>
    /// <param name="types"></param>
    private static void RegisterServices(Type[] types)
    {
        foreach (var type in types)
        {
            RegisterService(type);
        }
    }


    /// <summary>
    /// 注册一个GodotObject作为service, 这里我们要求Service的name必须唯一
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static void RegisterService(Type type)
    {
        if (type.IsAbstract || type.IsInterface ||
            Services.Has(type)) return;

        Services.Add(Activator.CreateInstance(type)!);
    }


    /// <summary>
    /// 同步加载service
    /// 分别加载单独在注解中标记的类型,这些类必须继承自GodotObject
    ///
    /// 以及直接加载继承了GodotService的类, 这些类我们规定不需要单独声明就自动加载到Services中.
    /// </summary>
    /// <param name="assembly"></param>
    private static List<Type> SyncLoadService(Assembly assembly)
    {
        List<Type> types = [];
        var componentScanTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<ComponentScanAttribute>() != null);

        foreach (var scanType in componentScanTypes)
        {
            var componentScanAttribute = scanType.GetCustomAttribute<ComponentScanAttribute>();
            if (componentScanAttribute?.Values == null) continue;

            // 加载必要的Service,这些service不会被销毁
            types.AddRange(componentScanAttribute.Values);
        }

        types.AddRange(assembly.GetTypes().Where(type =>
            type.GetCustomAttributes().Any(attribute => attribute.GetType() == typeof(InjectServiceAttribute))));
        return types;
    }
}