using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared.Models;
using Shared.Extensions;

namespace Client.Helpers
{
    public class AsynchronousClient
    {
        /// <summary>
        /// Port
        /// </summary>
        protected int Port { get; set; } = 11000;

        /// <summary>
        /// Address
        /// </summary>
        protected IPAddress IpAddress { get; set; }

        #region Events

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent _connectDone = new ManualResetEvent(false);

        #endregion


        #region Constructors

        public AsynchronousClient()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[0];
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
            // Connect to a remote device.  
            try
            {
                var remoteEp = new IPEndPoint(IpAddress, Port);

                // Create a TCP/IP socket.  
                var client = new Socket(IpAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEp, ConnectCallback, client);
                _connectDone.WaitOne();


                var messageToSend = "this is a text message from a socket";
                var messageData = Encoding.ASCII.GetBytes(messageToSend);
                var sendResponse = await client.CustomSendWithTimeoutAsync(messageData,
                    0,
                    StateObject.BufferSize,
                    0,
                    2000);


                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private Connection Authenticate(Socket socket)
        {

        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint);

                // Signal that the connection has been made.  
                _connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
