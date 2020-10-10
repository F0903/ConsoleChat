using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Common;

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

        readonly byte[] receiveBuffer = new byte[1024];

        public event Action<string>? ReceivedMessage;

        void Connect(IAsyncResult result)
        {
            client.EndConnect(result);
            Console.WriteLine("Successfully connected to server.");
            client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, 0, Receive, null);
        }

        void Receive(IAsyncResult result)
        {
            try
            {
                var count = client.EndReceive(result);
                client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, 0, Receive, null);

                var str = ASCII.GetString(receiveBuffer, 0, count);
                ReceivedMessage?.Invoke(str);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    Console.WriteLine("Connection was lost to the server.");
            }
        }

        public void Send(string message)
        {
            var buf = ASCII.GetBytes(message);
            client.BeginSend(buf, 0, buf.Length, 0, x => client.EndSend(x), null);
        }

        public void Start()
        {
            var end = new IPEndPoint(serverIp, serverPort);
            Console.WriteLine($"Attempting connection at {end}");
            client.BeginConnect(end, Connect, null);
        }
    }
}
