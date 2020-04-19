using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GR.Core.Extensions;
using Shared;
using Shared.Enums;
using Shared.Extensions;
using Shared.Models;

namespace Client
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

        protected string Token { get; set; }

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
                if (!authResult.Value.IsSuccessResult)
                {
                    return Result.Fail<Guid>(authResult.Value.Error);
                }

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

                Token = authResult.Value.Token;
                return Result.Ok(connection);
            }

            return Result.Fail<Guid>(authResult.Error);
        }


        /// <summary>
        /// Get files
        /// </summary>
        /// <returns></returns>
        public async Task<Result<IEnumerable<File>>> GetFilesAsync()
        {
            var packet = new Packet
            {
                Type = PacketType.GetFiles,
                Token = Token
            };
            var result = await SendAndReceivePacketAsync(Client, packet);
            if (result.Success)
            {
                if (!result.Value.IsSuccessResult)
                {
                    return Result.Fail<IEnumerable<File>>(result.Value.Error);
                }

                var files = result.Value.Data
                    .FirstOrDefault(x => x.Key.Equals("files"))
                    .Value
                    .Deserialize<IEnumerable<File>>();

                return Result.Ok(files);
            }

            return Result.Fail<IEnumerable<File>>(result.Error);
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="name"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public async Task<Result> UploadFileAsync(string name, byte[] blob)
        {
            var packet = new Packet
            {
                Type = PacketType.UploadFile,
                Token = Token,
                Data = new Dictionary<string, string>
                {
                    { "fileName", name },
                    { "blob", blob.SerializeAsJson() }
                }
            };
            var result = await SendAndReceivePacketAsync(Client, packet);
            if (result.Success)
            {
                if (!result.Value.IsSuccessResult)
                {
                    return Result.Fail(result.Value.Error);
                }

                return Result.Ok();
            }

            return Result.Fail(result.Error);
        }

        /// <summary>
        /// Download files
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<Result<IEnumerable<FileResult>>> DownloadFileAsync(IEnumerable<File> files)
        {
            var packet = new Packet
            {
                Type = PacketType.DownloadFiles,
                Token = Token,
                Data = new Dictionary<string, string>
                {
                    { "files", files.SerializeAsJson() }
                }
            };
            var result = await SendAndReceivePacketAsync(Client, packet);
            if (result.Success)
            {
                if (!result.Value.IsSuccessResult)
                {
                    return Result.Fail<IEnumerable<FileResult>>(result.Value.Error);
                }

                var downFiles = result.Value.Data
                    .FirstOrDefault(x => x.Key.Equals("files"))
                    .Value
                    .Deserialize<IEnumerable<FileResult>>();

                return Result.Ok(downFiles);
            }

            return Result.Fail<IEnumerable<FileResult>>(result.Error);
        }
    }
}
