using System;
using System.Threading;
using ChatClient_Practice.Services;


namespace ChatClient_Practice
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = args.Length > 0 ? args[0] : "127.0.0.1";
            int port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 5000;

            ChatClient client = new ChatClient(server, port);
            client.Connect();

            Console.WriteLine("[Client] Connected. Type message and press ENTER. Type /quit to exit.");
            Console.WriteLine("  (Tip: /name 你的名字 可以改暱稱)");

            CancellationTokenSource cts = new CancellationTokenSource();

            // 啟動背景接收執行緒
            client.StartReceiving(cts.Token);

            // 前景處理使用者輸入
            while (!cts.IsCancellationRequested)
            {
                string input = Console.ReadLine();
                if (input == null) continue;

                if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                {
                    cts.Cancel();
                    break;
                }

                client.SendMessage(input);
            }

            client.Close();
            Console.WriteLine("[Client] Bye.");
        }
    }
}
