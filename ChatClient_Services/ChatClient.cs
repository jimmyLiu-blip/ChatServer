using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatClient_Practice.Services
{
    public class ChatClient
    {
        private readonly string _server;
        private readonly int _port;
        private Socket _socket;
        private MessageReceiver _receiver;

        public ChatClient(string server, int port)
        {
            _server = server;
            _port = port;
        }

        public void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine($"[Client] Connecting to {_server}:{_port} ...");
                _socket.Connect(IPAddress.Parse(_server), _port);
                _receiver = new MessageReceiver(_socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Connect error: {ex.Message}");
                Utils.SocketHelper.SafeClose(_socket);
                throw;
            }
        }

        public void StartReceiving(CancellationToken ct)
        {
            Task.Run(() => _receiver.ReceiveLoop(ct));
        }

        public void SendMessage(string message)
        {
            if (_socket == null || !_socket.Connected) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                _socket.Send(data);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[Client] Send error: {ex.Message}");
            }
        }

        public void Close()
        {
            Utils.SocketHelper.SafeClose(_socket);
        }
    }
}
