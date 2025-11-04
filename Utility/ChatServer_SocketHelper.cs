using System;
// 載入「網路通訊 (Sockets)」的命名空間
// 這個命名空間提供了所有與網路連線、TCP/UDP 傳輸有關的類別，例如：
// Socket：最底層的網路通訊物件，可以建立 TCP/UDP 連線
// TcpListener：用來建立伺服器端（Server），監聽連線請求
// TcpClient：用來建立用戶端（Client）並連線伺服器
// NetworkStream：封裝在 Socket 上的資料流，用於讀取/寫入資料
// SocketException：用來捕捉 Socket 操作發生錯誤時的例外
using System.Net.Sockets;

namespace ChatServer_Practice.Utility
{
    // 不需要建立物件，就能直接呼叫裡面的工具方法
    public static class ChatServer_SocketHelper
    {
        // Socket 安全開關輔助工具類別
        // Socket 是資料型別，s 才是參數名稱
        public static void SafeClose(Socket s)
        {
            // 通知系統「這個連線不再傳輸資料」
            // SocketShutdown.Both 表示同時停止 發送 與 接收；SocketShutdown.Send、SocketShutdown.Receive
            try 
            { 
                s.Shutdown(SocketShutdown.Both); 
            } 
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Shutdown 發生錯誤: {ex.Message}");
                Console.ResetColor();
            }
            // 關閉 Socket 連線，釋放底層資源
            // 若沒有先 Shutdown()，可能會導致部分未送出的資料被強制中斷
            try 
            { 
                s.Close(); 
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Close 發生錯誤: {ex.Message}");
                Console.ResetColor();
            }
            // 釋放 Socket 物件佔用的非受控資源（例如作業系統的連線控制區）
            // 即使 Close() 通常會自動呼叫 Dispose()，仍加上這行確保萬無一失。
            try 
            {
                s.Dispose();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Dispose 發生錯誤: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
