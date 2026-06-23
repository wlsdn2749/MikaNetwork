using System.Net;
using System.Net.Sockets;
using System.Text;
using Shouldly;

using MikaNetwork.Server;

namespace MikaServerCore.test;

public class MikaAcceptorTests
{
    /// <summary>Listen 중인 Acceptor에 TcpClient가 정상적으로 연결되는지 확인한다.</summary>
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

    /// <summary>여러 클라이언트가 동시에 연결될 때 Accepted 이벤트가 연결 수만큼 발화하는지 확인한다.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task Accept_Multiple_TcpClient(int count)
    {
        var acceptor = new MikaAcceptor(IPAddress.Loopback, 0);
        var acceptedSessions = new List<MikaServerSession>();
        acceptor.Accepted += session => { lock (acceptedSessions) acceptedSessions.Add(session); };
        acceptor.Listen();

        var connectors = new List<TcpClient>();

        int port = ((IPEndPoint)acceptor.EndPoint).Port;

        for (int i = 0; i < count; i++)
        {
            var connector = new TcpClient();
            connectors.Add(connector);

            await connector.ConnectAsync(IPAddress.Loopback, port);
        }

        (await TestHelpers.WaitUntilAsync(
            () => { lock (acceptedSessions) return acceptedSessions.Count == count; },
            TimeSpan.FromSeconds(2))).ShouldBeTrue();
    }
    
}