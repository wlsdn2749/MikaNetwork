using System.Net;
using MikaNetwork.Client;
using MikaNetwork.Server;
using Shouldly;

namespace MikaServerCore.test;

public class MikaSessionTests
{
    /// <summary>여러 클라이언트가 연결되면 모두 IsConnected이고 서버의 SessionManager에 연결 수만큼 등록되는지 확인한다.</summary>
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
        await TestHelpers.WaitUntilAsync(() => server.SessionManager.Count == count, TimeSpan.FromSeconds(2));
        
        connectors.ShouldAllBe(connector => connector.IsConnected);
        connectors.Count.ShouldBe(count);
        server.SessionManager.Count.ShouldBe(count);
        
    }

    /// <summary>연결된 클라이언트들을 모두 끊으면 서버의 SessionManager에서 세션이 전부 제거되는지 확인한다.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(100)]
    public async Task DisconnectSessions(int count)
    {
        using var server = new MikaServer(IPAddress.Loopback, 0);
        server.Listen();

        var serverPort = ((IPEndPoint)server.EndPoint).Port;
        
        var clients = new List<MikaClient>();
        for(int i=0; i<count; i++)
        {
            var client = new MikaClient();
            await client.ConnectAsync(IPAddress.Loopback, serverPort);
            clients.Add(client);
        }
        
        SpinWait.SpinUntil(() => server.SessionManager.Count == count, TimeSpan.FromSeconds(1));
        foreach (var client in clients)
        {
            client.Disconnect();
        }
        
        SpinWait.SpinUntil(() => server.SessionManager.Count == 0, TimeSpan.FromSeconds(2));
        server.SessionManager.Count.ShouldBe(0);
    }
}