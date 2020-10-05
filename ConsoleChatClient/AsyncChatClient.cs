using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChatClient
{
    public class AsyncChatClient
    {
        public AsyncChatClient(bool setServerIpToLocalHost = false)
        {
            if (setServerIpToLocalHost)
                serverIp = Common.Utility.GetLocalAddress();
            else
                serverIp = IPAddress.Parse("83.221.156.57");
        }

        readonly IPAddress serverIp;

        const int serverPort = 0920;

        readonly ASCIIEncoding ASCII = new ASCIIEncoding();

        readonly Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public bool Running { get; private set; } = false;

        Task RecieveAsync()
        {
            while (Running)
            {
                if (client.Available < 1)
                    continue;
                byte[] buf = new byte[client.Available];
                client.Receive(buf);

                var str = ASCII.GetString(buf);
                Console.WriteLine(str);
            }
            return Task.CompletedTask;
        }

        public Task StartAsync()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(6000);
            var token = cancel.Token;
            token.Register(() => Console.WriteLine($"Could not establish connection to server at {serverIp}:{serverPort}..."));

            Console.WriteLine($"Attempting connection at {serverIp}:{serverPort}");

            bool refused = false;
            SocketException? exception = null;
            Task.Run(() =>
            {
                try { client.Connect(serverIp, serverPort); }
                catch (SocketException ex) { refused = true; exception = ex; return; }
            }, token).Wait();
            cancel.Dispose();
            
            if (refused)
            {
                Console.WriteLine("Server refused connection.");
                throw exception ?? new SocketException();
            }

            Console.WriteLine($"Successfully connected to {serverIp}:{serverPort}");
            Running = true;
            return RecieveAsync();
        }

        public Task StopAsync()
        {
            Running = false;
            client.Close();
            return Task.CompletedTask;
        }

        public Task SendAsync(string msg)
        {
            if (!client.Connected)
                throw new Exception("Client is not connected.");

            var strBytes = ASCII.GetBytes(msg);
            client.Send(strBytes);
            return Task.CompletedTask;
        }
    }
}
