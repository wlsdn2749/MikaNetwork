using System.Net.Sockets;
using System.Text;

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

    public void Send(string message)
    {
        // string -> byte
        byte[] bytes = Encoding.UTF8.GetBytes(message + "\0");

        connectSocket.Send(bytes);
    }

    public void Receive()
    {
        byte[] recvBuffer = new byte[1024];

        int recvBytes = connectSocket.Receive(recvBuffer);
        var recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvBytes);
        
        Console.WriteLine($"{recvMessage}");
    }
}