using System.Net;
using MikaServerCore.Network;
using MikaServerCore.Package.Text;

namespace MikaServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build server with explicit endpoint, package handler, and selectable pipeline filter.
            // You can replace LinePipelineFilter with another TextPackageInfo filter implementation.
            
            var server = new Server(
                new IPEndPoint(IPAddress.Any, 8080),
                new EchoPackageHandler(),
                new LinePipelineFilter());

            server.Init();

            // Start server. It binds to the endpoint configured in constructor.
            await server.StartAsync();
        }
    }
}
