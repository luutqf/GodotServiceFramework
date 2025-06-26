// using System.Net;
// using System.Runtime.InteropServices;
// using DotNet.Globbing;
// using GodotServiceFramework.Util;
//
// namespace GodotServiceFramework.Http.Server;
//
// public class SimpleHttpServer : ICloseable
// {
//     public static event Action<HttpListenerContext> RequestHandler;
//
//     private readonly HttpListener _listener;
//
//     private readonly int _port;
//
//     private static SimpleHttpServer? _instance;
//
//     private static readonly HashSet<Route> Routes = [];
//
//     static SimpleHttpServer()
//     {
//         RequestHandler += HandleRequest;
//     }
//
//     public static SimpleHttpServer GetInstance()
//     {
//         return _instance!;
//     }
//
//     public static SimpleHttpServer CreateServer(int port)
//     {
//         if (_instance?._port == port)
//         {
//             return _instance;
//         }
//
//         _instance?.Close();
//         _instance = new SimpleHttpServer(port);
//
//         return _instance;
//     }
//
//     public void Close()
//     {
//         _listener.Close();
//         _instance = null;
//         Routes.Clear();
//     }
//
//
//     public SimpleHttpServer(int port)
//     {
//         if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//         {
//             WindowsUtils.OpenFirewallPort(port);
//         }
//
//         _listener = new HttpListener();
//         _port = port;
//
//         Task.Run(() =>
//         {
//             _listener.Prefixes.Add($"http://+:{port}/");
//             _listener.Start();
//             try
//             {
//                 while (true)
//                 {
//                     // 等待并获取客户端请求
//                     var context = _listener.GetContext(); // 同步方法，也可以使用异步方法
//                     Task.Run(() => RequestHandler.Invoke(context)); // 使用 Task.Run 异步处理每个请求
//                 }
//             }
//             catch (HttpListenerException e)
//             {
//                 Log.Error(e);
//             }
//             finally
//             {
//                 _listener.Close();
//             }
//         });
//     }
//
//     private static async void HandleRequest(HttpListenerContext context)
//     {
//         var port = context.Request.LocalEndPoint.Port;
//         // GD.Print($"local port: {port}");
//         if (context.Request.Url == null) return;
//
//         var requestPath = context.Request.Url.AbsolutePath;
//         var requestMethod = context.Request.HttpMethod;
//
//
//         // 查找匹配的路由
//         foreach (var route in Routes.Where(route =>
//                      MatchRoutePath(route.PathGlob, requestPath) &&
//                      MatchRouteMethod(route.Method, requestMethod)))
//         {
//             await route.Handler(context);
//             return;
//         }
//
//         // 处理404未找到的情况
//         context.Response.StatusCode = (int)HttpStatusCode.NotFound;
//         var responseBytes = "404 - Not Found"u8.ToArray();
//         await context.Response.OutputStream.WriteAsync(responseBytes);
//         context.Response.Close();
//     }
//
//     private static bool MatchRoutePath(Glob rule, string target)
//     {
//         return rule.IsMatch(target);
//     }
//
//     private static bool MatchRouteMethod(string rule, string target)
//     {
//         return rule == "*" || string.Equals(rule, target, StringComparison.OrdinalIgnoreCase);
//     }
//
//
//
//     // 注册路由
//     // public static void RegisterRoute(string path, string method, Func<HttpListenerContext, Task> handler)
//     // {
//     //     Routes.Add(new Route(path, method, handler));
//     // }
//
//     public static void RegisterRoute(Route route)
//     {
//         if (Routes.Contains(route))
//         {
//             Log.Error($"{route} is already registered");
//             return;
//         }
//
//         Console.WriteLine($"Registering route {route}");
//         Routes.Add(route);
//     }
//
//     public static void DeleteRoute(Route route)
//     {
//         Routes.Remove(route);
//     }
//
//
//     public static void Test01()
//     {
//         var glob = Glob.Parse("**");
//         var isMatch = glob.IsMatch("sdfs/2123123");
//         Console.WriteLine(isMatch);
//     }
// }
//
// public class Route
// {
//     public Route(string path, string method, Func<HttpListenerContext, Task> handler)
//     {
//         Path = path;
//         Method = method;
//         Handler = handler;
//         PathGlob = Glob.Parse(Path);
//     }
//
//     public string Path { get; }
//     public string Method { get; }
//
//     public Glob PathGlob { get; }
//
//     public Func<HttpListenerContext, Task> Handler { get; }
//
//     public override bool Equals(object? obj)
//     {
//         if (obj is Route otherRoute)
//         {
//             return Path == otherRoute.Path && Method == otherRoute.Method;
//         }
//
//         return false;
//     }
//
//     public override int GetHashCode()
//     {
//         return HashCode.Combine(Path, Method);
//     }
//
//
//     public override string ToString()
//     {
//         return $"[path: {Path}, method: {Method}]";
//     }
// }
//
// public class RequestHandler(string key, Func<HttpListenerContext, Task> handler)
// {
//     public string Key { get; } = key;
//     public Func<HttpListenerContext, Task> Handler { get; } = handler;
// }