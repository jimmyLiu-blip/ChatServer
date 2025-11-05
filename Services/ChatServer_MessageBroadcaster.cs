using System;
// 提供 執行緒安全集合
using System.Collections.Concurrent;
// 提供 Socket 通訊的所有類別（TCP/UDP）
using System.Net.Sockets;
using System.Text;
// 支援非同步程式設計 (Task, async/await)
using System.Threading.Tasks;

namespace ChatServer_Practice.Services
{
    public class ChatServer_MessageBroadcaster
    {
        // ConcurrentDictionary 多執行緒安全版的字典（不用額外加 lock）
        // _clients 用來存放目前所有已連線的 Client 清單
        private readonly ConcurrentDictionary<int, Socket> _clients;

        public ChatServer_MessageBroadcaster(ConcurrentDictionary<int, Socket> clients)
        {
            _clients = clients;
        }

        // async + Task 是用來建立非同步的方法
        // void 方法執行完就結束，無法等待或追蹤它什麼時候做完
        // Task 則代表「一個進行中的工作（任務）」，可以 await 它，也可以讓外部知道「它還沒做完」。
        public async Task BroadcastAsync(string message, int fromId)
        {
            // Encoding.UTF8 的意思是「使用 UTF-8 編碼」將文字轉成位元組（Byte）
            // 為什麼要編碼，因為網路傳輸只能傳 位元組（byte），不能直接傳 C# 字串。
            // GetBytes 把「字串」轉成「位元組陣列（byte[]）
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            // kv 是指 _clients中的 KeyValues
            foreach (var kv in _clients)
            {
                try
                {
                    // 檢查：「這個 Socket（也就是某個 Client）目前是否還連線著
                    // kv.Value 是什麼？    這是目前迴圈中的某一個 Socket（連線中的 Client）。
                    // SendAsync(...) 是什麼？  這是 Socket 類別提供的非同步傳送方法。
                    // new ArraySegment<byte>(data)，這裡的 data 是我們上面準備好的 byte[]
                    // ArraySegment<byte> 表示「整個 data 陣列的一段範圍」。在這裡等於「傳送整個陣列」。
                    // 為什麼不用直接傳 data？因為底層 API 要求的是「一段 byte 陣列的範圍」（可能只傳一部分），所以這樣包裝起來比較靈活。
                    // SocketFlags.None：指定傳送的額外選項。None 代表「正常傳送」，不加任何特殊行為。
                    // await 會：等待這個「傳送工作」完成後，再繼續往下執行程式。但這個等待不會卡住整個執行緒。它會讓出控制權，讓系統可以同時做其他事（例如處理別的 Client）。
                    if (kv.Value.Connected)
                        await kv.Value.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"主伺服器傳送訊息到Client發生錯誤，{ex.Message}");
                    Console.ResetColor();
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
