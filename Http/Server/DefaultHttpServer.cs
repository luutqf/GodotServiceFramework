// using System.Diagnostics;
// using System.Net;
// using System.Runtime.InteropServices;
// using DotNet.Globbing;
// using Godot;
// using GodotServiceFramework.Util;
//
// namespace GodotServiceFramework.Http.Server;
//
// // [AutoGlobalService]
// public class DefaultHttpServer
// {
//     private readonly HttpListener _listener;
//
//     private bool _running;
//
//     public event Action<HttpListenerContext> RequestHandler;
//
//     private readonly Dictionary<Route, List<RequestHandler>> _routes = [];
//
//
//     public DefaultHttpServer(int port, Action errorCallback)
//     {
//         _listener = new HttpListener();
//         RequestHandler += HandleRequest;
//         _running = true;
//
//         Task.Run(() =>
//         {
//             try
//             {
//                 string? uriPrefix = null;
//                 try
//                 {
//                     uriPrefix = $"http://+:{port}/";
//                     _listener.Prefixes.Add(uriPrefix);
//                     // _listener.Prefixes.Add("https://10.0.11.184:8443/");
//                 }
//                 catch (Exception e)
//                 {
//                     Log.Info($"not support ipv4 {e.Message}");
//
//                     // throw;
//                 }
//
//
//                 if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//                 {
//                     try
//                     {
//                         if (uriPrefix != null) AddUrlAcl(uriPrefix, "Everyone");
//                         // if (uriPrefixV6 != null) AddUrlAcl(uriPrefixV6, "Everyone");
//                         OpenFirewallPort(port);
//                     }
//                     catch (Exception e)
//                     {
//                         Log.Error(e.Message);
//                     }
//                 }
//
//                 _listener.Start();
//
//                 while (_running)
//                 {
//                     // 等待并获取客户端请求
//                     var context = _listener.GetContext(); // 同步方法，也可以使用异步方法
//                     Task.Run(() => RequestHandler.Invoke(context)); // 使用 Task.Run 异步处理每个请求
//                 }
//             }
//             catch (HttpListenerException e)
//             {
//                 Log.Error(e);
//                 errorCallback.Invoke();
//             }
//             finally
//             {
//                 _listener.Close();
//             }
//         });
//     }
//
//
//     public void RegisterRoute(Route route, RequestHandler handler, bool replace = false)
//     {
//         if (!_routes.TryGetValue(route, out var list))
//         {
//             list = [];
//             _routes.Add(route, list);
//         }
//
//         if (replace)
//         {
//             list.Clear();
//         }
//         else if (list.Any(value => value.Key == handler.Key))
//         {
//             return;
//         }
//
//
//         list.Add(handler);
//     }
//
//
//     public bool UseHandler(Route route, string key)
//     {
//         if (!_routes.TryGetValue(route, out var list))
//         {
//             return false;
//         }
//
//         var requestHandler = list.FirstOrDefault((handler) => handler.Key == key);
//         if (requestHandler == null) return false;
//         list.Remove(requestHandler);
//         list.Insert(0, requestHandler);
//         return true;
//     }
//
//     public void RemoveRoute(Route route, string key)
//     {
//         try
//         {
//             var handler = _routes[route].First(requestHandler => requestHandler.Key == key);
//
//             _routes[route].Remove(handler);
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine(e);
//         }
//     }
//
//     private async void HandleRequest(HttpListenerContext context)
//     {
//         // var port = context.Request.LocalEndPoint.Port;
//         if (context.Request.Url == null) return;
//
//         var requestPath = context.Request.Url.AbsolutePath;
//         var requestMethod = context.Request.HttpMethod;
//
//
//         //TODO 这里要对Route排序, 更精确的更先触发
//         // 查找匹配的路由
//         foreach (var route in _routes.Keys.Where(route =>
//                      MatchRoutePath(route.PathGlob, requestPath) &&
//                      MatchRouteMethod(route.Method, requestMethod)))
//         {
//             await _routes[route].First().Handler.Invoke(context);
//             // await route.Handler(context);
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
//     public void Destroy()
//     {
//         _running = false;
//         _listener?.Close();
//         RequestHandler -= HandleRequest;
//     }
//
//
//     public static void AddUrlAcl(string url, string user)
//     {
//         try
//         {
//             var process = new Process();
//             process.StartInfo.FileName = "netsh";
//             process.StartInfo.Arguments = $"http add urlacl url={url} user={user}";
//             process.StartInfo.UseShellExecute = false;
//             process.StartInfo.RedirectStandardOutput = true;
//             process.Start();
//
//             string output = process.StandardOutput.ReadToEnd();
//             process.WaitForExit();
//
//             if (process.ExitCode == 0)
//             {
//                 Console.WriteLine("URL ACL added successfully.");
//             }
//             else
//             {
//                 Console.WriteLine($"Error adding URL ACL: {output}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Exception: {ex.Message}");
//         }
//     }
//
//     public static void OpenFirewallPort(int port)
//     {
//         var command =
//             $"advfirewall firewall add rule name=\"GodotHttpListenerTCP{port}\" protocol=TCP dir=in localport={port} action=allow";
//         var psi = new ProcessStartInfo("netsh", command)
//         {
//             Verb = "runas", // 以管理员权限运行
//             WindowStyle = ProcessWindowStyle.Hidden,
//             CreateNoWindow = true
//         };
//
//         try
//         {
//             var process = new Process();
//             process.StartInfo = psi;
//             process.Start();
//             process.WaitForExit();
//         }
//         catch (Exception e)
//         {
//             GD.Print(e);
//         }
//     }
// }