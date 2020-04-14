using System.Net.Sockets;
using System.Text;

namespace Shared.Models
{
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 20;


        // Client socket.  
        public Socket WorkSocket = null;
        // Receive buffer.  
        public byte[] Buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder Sb = new StringBuilder();
    }
}