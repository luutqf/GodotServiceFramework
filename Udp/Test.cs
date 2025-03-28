using GodotServiceFramework.Proto;

namespace GodotServiceFramework.Udp;

public class Test
{
    public static void Main()
    {
        var myUdpClient = new MyUdpClient();
        myUdpClient.AddEndpoint("10.0.10.133", 7879);
        myUdpClient.SetMain("10.0.10.133", 7879);
        myUdpClient.ReceiveProtoMessage((message) => { Console.WriteLine(message.RespType); } );
        //
        //
        //     
        myUdpClient.SendProtoMessage(new PlayerAction()
        {
            ActionType = "SophonInfo"
        });

        Thread.Sleep(20000);
    }
}