using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace MikaServerCore.test;
using Shouldly;

public class MikaConnectorTests
{
    [Fact]
    public void Connect_To_TcpServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        using var connector = new MikaConnector();
        connector.Connect(IPAddress.Loopback, port);
        connector.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Hello World!")]
    [InlineData("123")]
    public async Task Send_Message_To_TcpServer(string message)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var acceptTask = listener.AcceptTcpClientAsync();

        using var connector = new MikaConnector();
        connector.Connect(IPAddress.Loopback, port);
        
        using var acceptClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(1));  
        connector.Send(message);

        var buffer = new byte[1024];
        var recvBytes = await acceptClient
            .GetStream()
            .ReadAsync(buffer)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(1));

        var recvMessage = Encoding.UTF8.GetString(buffer, 0, recvBytes);
        
        recvMessage.ShouldBe(message);
    }

    [Theory]
    [InlineData("Hello World!")]
    [InlineData("123")]
    public async Task Receive_Message_From_TcpServer(string message)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        var connector = new MikaConnector();
        connector.Connect(IPAddress.Loopback, port);
        
        var acceptTask = listener.AcceptTcpClientAsync();
        var acceptClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(1));
        connector.Send(message);
        
        var recvBuffer = new byte[1024];
        var recvBytes = await acceptClient
            .GetStream()
            .ReadAsync(recvBuffer)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(1));

        var recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvBytes)
            .Replace("\0", string.Empty);
        
        await using NetworkStream stream = acceptClient.GetStream();
        
        var sendBytes = Encoding.UTF8.GetBytes(recvMessage);
        await stream.WriteAsync(sendBytes);
        
        var recvString = connector.Receive();
        recvString.ShouldBe(message);
    }
}