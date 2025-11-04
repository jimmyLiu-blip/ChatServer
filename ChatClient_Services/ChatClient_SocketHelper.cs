using System;
using System.Net.Sockets;

namespace ChatClient_Practice.Utils
{
    public static class SocketHelper
    {
        public static void SafeClose(Socket s)
        {
            if (s == null) return;

            try { s.Shutdown(SocketShutdown.Both); } catch { }
            try { s.Close(); } catch { }
            try { s.Dispose(); } catch { }
        }
    }
}
