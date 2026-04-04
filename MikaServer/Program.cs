using System.Net;
using MikaServerCore;

namespace MikaServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var listener = new MikaListener("127.0.0.1", 7777);
            listener.Start();
        }
    }
}
