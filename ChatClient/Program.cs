using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient_Practice
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = args.Length > 0 ? args[0] : "127.0.0.1";
            int port = 5000;
            if (args.Length > 1)
            {
                int.TryParse(args[1], out port);
            }

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine("[Client] Connecting to " + server + ":" + port + " ...");
                socket.Connect(IPAddress.Parse(server), port);
                Console.WriteLine("[Client] Connected. Type message and press ENTER. Type /quit to exit.");
                Console.WriteLine("  (Tip: /name 你的名字 可以改暱稱)");

                CancellationTokenSource cts = new CancellationTokenSource();

                // 背景接收
                Task recvTask = Task.Run(() => ReceiveLoop(socket, cts.Token));

                // 主線程處理使用者輸入
                while (!cts.IsCancellationRequested)
                {
                    string line = Console.ReadLine();
                    if (line == null) continue;

                    if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    {
                        cts.Cancel();
                        break;
                    }

                    byte[] data = Encoding.UTF8.GetBytes(line + "\n");
                    socket.Send(data);
                }

                // 收尾等待
                try { recvTask.Wait(500); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Client] Error: " + ex.Message);
            }
            finally
            {
                SafeClose(socket);
                Console.WriteLine("[Client] Bye.");
            }
        }

        static void ReceiveLoop(Socket socket, CancellationToken ct)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && socket.Connected)
                {
                    int n = socket.Receive(buffer);
                    if (n <= 0) break;

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, n));

                    while (true)
                    {
                        string text = sb.ToString();
                        int idx = text.IndexOf('\n');
                        if (idx < 0) break;

                        string line = text.Substring(0, idx).TrimEnd('\r');
                        sb.Remove(0, idx + 1);
                        Console.WriteLine(line);
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("[Client] Disconnected from server.");
            }
            catch (ObjectDisposedException)
            {
                // 忽略：socket 已關閉
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Client] Receive error: " + ex.Message);
            }
        }

        static void SafeClose(Socket s)
        {
            try { s.Shutdown(SocketShutdown.Both); } catch { }
            try { s.Close(); } catch { }
            try { s.Dispose(); } catch { }
        }
    }
}
