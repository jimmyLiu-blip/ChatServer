using System;
using System.Net.Sockets;

namespace ChatServer_Practice.Utility
{
    public static class ChatServer_SocketHelper
    {
        public static void SafeClose(Socket s)
        {
            try { s.Shutdown(SocketShutdown.Both); } catch { }
            try { s.Close(); } catch { }
            try { s.Dispose(); } catch { }
        }
    }
}
