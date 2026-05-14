using System.Net;
using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaSessionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task ConnectMultipleSession(int count)
    {
        using var server = new MikaServer(IPAddress.Loopback, 0);
        server.Listen();

        var serverPort = ((IPEndPoint)server.EndPoint).Port;
        
        var connectors = new List<MikaConnector>();
        for(int i=0; i<count; i++)
        {
            var connector = new MikaConnector();
            await connector.ConnectAsync(IPAddress.Loopback, serverPort);
            connectors.Add(connector);
            
        }
        
        // 서버가 백그라운드 동안 Accept -> SessionAdd 할 시간을 확보
        SpinWait.SpinUntil(() => connectors.Count == count, TimeSpan.FromSeconds(1));
        
        connectors.ShouldAllBe(connector => connector.IsConnected);
        connectors.Count.ShouldBe(count);
        server.SessionManager.Count.ShouldBe(count);
    }
}