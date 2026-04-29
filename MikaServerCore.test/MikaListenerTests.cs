using System.Net;
using System.Net.Sockets;
using System.Text;
using Shouldly;

namespace MikaServerCore.test;

public class MikaListenerTests
{
    [Fact]
    public void Start_To_Accept_TcpClient()
    {
        var listener = new MikaListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.EndPoint).Port;

        var connector = new TcpClient();
        connector.Connect(IPAddress.Loopback, port);

        connector.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Hello world!")]
    [InlineData("1234!")]
    public async Task Receive_Message_From_TcpClient(string message)
    {
        var listener = new MikaListener(IPAddress.Loopback, 0);
        listener.Start();
        
        int port = ((IPEndPoint)listener.EndPoint).Port;
        var connector = new TcpClient();
        connector.ConnectAsync(IPAddress.Loopback, port);

        var acceptTask = listener.AcceptAsync();
        var acceptClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(1));
        
        var sendBytes = Encoding.UTF8.GetBytes(message);
        await connector
            .GetStream()
            .WriteAsync(sendBytes, 0, sendBytes.Length);
        
        var recvBuffer = new byte[1024];
        var recvBytes = await acceptClient
            .ReceiveAsync(recvBuffer)
            .WaitAsync(TimeSpan.FromSeconds(1));
        
        var recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvBytes);
        recvMessage.ShouldBe(message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task Accept_Multiple_TcpClient(int count)
    {
        var listener = new MikaListener(IPAddress.Loopback, 0);
        listener.Start();

        var connectors = new List<TcpClient>();
        var acceptClients = new List<Socket>();
        
        int port = ((IPEndPoint)listener.EndPoint).Port;
        
        for (int i = 0; i < count; i++)
        {
            var connector = new TcpClient();
            connectors.Add(connector);
            
            await connector.ConnectAsync(IPAddress.Loopback, port);

            var acceptTask = listener
                .AcceptAsync()
                .WaitAsync(TimeSpan.FromSeconds(1));
            
            var acceptClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(1));
            acceptClients.Add(acceptClient);
        }
        
        acceptClients.Count.ShouldBe(count);
        acceptClients.ShouldAllBe(socket => socket.Connected);
    }
    
}