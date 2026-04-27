using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MikaServerCore;

public class MikaConnector : IDisposable
{
    private readonly Socket _connectSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public void Connect(string ipAddress, int port) => _connectSocket.Connect(ipAddress, port);
    public void Connect(IPAddress ipAddress, int port) => _connectSocket.Connect(ipAddress, port);

    public void Send(string message)
    {
        // string -> byte
        byte[] bytes = Encoding.UTF8.GetBytes(message);

        _connectSocket.Send(bytes);
    }

    public string Receive()
    {
        byte[] recvBuffer = new byte[1024];

        int recvBytes = _connectSocket.Receive(recvBuffer);
        var recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvBytes);

        return recvMessage;
    }

    public void Dispose()
    {
        _connectSocket.Dispose();
    }
}