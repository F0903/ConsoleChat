using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Common;

namespace ConsoleChatServer
{
    public class AsyncChatServer
    {
        public const int Port = 0920;

        public static readonly IPAddress IP = Common.Utility.GetLocalAddress();

        readonly ASCIIEncoding ASCII = new ASCIIEncoding();

        readonly Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        readonly List<Socket> clients = new List<Socket>();
        readonly object clientLock = new object();

        public bool Listening { get; private set; } = false;

        Task MessageRecieved(Socket sender, string msg)
        {
            var senderName = sender.RemoteEndPoint.ToString();
            Console.WriteLine($"[MESSAGE | {senderName}] {msg}");
            foreach (var client in clients)
            {
                if (client.RemoteEndPoint == sender.RemoteEndPoint) continue;
                var byteStr = ASCII.GetBytes($"[{senderName}] {msg}");
                try { client.Send(byteStr, byteStr.Length, SocketFlags.None); }
                catch { }
            }
            return Task.CompletedTask;
        }

        Task ManageClients()
        {
            lock (clientLock)
            {
                clients.RemoveAllAndDo(x => !x.Connected, x => Console.WriteLine($"[DISCONNECTED | {x.RemoteEndPoint}]"));
                foreach (var client in clients)
                {
                    if (client.Available < 1)
                        continue;

                    StringBuilder sb = new StringBuilder();
                    byte[] buf = new byte[client.Available];
                    client.Receive(buf);
                    sb.Append(ASCII.GetString(buf));

                    MessageRecieved(client, sb.ToString());
                }
            }
            return Task.CompletedTask;
        }

        Task Accept()
        {
            while (Listening)
            {
                var client = server.Accept();
                lock (clientLock)
                    clients.Add(client);
                Console.WriteLine($"[CONNECTED | {client.RemoteEndPoint}]");
            }
            return Task.CompletedTask;
        }

        Task Listen()
        {
            server.Listen(10);
            Task.Run(Accept);
            while (Listening)
            {
                ManageClients();
            }
            return Task.CompletedTask;
        }

        public Task StartAsync()
        {
            if (Listening)
                return Task.CompletedTask;

            Listening = true;
            Console.WriteLine($"Started listening on {IP}:{Port}");
            server.Bind(new IPEndPoint(IP, Port));

            Task.Run(Listen);
            Task.Run(ManageClients);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Listening = false;
            foreach (var client in clients)
            {
                var byteStr = ASCII.GetBytes("[SERVER] Server closing");
                client.Send(byteStr);
                client.Close();
            }
            server.Shutdown(SocketShutdown.Both);
            server.Close();
            return Task.CompletedTask;
        }
    }
}
