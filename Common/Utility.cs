using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common
{
    public static class Utility
    {
        public static void RemoveAllAndDo<T>(this List<T> list, Func<T, bool> predicate, Action<T> action)
        {
            foreach (T item in list)
            {
                if (!predicate(item)) continue;
                list.Remove(item);
                action(item);
            }
        }

        public static IPAddress GetLocalAddress()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Connect("8.8.8.8", 65530);
            var endpoint = (socket.LocalEndPoint as IPEndPoint) ?? throw new Exception("Could resolve local ip.");
            return endpoint.Address;
        }
    }
}
