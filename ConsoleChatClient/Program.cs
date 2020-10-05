using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChatClient
{
    class Program
    {
        static AsyncChatClient? client;

        const bool forceLocal = false; // SET TO FALSE FOR WAN USE

        static void Throw(Exception inner) => throw inner;

        static async Task Main(string[] args)
        {
            client = new AsyncChatClient(forceLocal || args.Contains("-localhost"));

            var clientThread = new Thread(() =>
            {
                try { client.StartAsync(); }
                catch (SocketException ex) { Throw(ex); }
            });
            clientThread.Start();

            StringBuilder sb = new StringBuilder();

            while (clientThread.IsAlive)
            {
                var key = Console.ReadKey(true);               
                if (key.Key == ConsoleKey.Escape)
                    break;

                if(key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length < 1) continue;
                    sb.Remove(sb.Length - 1, 1);
                    Console.Write("\b \b");
                    continue;
                }

                sb.Append(key.KeyChar);
                Console.Write(key.KeyChar);

                if (key.Key != ConsoleKey.Enter)
                    continue;

                await client.SendAsync(sb.ToString());
                Console.Write('\n');
                sb.Clear();
            }
            await client.StopAsync();
        }
    }
}
