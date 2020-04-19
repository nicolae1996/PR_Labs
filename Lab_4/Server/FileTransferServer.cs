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
    public class FileTransferServer : BaseSocketCommunication
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
        public FileTransferServer()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[2];
            IpAddress = ipAddress;
            OnInit();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public FileTransferServer(IPAddress address, int port)
        {
            Port = port;
            IpAddress = address;
            OnInit();
        }

        private void OnInit()
        {
            ShowWelcomeMessage();
        }

        /// <summary>
        /// Show welcome message
        /// </summary>
        public void ShowWelcomeMessage()
        {
            var message = "File Transfer server";
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            var topAndBottom = new string('-', Console.WindowWidth - 1);
            Console.Write(topAndBottom + "\n");
            Console.Write(topAndBottom + "\n");
            var padding = new string('-', (Console.WindowWidth - message.Length) / 2);
            Console.Write(padding);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write(padding + "\n");
            Console.Write(topAndBottom + "\n");
            Console.Write(topAndBottom + "\n");
            Console.ForegroundColor = ConsoleColor.White;
        }


        /// <summary>
        /// Star
        /// </summary>
        public void Start()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Server started on {IpAddress} with port {Port}");
            Console.ForegroundColor = ConsoleColor.White;

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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Total connections: {Connections.Count}");

                    Console.WriteLine("Waiting for a connection... \n");
                    Console.ForegroundColor = ConsoleColor.White;
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

        /// <summary>
        /// Callback
        /// </summary>
        /// <param name="ar"></param>
        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            // Create new connection
            var connection = new Connection(handler);

            AddNewConnection(connection);

            // Signal the main thread to continue.  
            AllDone.Set();

            handler.BeginReceive(connection.StateObject.Buffer, 0, StateObject.BufferSize, 0, ReadCallback, connection);
        }

        /// <summary>
        /// Add new connection
        /// </summary>
        /// <param name="connection"></param>
        private void AddNewConnection(Connection connection)
        {
            Connections.TryAdd(connection.ConnectionId, connection);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"New device connected with id: {connection.ConnectionId}");
            Console.ForegroundColor = ConsoleColor.White;
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection {connection.ConnectionId} was closed");
            Console.ForegroundColor = ConsoleColor.White;
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
                    await AuthorizeClientHandlerAsync(conn, packet);
                    break;
                case PacketType.GetFiles:
                    await GetFilesHandlerAsync(conn, packet);
                    break;
                case PacketType.UploadFile:
                    await UploadFileHandlerAsync(conn, packet);
                    break;
                case PacketType.DownloadFiles:
                    await DownloadFilesHandlerAsync(conn, packet);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Get files
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task GetFilesHandlerAsync(Connection conn, Packet packet)
        {
            Packet responsePacket;
            if (!conn.IsAuthenticated)
            {
                responsePacket = new Packet
                {
                    Type = PacketType.AuthenticationResponse,
                    Error = "Unauthorized"
                };
            }
            else
            {
                var user = GetUserFromToken(packet.Token);
                if (user == null)
                {
                    responsePacket = new Packet
                    {
                        Type = PacketType.AuthenticationResponse,
                        Error = "Unauthorized"
                    };
                }
                else
                {
                    var files = FileManager.GetFiles(conn.User);
                    responsePacket = new Packet
                    {
                        IsSuccessResult = true,
                        Type = PacketType.GetFiles,
                        Data = new Dictionary<string, string>
                        {
                            { "files", files.SerializeAsJson() }
                        }
                    };
                }
            }

            await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
        }

        /// <summary>
        /// Download files
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task DownloadFilesHandlerAsync(Connection conn, Packet packet)
        {
            Packet responsePacket;
            if (!conn.IsAuthenticated)
            {
                responsePacket = new Packet
                {
                    Type = PacketType.AuthenticationResponse,
                    Error = "Unauthorized"
                };
            }
            else
            {
                var user = GetUserFromToken(packet.Token);
                if (user == null)
                {
                    responsePacket = new Packet
                    {
                        Type = PacketType.AuthenticationResponse,
                        Error = "Unauthorized"
                    };
                }
                else
                {
                    var reqFiles = packet.Data.FirstOrDefault(x => x.Key.Equals("files"))
                        .Value
                        .Deserialize<IEnumerable<File>>();

                    var files = FileManager.GetDownloadFiles(reqFiles);
                    responsePacket = new Packet
                    {
                        IsSuccessResult = true,
                        Type = PacketType.DownloadFiles,
                        Data = new Dictionary<string, string>
                        {
                            { "files", files.SerializeAsJson() }
                        }
                    };
                }
            }

            await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
        }

        /// <summary>
        /// Upload file handler
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task UploadFileHandlerAsync(Connection conn, Packet packet)
        {
            Packet responsePacket;
            if (!conn.IsAuthenticated)
            {
                responsePacket = new Packet
                {
                    Type = PacketType.AuthenticationResponse,
                    Error = "Unauthorized"
                };
            }
            else
            {
                var user = GetUserFromToken(packet.Token);
                if (user == null)
                {
                    responsePacket = new Packet
                    {
                        Type = PacketType.AuthenticationResponse,
                        Error = "Unauthorized"
                    };
                }
                else
                {
                    var fileName = packet.Data.FirstOrDefault(x => x.Key.Equals("fileName")).Value;
                    var blob = packet.Data.FirstOrDefault(x => x.Key.Equals("blob")).Value.Deserialize<byte[]>();

                    var saveResult = await FileManager.UploadFile(user, fileName, blob);

                    responsePacket = new Packet
                    {
                        IsSuccessResult = saveResult.Success,
                        Error = saveResult.Error,
                        Type = PacketType.UploadFile
                    };
                }
            }

            await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
        }

        /// <summary>
        /// Authorize
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task AuthorizeClientHandlerAsync(Connection conn, Packet packet)
        {
            var authData = packet.Data.FirstOrDefault(x => x.Key.Equals(GlobalResources.CommonKeys.Authentication)).Value.Deserialize<AuthenticationCredentials>();
            var user = InMemoryUsers.Users.FirstOrDefault(x =>
                x.UserName.Equals(authData.UserName) && x.Password.Equals(authData.Password));

            if (user == null)
            {
                var responsePacket = new Packet
                {
                    Type = PacketType.AuthenticationResponse,
                    Error = "Invalid user"
                };

                await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
            }
            else
            {
                var responsePacket = new Packet
                {
                    IsSuccessResult = true,
                    Type = PacketType.AuthenticationResponse,
                    Data = new Dictionary<string, string>
                    {
                        {"connection", conn.ConnectionId.ToString()}, {"userInfo", user.SerializeAsJson()}
                    },
                    Token = CreateUserToken(user)
                };


                conn.IsAuthenticated = true;
                conn.User = user;
                await SendPacketAsync(conn.StateObject.WorkSocket, responsePacket);
            }
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
            if (token.IsNullOrEmpty()) return null;
            var basicStr = EncryptTool.Decrypt(token, GlobalResources.SecretKey);
            if (basicStr.IsNullOrEmpty()) return null;
            var spl = basicStr.Split(":");
            var user = spl[0];
            var pass = spl[1];
            return InMemoryUsers.Users.FirstOrDefault(x => x.UserName.Equals(user) && x.Password.Equals(pass));
        }
    }
}