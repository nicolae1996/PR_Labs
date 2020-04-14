using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GR.Core.Extensions;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Models;

namespace Shared
{
    public abstract class BaseSocketCommunication
    {
        /// <summary>
        /// Decrypt packet
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        protected Packet DecryptPacket(string encryptedString)
        {
            var decrypted = EncryptTool.Decrypt(encryptedString, GlobalResources.SecretKey);
            return decrypted.Deserialize<Packet>();
        }


        /// <summary>
        /// Encrypt packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        protected string EncryptPacket(Packet packet)
        {
            var str = packet.SerializeAsJson();
            var encrypted = EncryptTool.Encrypt(str, GlobalResources.SecretKey);
            return encrypted;
        }

        /// <summary>
        /// Send packet
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task<Result<Packet>> SendAndReceivePacketAsync(Socket socket, Packet packet)
        {
            var sendResponse = await SendPacketAsync(socket, packet);
            if (!sendResponse.Success) return Result.Fail<Packet>(sendResponse.Error);

            var buffer = new byte[StateObject.BufferSize];

            var receiveResult = await socket.CustomReceiveWithTimeoutAsync(
                    buffer,
                    0,
                    StateObject.BufferSize,
                    0,
                    GlobalResources.Timeout).ConfigureAwait(false);

            var bytesReceived = receiveResult.Value;
            if (bytesReceived == 0)
            {
                return Result.Fail<Packet>("Error reading message from client, no data was received");
            }

            var receivedEncryptedMessage = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

            var receivedPacket = DecryptPacket(receivedEncryptedMessage);

            return Result.Ok(receivedPacket);
        }

        /// <summary>
        /// Send packet
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task<Result> SendPacketAsync(Socket socket, Packet packet)
        {
            var data = EncryptPacket(packet);
            var bytes = Encoding.ASCII.GetBytes(data);

            var sendResponse = await socket.CustomSendWithTimeoutAsync(
                    bytes,
                    0,
                    bytes.Length,
                    0,
                    GlobalResources.Timeout)
                .ConfigureAwait(false);
            return sendResponse;
        }
    }
}