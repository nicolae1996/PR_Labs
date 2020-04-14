using System;
using System.Net.Sockets;

namespace Shared.Models
{
    public class Connection
    {
        public Connection(Socket socket, bool waitForConnectionId = false)
        {
            if (!waitForConnectionId) ConnectionId = Guid.NewGuid();
            StateObject = new StateObject
            {
                WorkSocket = socket
            };
        }

        /// <summary>
        /// Get socket
        /// </summary>
        /// <returns></returns>
        public Socket GetSocket() => StateObject.WorkSocket;

        public Guid ConnectionId { get; set; }

        public StateObject StateObject { get; set; }
    }
}
