using ChatServer_Practice.Utility;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer_Practice.Services
{ 
    // 負責處理單一個 Client 的所有收發行為
    public class ChatServer_ClientHandler
    {
        private readonly int _id;                                       // 用戶在伺服器中的編號（例如 Client#1）
        private readonly Socket _socket;                                // 此 Client 的網路連線通道（收/發資料都靠它）
        private readonly ConcurrentDictionary<int, Socket> _clients;    // 所有線上使用者清單（共用）
        private readonly ChatServer_MessageBroadcaster _broadcaster;    // 廣播器（用來傳訊息給大家）
        private readonly StringBuilder _buffer = new StringBuilder();   // 暫存目前接收但還沒讀完的資料（因為訊息可能分段傳進來）

        public ChatServer_ClientHandler(int id, Socket socket, ConcurrentDictionary<int, Socket> clients, ChatServer_MessageBroadcaster broadcaster)
        {
            _id = id;
            _socket = socket;
            _clients = clients;
            _broadcaster = broadcaster;
        }

        // 在背景持續監聽某個 Client 是否傳送訊息進來，傳入一個「中止請求」的控制開關。
        public async Task HandleAsync(CancellationToken ct)
        {
            await _broadcaster.BroadcastAsync($"[System] Client#{_id} joined.", _id);

            // 不斷地從這個 Client 的 Socket 通道中接收資料，把收到的文字存到 _buffer 暫存區裡，
            // 一旦收到完整訊息（例如一行以 \n 結尾），就交給 ProcessBuffer() 去處理。」
            // 建立一個可以暫存「接收資料」的容器；byte[]：位元組陣列（網路傳輸的最小單位就是 byte，不是文字）
            // new byte[4096]：建立一個「4096 bytes」大小的緩衝區（約 4KB）。
            byte[] buf = new byte[4096];
            try
            {
                // !ct.IsCancellationRequested：伺服器尚未要求停止（沒下打烊指令）
                // _socket.Connected：這位客人仍然在線上
                while (!ct.IsCancellationRequested && _socket.Connected)
                {
                    int n = await _socket.ReceiveAsync(new ArraySegment<byte>(buf), SocketFlags.None);
                    // 當 n == 0 代表：客戶端斷線、網路通道被關閉，伺服器主動中止連線。
                    if (n <= 0) break;

                    // 把剛才收到的 byte[0..n] 區段，轉成 UTF-8 字串。
                    // 0 是指從第 0 個 Index 開始轉換，n 是指要轉換幾個 byte（長度）
                    _buffer.Append(Encoding.UTF8.GetString(buf, 0, n));
                    ProcessBuffer();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Server] Client#{_id} error: {ex.Message}");
                Console.ResetColor();
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
                int index = text.IndexOf('\n');
                if (index < 0) break;

                string line = text.Substring(0, index).TrimEnd('\r');
                _buffer.Remove(0, index + 1);

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
