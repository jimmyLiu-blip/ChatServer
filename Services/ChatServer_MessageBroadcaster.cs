using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer_Practice.Services
{
    public class ChatServer_MessageBroadcaster
    {
        private readonly ConcurrentDictionary<int, Socket> _clients;

        public ChatServer_MessageBroadcaster(ConcurrentDictionary<int, Socket> clients)
        {
            _clients = clients;
        }

        public async Task BroadcastAsync(string message, int fromId)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            foreach (var kv in _clients)
            {
                try
                {
                    if (kv.Value.Connected)
                        await kv.Value.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                }
                catch
                {
                    // 忽略個別錯誤
                }
            }
            Console.WriteLine(message);
        }
    }
}
