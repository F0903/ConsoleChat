using System;
using System.Threading.Tasks;

namespace ConsoleChatServer
{
    class Program
    {
        readonly static AsyncChatServer server = new AsyncChatServer();

        static async Task Main()
        {
            server.Start();
            await Task.Delay(-1);
        }
    }
}
