using System;
using System.Threading;

namespace ChatServer_Practice
{
    class Program
    {
        static void Main()
        {
            var server = new Services.ChatServer(5000);

            // 會幫你建立一個「中止信號的發射器」。
            // 把 cts.Token 傳給任務：讓任務能「聽到」中止信號。
            // 任務內定期檢查 ct.IsCancellationRequested，如果被設定為 true，就安全退出。
            // 執行 cts.Cancel(); 發送中止信號。所有任務都能知道該停止。
            var cts = new CancellationTokenSource();
            // 程式如何正確偵測 Ctrl+C 並觸發安全中止
            // 這行是註冊一個事件處理器 (event handler)，Console.CancelKeyPress 是一個事件
            // 當事件發生時，系統會自動呼叫一個方法，並傳進去兩個參數
            // sender：事件來源（這裡是 Console），e：包含事件資訊（例如是 Ctrl+C 還是 Ctrl+Break）
            // 當使用者在 Console 視窗按下 Ctrl + C 時，不要讓程式立刻中斷
            // 而是呼叫裡面的程式區塊（e.Cancel = true; cts.Cancel();），讓程式優雅地關閉。
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine($"偵測到按鍵：{e.SpecialKey}");
                e.Cancel = true; // 告訴系統「不要強制中斷程式」
                cts.Cancel();    // 發出中止信號給所有 Task
            };

            server.Start(cts.Token);

            Console.WriteLine("Type /quit to stop server.");
            while (!cts.IsCancellationRequested)
            {
                var cmd = Console.ReadLine();
                if (cmd == "/quit")
                {
                    cts.Cancel();
                    break;
                }
            }

            server.Stop();
            Console.WriteLine("[Server] Bye.");
        }
    }
}
