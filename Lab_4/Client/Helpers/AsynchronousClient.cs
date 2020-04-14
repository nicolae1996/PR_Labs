using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GR.Core.Extensions;
using Shared.Models;
using Shared.Extensions;
using Shared;
using Shared.Enums;

namespace Client.Helpers
{
    public class AsynchronousClient : BaseSocketCommunication
    {
        /// <summary>
        /// Port
        /// </summary>
        protected int Port { get; set; } = 11000;

        /// <summary>
        /// Address
        /// </summary>
        protected IPAddress IpAddress { get; set; }
        /// <summary>
        /// Client connection
        /// </summary>
        protected Connection ClientConnection { get; set; }

        /// <summary>
        /// Client
        /// </summary>
        protected Socket Client { get; set; }

        #region Constructors

        public AsynchronousClient()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[2];
            IpAddress = ipAddress;
        }

        public AsynchronousClient(IPAddress address, int port)
        {
            IpAddress = address;
            Port = port;
        }

        #endregion


        /// <summary>
        /// Start client
        /// </summary>
        public async Task StartClientAsync()
        {
            var remoteEp = new IPEndPoint(IpAddress, Port);

            // Create a TCP/IP socket.  
            Client = new Socket(IpAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            var connectResponse = await Client.CustomConnectAsync(remoteEp);
            if (!connectResponse.Success)
                throw new Exception("Fail to connect");
        }

        /// <summary>
        /// Close connection
        /// </summary>
        public void CloseConnection()
        {
            // Release the socket.  
            Client.Shutdown(SocketShutdown.Both);
            Client.Close();
        }

        /// <summary>
        /// Authenticate
        /// </summary>
        /// <returns></returns>
        public async Task<Result<Guid>> AuthenticateAsync(AuthenticationCredentials credentials)
        {
            var packet = new Packet
            {
                Type = PacketType.Authentication,
                Data = new Dictionary<string, string>
                {
                    { GlobalResources.CommonKeys.Authentication, credentials.SerializeAsJson() }
                }
            };
            var authResult = await SendAndReceivePacketAsync(Client, packet);
            if (authResult.Success)
            {
                var connection = authResult.Value.Data
                    .FirstOrDefault(x => x.Key.Equals("connection"))
                    .Value
                    .ToGuid();

                var user = authResult.Value.Data
                    .FirstOrDefault(x => x.Key == "user")
                    .Value
                    .Deserialize<User>();

                ClientConnection = new Connection(Client, true)
                {
                    ConnectionId = connection,
                    User = user
                };

                return Result.Ok(connection);
            }

            return Result.Fail<Guid>(authResult.Error);
        }
    }
}
