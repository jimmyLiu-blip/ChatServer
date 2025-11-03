using ChatServer_Practice.Utility;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer_Practice.Services
{
    public class ChatServer_ClientHandler
    {
        private readonly int _id;
        private readonly Socket _socket;
        private readonly ConcurrentDictionary<int, Socket> _clients;
        private readonly ChatServer_MessageBroadcaster _broadcaster;
        private readonly StringBuilder _buffer = new StringBuilder();

        public ChatServer_ClientHandler(int id, Socket socket, ConcurrentDictionary<int, Socket> clients, ChatServer_MessageBroadcaster broadcaster)
        {
            _id = id;
            _socket = socket;
            _clients = clients;
            _broadcaster = broadcaster;
        }

        public async Task HandleAsync(CancellationToken ct)
        {
            await _broadcaster.BroadcastAsync($"[System] Client#{_id} joined.", _id);

            byte[] buf = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && _socket.Connected)
                {
                    int n = await _socket.ReceiveAsync(new ArraySegment<byte>(buf), SocketFlags.None);
                    if (n <= 0) break;

                    _buffer.Append(Encoding.UTF8.GetString(buf, 0, n));
                    ProcessBuffer();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Client#{_id} error: {ex.Message}");
            }
            finally
            {
                Socket tmp;
                if (_clients.TryRemove(_id, out tmp))
                {
                    Utility.ChatServer_SocketHelper.SafeClose(_socket);
                    await _broadcaster.BroadcastAsync($"[System] Client#{_id} left.", _id);
                    Console.WriteLine($"[Server] Client#{_id} disconnected.");
                }
            }
        }

        private async void ProcessBuffer()
        {
            while (true)
            {
                var text = _buffer.ToString();
                int idx = text.IndexOf('\n');
                if (idx < 0) break;

                string line = text.Substring(0, idx).TrimEnd('\r');
                _buffer.Remove(0, idx + 1);

                if (line.StartsWith("/name "))
                {
                    string name = line.Substring(6).Trim();
                    await _broadcaster.BroadcastAsync($"[System] Client#{_id} is now '{name}'.", _id);
                }
                else
                {
                    await _broadcaster.BroadcastAsync($"Client#{_id}: {line}", _id);
                }
            }
        }
    }
}
