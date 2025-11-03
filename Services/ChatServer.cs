using ChatServer_Practice.Utility;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer_Practice.Services
{
    public class ChatServer
    {
        private readonly int _port;
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<int, Socket> _clients = new ConcurrentDictionary<int, Socket>();
        private int _idSeq = 0;
        private readonly ChatServer_MessageBroadcaster _broadcaster;

        public ChatServer(int port)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
            _broadcaster = new ChatServer_MessageBroadcaster(_clients);
        }

        public void Start(CancellationToken ct)
        {
            _listener.Start();
            Console.WriteLine($"[Server] Listening on port {_port}...");

            Task.Run(() => AcceptLoop(ct));
        }

        public void Stop()
        {
            _listener.Stop();
            foreach (var kv in _clients)
                Utility.ChatServer_SocketHelper.SafeClose(kv.Value);
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptSocketAsync();
                    int id = Interlocked.Increment(ref _idSeq);
                    _clients[id] = client;
                    Console.WriteLine($"[Server] Client#{id} connected from {client.RemoteEndPoint}");

                    _ = Task.Run(() =>
                    {
                        var handler = new ChatServer_ClientHandler(id, client, _clients, _broadcaster);
                        _ = handler.HandleAsync(ct);
                    });
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Accept error: {ex.Message}");
                }
            }
        }
    }
}
