using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace MikaServerCore.test;
using Shouldly;

public class MikaConnectorTests
{
    /// <summary>Connector가 TcpListener로 떠 있는 서버에 동기 방식으로 연결되는지 확인한다.</summary>
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

    /// <summary>Connector가 보낸 메시지가 서버 측에서 동일한 바이트로 수신되는지 확인한다.</summary>
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

    /// <summary>서버가 보낸 메시지를 Connector가 동일한 문자열로 수신하는지 확인한다.</summary>
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