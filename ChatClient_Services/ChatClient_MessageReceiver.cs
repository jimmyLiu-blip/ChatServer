using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClient_Practice.Services
{
    public class MessageReceiver
    {
        private readonly Socket _socket;

        public MessageReceiver(Socket socket)
        {
            _socket = socket;
        }

        public void ReceiveLoop(CancellationToken ct)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _socket.Connected)
                {
                    int n = _socket.Receive(buffer);
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
                // Socket 已關閉
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Client] Receive error: " + ex.Message);
            }
        }
    }
}
