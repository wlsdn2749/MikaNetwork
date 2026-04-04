using System.Net;
using System.Net.Sockets;
using System.Text;

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
            byte[] recvBuffer = new byte[1024];
            int recvBytes = acceptSocket.Receive(recvBuffer);
            string recvData = Encoding.UTF8.GetString(recvBuffer, 0, recvBytes);
            
            Console.WriteLine($"Received Data : {recvData}");

            
            // Echo
            byte[] sendBuffer = new byte[1024];
            sendBuffer = Encoding.UTF8.GetBytes(recvData + "\0"); 
            acceptSocket.Send(sendBuffer);
        }
    }
    
}