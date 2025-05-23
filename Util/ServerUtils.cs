using System.Net;
using System.Net.Sockets;

namespace GodotServiceFramework.Util;

public static class ServerUtils
{
    public static int GetRandomAvailablePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();

        return port;
    }
}