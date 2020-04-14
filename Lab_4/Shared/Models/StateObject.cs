using System.Net.Sockets;
using System.Text;

namespace Shared.Models
{
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = int.MaxValue / 100;


        // Client socket.  
        public Socket WorkSocket = null;

        // Receive buffer.  
        public byte[] Buffer = new byte[BufferSize];
    }
}