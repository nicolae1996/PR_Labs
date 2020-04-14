using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GR.Core.Extensions;
using Shared;
using Shared.Enums;
using Shared.Helpers;
using Shared.Models;

namespace Server
{
    public class ServerAsynchronousSocketListener : BaseSocketCommunication
    {
        /// <summary>
        /// Connections
        /// </summary>
        protected ConcurrentDictionary<Guid, Connection> Connections = new ConcurrentDictionary<Guid, Connection>();

        /// <summary>
        /// Ip address
        /// </summary>
        protected IPAddress IpAddress { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        protected int Port { get; set; } = 11000;

        #region Async Events

        // Thread signal. 
        protected ManualResetEvent AllDone = new ManualResetEvent(false);

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public ServerAsynchronousSocketListener()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[2];
            IpAddress = ipAddress;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public ServerAsynchronousSocketListener(IPAddress address, int port)
        {
            Port = port;
            IpAddress = address;
        }

        public void Start()
        {
            Console.WriteLine($"Server started on {IpAddress} with port {Port}");
            var localEndPoint = new IPEndPoint(IpAddress, Port);

            // Create a TCP/IP socket.  
            var listener = new Socket(IpAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    AllDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        AcceptCallback,
                        listener);

                    // Wait until a connection is made before continuing.  
                    AllDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            AllDone.Set();

            // Get the socket that handles the client request.  
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            // Create new connection
            var connection = new Connection(handler);

            Connections.TryAdd(connection.ConnectionId, connection);
            handler.BeginReceive(connection.StateObject.Buffer, 0, StateObject.BufferSize, 0, ReadCallback, connection);
        }

        /// <summary>
        /// Read callback
        /// </summary>
        /// <param name="ar"></param>
        public async void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            var connection = (Connection)ar.AsyncState;
            var handler = connection.StateObject.WorkSocket;

            try
            {
                // Read data from the client socket.
                var bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    var receivedStringPacket = Encoding.ASCII.GetString(connection.StateObject.Buffer, 0, bytesRead);
                    var packet = DecryptPacket(receivedStringPacket);
                    await ProcessPacketAsync(connection, packet);
                }

                handler.BeginReceive(connection.StateObject.Buffer, 0, StateObject.BufferSize, 0,
                    ReadCallback, connection);
            }
            catch (SocketException)
            {
                CloseClientConnection(connection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Close connection
        /// </summary>
        /// <param name="connection"></param>
        public void CloseClientConnection(Connection connection)
        {
            Connections.TryRemove(connection.ConnectionId, out _);
        }

        /// <summary>
        /// Process packet
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task ProcessPacketAsync(Connection conn, Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.Authentication:
                    await AuthorizeClientAsync(conn, packet);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Authorize
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task AuthorizeClientAsync(Connection conn, Packet packet)
        {
            var authData = packet.Data.FirstOrDefault(x => x.Key.Equals(GlobalResources.CommonKeys.Authentication)).Value.Deserialize<AuthenticationCredentials>();
            var user = InMemoryUsers.Users.FirstOrDefault(x =>
                x.UserName.Equals(authData.UserName) && x.Password.Equals(authData.Password));

            var responsePacket = new Packet
            {
                Type = PacketType.AuthenticationResponse,
                Token = CreateUserToken(user),
                Data = new Dictionary<string, string>
                {
                    { "connection",  conn.ConnectionId.ToString() },
                    { "userInfo",  user.SerializeAsJson() }
                }
            };

            conn.IsAuthenticated = true;
            conn.User = user;
            await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
        }

        /// <summary>
        /// Create user token
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public string CreateUserToken(User user)
            => EncryptTool.Encrypt($"{user.UserName}:{user.Password}", GlobalResources.SecretKey);

        /// <summary>
        /// Get user name from token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public User GetUserFromToken(string token)
        {
            var basicStr = EncryptTool.Decrypt(token, GlobalResources.SecretKey);
            if (basicStr.IsNullOrEmpty()) return null;
            var spl = basicStr.Split(":");
            var user = spl[0];
            var pass = spl[1];
            return InMemoryUsers.Users.FirstOrDefault(x => x.UserName.Equals(user) && x.Password.Equals(pass));
        }
    }
}