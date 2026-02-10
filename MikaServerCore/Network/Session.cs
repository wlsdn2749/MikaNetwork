using System.Net.Sockets;

namespace ServerCore;

public class Session
{
    private Socket  _socket;
    private int     _sessionId;

    public async Task StartAsync()
    {
        // WhileLoop를 돌면서, ReceiveAsync 호출
    }

    public async Task SendAsync()
    {
        // Header를 붙여서 SendAsync 호출
    }
    
    public virtual void OnReceive(byte[] buffer) {}
    public virtual void OnDisconnected() {}
    
    public void Disconnect(){}
}