using System.Net.Sockets;

namespace MikaServerCore;

public class MikaConnector
{
    private Socket connectSocket;
    
    public MikaConnector()
    {
        connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Connect(string ipAddress, int port)
    {
        connectSocket.Connect(ipAddress, port);
    }
}