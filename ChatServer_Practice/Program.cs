using System;
using System.Threading;

namespace ChatServer_Practice
{
    class Program
    {
        static void Main()
        {
            var server = new Services.ChatServer(5000);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
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
