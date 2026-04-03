using System.Net;
using System.Net.Sockets;

namespace MikaServerCore;

public class MikaListener
{
    private int backlog = 10000;
    private Socket listenSocket;

    public MikaListener(string ipAddress, int port) : this(IPAddress.Parse(ipAddress), port)
    {
        
    }
    public MikaListener(IPAddress ipAddress, int port)
    {
        listenSocket  = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        IPEndPoint ep = new IPEndPoint(ipAddress, port);
        listenSocket.Bind(ep);
    }
    
    public void Start()
    {
        listenSocket.Listen(backlog);
        
        while (true)
        {
            Socket acceptSocket = listenSocket.Accept();
            Console.WriteLine($"{acceptSocket.AddressFamily} + {acceptSocket.SocketType} + {acceptSocket.ProtocolType}");
        }
    }
    
}