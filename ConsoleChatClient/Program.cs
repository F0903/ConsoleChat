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
        static readonly AsyncChatClient client = new AsyncChatClient();

        static void OnMessageReceived(string msg)
        {
            Console.WriteLine();
        }

        static void Main()
        {
            client.Start();
            client.ReceivedMessage += OnMessageReceived;

            StringBuilder sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                    break;

                if(key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length < 1)
                        continue;
                    Console.Write("\b \b");
                    sb.Remove(sb.Length - 1, 1);
                    continue;
                }

                Console.Write(key.KeyChar);
                sb.Append(key.KeyChar);

                if (key.Key != ConsoleKey.Enter)
                    continue;
                client.Send(sb.ToString());
                sb.Clear();
            }
        }
    }
}
