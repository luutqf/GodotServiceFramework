using System.Diagnostics;

namespace GodotServiceFramework.Util;

public class WindowsUtils
{
    public static void OpenFirewallPort(int port)
    {
        var command =
            $"advfirewall firewall add rule name=\"HttpListener{port}TCP\" protocol=TCP dir=in localport={port} action=allow";
        var psi = new ProcessStartInfo("netsh", command)
        {
            Verb = "runas", // 以管理员权限运行
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };

        try
        {
            var process = new Process();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}