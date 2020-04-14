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

        /// <summary>
        /// User
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Connection id
        /// </summary>
        public Guid ConnectionId { get; set; }

        /// <summary>
        /// State
        /// </summary>
        public StateObject StateObject { get; set; }

        /// <summary>
        /// Is auth
        /// </summary>
        public bool IsAuthenticated { get; set; }
    }
}
