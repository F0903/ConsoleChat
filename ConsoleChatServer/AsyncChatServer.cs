using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Common;

namespace ConsoleChatServer
{
    public class ClientState
    {
        public byte[] Buffer { get; } = new byte[1024];
        public Socket? Socket { get; set; }
    }

    public class AsyncChatServer
    {
        public const int Port = 0920;

        public static readonly IPAddress ServerIp = Common.Utility.GetLocalAddress();

        readonly ASCIIEncoding ASCII = new ASCIIEncoding();

        readonly Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        readonly Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        void DisconnectClient(ClientState client)
        {
            using var sock = client.Socket;
            if (sock != null && sock.Connected)
                sock.BeginDisconnect(false, x => sock.EndDisconnect(x), null);
            clients.Remove(sock ?? clients.Single(x => x.Value == client).Key);
            Console.WriteLine($"[DISCONNECTED] {sock?.RemoteEndPoint}");
        }

        void SendToAllClients(byte[] data, int count)
        {
            foreach (var client in clients.Values)
            {
                var sock = client.Socket;
                if (sock == null || (sock != null && !sock.Connected))
                {
                    DisconnectClient(client);
                    continue;
                }              
                sock!.BeginSend(data, 0, count, 0, x => sock.EndSend(x), null);
            }
        }

        void Accept(IAsyncResult result)
        {
            try
            {
                var sock = server.EndAccept(result);
                Console.WriteLine($"[CONNECTED] {sock.RemoteEndPoint}");
                server.BeginAccept(Accept, null);
                var client = new ClientState() { Socket = sock };
                clients.Add(sock, client);
                sock.BeginReceive(client.Buffer, 0, client.Buffer.Length, 0, Receive, client);
            }
            catch (SocketException) { }
        }

        void Receive(IAsyncResult result)
        {
            try
            {
                var state = result.AsyncState as ClientState ?? throw new Exception("ClientState passed to Receive was null.");
                var sock = state.Socket!;

                var receiveCount = sock.EndReceive(result);
                sock.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, Receive, state);

                if (receiveCount < 1)
                {
                    DisconnectClient(state);
                }

                Console.WriteLine($"Received {receiveCount} bytes from {state.Socket?.RemoteEndPoint}");

                var ipStrBytes = ASCII.GetBytes($"[{sock.RemoteEndPoint}] ");
                var dataCount = receiveCount + ipStrBytes.Length;
                byte[] data = new byte[dataCount];
                Buffer.BlockCopy(ipStrBytes, 0, data, 0, ipStrBytes.Length);
                Buffer.BlockCopy(state.Buffer, 0, data, ipStrBytes.Length, receiveCount);

                SendToAllClients(data, dataCount);
            }
            catch(SocketException) { }
        }

        public void Start()
        {
            var localEnd = new IPEndPoint(ServerIp, Port);
            server.Bind(localEnd);
            server.Listen(10);
            Console.WriteLine($"Started listening on {localEnd}");
            server.BeginAccept(Accept, null);
        }

        public void Stop()
        {
            server.Close();
            clients.Clear();
        }
    }
}
