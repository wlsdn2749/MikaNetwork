using System.Net;
using System.Net.Sockets;
using System.Text;
using Shouldly;

using MikaServerCore.Network;

namespace MikaServerCore.test;

public class MikaAcceptorTests
{
    [Fact]
    public async Task Start_To_Accept_TcpClient()
    {
        var acceptor = new MikaAcceptor(IPAddress.Loopback, 0);
        acceptor.Listen();

        int port = ((IPEndPoint)acceptor.EndPoint).Port;

        var connector = new TcpClient();
        await connector.ConnectAsync(IPAddress.Loopback, port);

        connector.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task Accept_Multiple_TcpClient(int count)
    {
        var acceptor = new MikaAcceptor(IPAddress.Loopback, 0);
        acceptor.Listen();

        var connectors = new List<TcpClient>();
        var acceptClients = new List<Socket>();
        
        int port = ((IPEndPoint)acceptor.EndPoint).Port;
        
        for (int i = 0; i < count; i++)
        {
            var connector = new TcpClient();
            connectors.Add(connector);
            
            await connector.ConnectAsync(IPAddress.Loopback, port);
        }
        
        acceptClients.ShouldAllBe(socket => socket.Connected);
    }
    
}