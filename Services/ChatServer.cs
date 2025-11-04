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

        // TcpListener = TCP「監聽器」：負責在某個 IP + 連接埠上「開門等連線」。
        // IPAddress.Any
        // 代表「這台機器上的所有網卡都接受」
        // 等於：不管是 127.0.0.1（本機）或區域網卡 192.168.x.x / 10.x.x.x… 只要封包是打到「這台電腦」的_port，都收得到。
        // 如果你只想收本機回圈連線，會用 IPAddress.Loopback（127.0.0.1）；
        // 只想收某張網卡，也可以填那張網卡的 IP（例如 IPAddress.Parse("192.168.1.20")）。

        // ChatServer_MessageBroadcaster = 「廣播器」：把一則訊息送給所有線上 client。
        // _clients 是 ConcurrentDictionary<int, Socket>：伺服器目前所有連線清單（key = 客戶 ID，value = 該客戶的 Socket）。

        // 為什麼用 ConcurrentDictionary？
        // 因為多執行緒：每個 ClientHandler 都可能同時加入/移除連線、同時呼叫廣播。
        // ConcurrentDictionary 提供執行緒安全的 新增/刪除/讀取，減少鎖的麻煩。

        // 原本是使用 Dictionary<string, string> phoneBook = new Dictionary<string, string>(); => 一本筆記本，一次只能一個人寫
        // phoneBook["Jimmy"] = "0912-345-678"

        // Concurrent = 同時發生（多執行緒同時操作）。 => 有管理員幫你排隊寫入，確保資料不會打架
        // 如果用一般的 Dictionary，會出現「兩個人同時寫筆記本」的錯誤（衝突）。

        // Socket = 「插座」、「插孔」，在電腦世界中，它就是「網路通訊的插孔」。
        // 可以把它想成：Client 插上 Server 的網路插孔（Socket），兩邊透過這個插孔就能互相傳訊息。
        // Socket 其實是「網路連線」的實體代表。每次一個 Client 連進來，Server 都會拿到一個新的 Socket。

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
