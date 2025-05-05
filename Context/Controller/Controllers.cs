using System.Reflection;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.Util;
using Newtonsoft.Json;
using Tavis.UriTemplates;

namespace GodotServiceFramework.Context.Controller;

/// <summary>
/// Controller在设计中的作用是, 所有业务的入口
/// </summary>
/// <param name="name"></param>
/// <param name="lazy"></param>
[AttributeUsage(AttributeTargets.Class)]
public class GodotControllerAttribute(string name = "", bool lazy = true) : Attribute
{
    public string Resource { get; } = name;

    public bool Lazy { get; } = lazy;

    public GodotControllerAttribute() : this("")
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RequestMappingAttribute(string name = "", string method = "GET", string[]? aliases = null) : Attribute
{
    public string Resource { get; } = name;

    public string Method { get; } = method;

    public string[]? Aliases { get; } = aliases;

    public RequestMappingAttribute() : this("")
    {
    }
}

[Order(-999)]
public partial class Controllers : Service.AutoGodotService
{
    private static Controllers? Instance { get; set; }

    private readonly System.Collections.Generic.Dictionary<string, Type> _controllerTypes = [];

    private readonly
        System.Collections.Generic.Dictionary<string,
            System.Collections.Generic.Dictionary<(string m, string r), (Delegate d, UriTemplate u)>> _mappings = [];

    private readonly System.Collections.Generic.Dictionary<string, Delegate> _aliasesMappings = [];

    private readonly System.Collections.Generic.Dictionary<string, List<string>> _aliasesControllerMappings = [];

    /// <summary>
    /// 注册一些
    /// </summary>
    /// <param name="types"></param>
    public void Register(Type[] types)
    {
        foreach (var type in types)
        {
            try
            {
                Register(type);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }

    /// <summary>
    /// 注册一个controller,懒加载
    /// </summary>
    /// <param name="type"></param>
    public void Register(Type type)
    {
        var attribute = type.GetCustomAttribute<GodotControllerAttribute>();
        if (attribute == null)
        {
            return;
        }

        // if (attribute.Lazy) return;

        var instance = type.CreateInstanceForController();
        InitMapping(attribute.Resource, instance);

        _controllerTypes.TryAdd(attribute.Resource, type);
    }

    public static bool HasAlias(string alias)
    {
        return Instance!._aliasesMappings.ContainsKey(alias);
    }


    /// <summary>
    /// 删除一个controller
    /// </summary>
    /// <param name="name"></param>
    public void Remove(string name)
    {
        _controllerTypes.Remove(name);
        _mappings.Remove(name);
        if (_aliasesControllerMappings.TryGetValue(name, out var aliases))
        {
            foreach (var alias in aliases)
            {
                _aliasesMappings.Remove(alias);
            }
        }
    }


    private Dictionary<(string, string), (Delegate, UriTemplate)> InitMapping(
        string controller,
        object obj)
    {
        Dictionary<(string, string), (Delegate, UriTemplate)> dictionary = [];

        foreach (var methodInfo in obj.GetType().GetMethods()
                     .Where(info => info.GetCustomAttribute<RequestMappingAttribute>() != null))
        {
            var attribute = methodInfo.GetCustomAttribute<RequestMappingAttribute>();
            var @delegate = GetActionWithParamFromMethod(methodInfo, obj);
            var tuple = (@delegate,
                new UriTemplate(attribute!.Resource));
            dictionary[(attribute!.Method, attribute.Resource)] = tuple;

            attribute.Aliases?.ForEach(alias =>
            {
                if (!_aliasesControllerMappings.TryGetValue(controller, out var list))
                {
                    list = [];
                    _aliasesControllerMappings.Add(controller, list);
                }

                list.Add(alias);

                _aliasesMappings[alias] = @delegate;
            });
        }

        _mappings[controller] = dictionary;
        return dictionary;
    }

    /// <summary>
    /// 创建委托,不同的参数数量需要指定不同的Func...
    /// </summary>
    /// <param name="method"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private static Delegate GetActionWithParamFromMethod(MethodInfo method, object target)
    {
        try
        {
            var parameters = method.GetParameters();

            // 获取参数类型列表
            var types = parameters.Select(p => p.ParameterType).ToList();

            if (method.ReturnType == typeof(void))
            {
                var delegateType = parameters.Length switch
                {
                    0 => typeof(Action),
                    1 => typeof(Action<>).MakeGenericType(types.ToArray()),
                    2 => typeof(Action<,>).MakeGenericType(types.ToArray()),
                    3 => typeof(Action<,,>).MakeGenericType(types.ToArray()),
                    _ => throw new ArgumentException($"不支持 {parameters.Length} 个参数的方法")
                };

                return Delegate.CreateDelegate(delegateType, target, method);
            }
            else
            {
                // 添加返回类型
                types.Add(method.ReturnType);
                // 根据参数数量选择合适的 Func 类型
                var delegateType = parameters.Length switch
                {
                    0 => typeof(Func<>).MakeGenericType(types.ToArray()),
                    1 => typeof(Func<,>).MakeGenericType(types.ToArray()),
                    2 => typeof(Func<,,>).MakeGenericType(types.ToArray()),
                    3 => typeof(Func<,,,>).MakeGenericType(types.ToArray()),
                    _ => throw new ArgumentException($"不支持 {parameters.Length} 个参数的方法")
                };


                return Delegate.CreateDelegate(delegateType, target, method);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"创建委托失败: {ex.Message}", ex);
        }
    }

    public static void Invoke(string alias, params object?[]? args)
    {
        if (Instance!._aliasesMappings.TryGetValue(alias, out var @delegate))
        {
            @delegate.DynamicInvoke(args);
        }
    }

    public static GodotResult<T?> Invoke<T>(string alias, params object?[]? args)
    {
        if (Instance!._aliasesMappings.TryGetValue(alias, out var @delegate))
        {
            return DynamicInvoke<T>(@delegate, args);
        }

        return new GodotResult<T?>(404, default, "notfound");
    }

    public static void Invoke(string controller, string resource, string method = "GET", params object?[]? args)
    {
        if (!GetMappingDelegates(controller, out var delegates))
        {
            return;
        }


        foreach (var ((method0, resource0), (@delegate, uriTemplate)) in delegates!)
        {
            if (method0 != method) continue;
            if (resource0 == resource)
            {
                @delegate.DynamicInvoke(args);
                break;
            }

            var parameters = GetUriParameters(resource, uriTemplate);
            if (parameters is not { Count: > 0 }) continue;

            //TODO 这里可能不是很恰当，但后面再说
            var objects = NumberUtils.AutoInferTypes(parameters);

            @delegate.DynamicInvoke(args != null
                ? objects.Values.Concat(args).ToArray()
                : objects.Values.ToArray());

            break;
        }
    }

    public static GodotResult<TR?> Invoke<TR>(string controller, string resource, string method = "GET")
    {
        return Invoke<TR>(controller, resource, method, null);
    }

    public static GodotResult<object> InvokeVariant(string controller, string resource, string args = "{}",
        string method = "GET")
    {
        if (!GetMappingDelegates(controller, out var delegates))
        {
            return new GodotResult<object>(404, "", "notfound");
        }

        if (delegates == null) return new GodotResult<object>(404, "", "notfound");


        foreach (var ((method0, resource0), (@delegate, uriTemplate)) in delegates)
        {
            if (method0 != method) continue;
            if (resource0 == resource)
            {
                var parameterInfos = @delegate.Method.GetParameters();
                object result;
                switch (parameterInfos.Length)
                {
                    case 0:
                        result = @delegate.DynamicInvoke() ?? "";
                        break;
                    default:
                    {
                        var deserializeObject = JsonConvert.DeserializeObject(args, parameterInfos[0].ParameterType);
                        var targetArgs = new object?[parameterInfos.Length];
                        targetArgs[0] = deserializeObject;

                        result = @delegate.DynamicInvoke(targetArgs) ?? "";

                        if (result is Task<dynamic> task)
                        {
                            result = task.Result;
                        }

                        break;
                    }
                }

                return new GodotResult<object>(200, result, "success");
            }

            var parameters = GetUriParameters(resource, uriTemplate);
            if (parameters is not { Count: > 0 }) continue;

            @delegate.DynamicInvoke(parameters.Values.ToArray());
            break;
        }

        return new GodotResult<object>(200, "", "none");
    }

    private static bool GetMappingDelegates(string controller,
        out System.Collections.Generic.Dictionary<(string m, string r), (Delegate d, UriTemplate u)>? delegates)
    {
        var manager = Instance!;
        if (manager._mappings.TryGetValue(controller, out delegates)) return true;

        if (!manager._controllerTypes.TryGetValue(controller, out var type))
        {
            return false;
        }

        var instance = type.CreateInstanceForController();
        delegates = manager.InitMapping(controller, instance);

        return true;
    }

    public static GodotResult<TR?> Invoke<TR>(string controller,
        string resource, string method = "GET", params object?[]? args)
    {
        var manager = Instance!;
        if (!manager._mappings.TryGetValue(controller, out var value))
        {
            if (!manager._controllerTypes.TryGetValue(controller, out var type))
            {
                return new GodotResult<TR?>(404, default, "notfound");
            }

            var instance = type.CreateInstanceForController();
            value = manager.InitMapping(controller, instance);
        }

        foreach (var ((m, r), (d, u)) in value)
        {
            if (method != m) continue;
            if (resource == r)
            {
                return DynamicInvoke<TR>(d, args);
            }

            var parameters = GetUriParameters(resource, u);
            if (parameters is not { Count: > 0 }) continue;

            return DynamicInvoke<TR>(d,
                args != null ? parameters.Values.Concat(args).ToArray() : parameters.Values.ToArray());
        }

        return new GodotResult<TR?>(404, default, "notfound");
    }

    private static IDictionary<string, object> GetUriParameters(string resource, UriTemplate u)
    {
        var builder = new UriBuilder
        {
            Path = resource
        };

        var parameters = u.GetParameters(builder.Uri);
        return parameters;
    }


    /// <summary>
    /// 目前只能接受一个参数
    /// </summary>
    /// <param name="delegate"></param>
    /// <param name="args"></param>
    /// <typeparam name="TR"></typeparam>
    /// <returns></returns>
    private static GodotResult<TR?> DynamicInvoke<TR>(Delegate @delegate, params object?[]? args)
    {
        try
        {
            //这里默认参数类型没问题
            var result = @delegate.DynamicInvoke(args);

            return result switch
            {
                GodotResult<TR?> gr => gr,
                TR rt => new GodotResult<TR?>(200, rt, "success"),
                _ => new GodotResult<TR?>(400, default, $"unknown result type:{result?.GetType().Name}")
            };
        }
        catch (Exception e)
        {
            Log.Error(e);
            return new GodotResult<TR?>(500, default, e.Message);
        }
    }

    public override void Init()
    {
        Instance = this;
    }

    public override void Destroy()
    {
        Instance?._controllerTypes.Clear();
        Instance?._mappings.Clear();
        Instance = null;
    }
}

public class GodotResult<T>(int code, T data, string message)
{
    [JsonProperty("code")] public readonly int Code = code;

    [JsonProperty("data")] public readonly T Data = data;

    [JsonProperty("message")] public readonly string Message = message;

    public bool IsSuccess()
    {
        return Code == 200;
    }
}