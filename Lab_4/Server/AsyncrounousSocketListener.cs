using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shared.Models;

namespace Server
{
    public class ServerAsynchronousSocketListener
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
            var ipAddress = ipHostInfo.AddressList[0];
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

        public static void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            var connection = (Connection)ar.AsyncState;
            var handler = connection.StateObject.WorkSocket;

            // Read data from the client socket.
            var bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                connection.StateObject.Sb.Append(Encoding.ASCII.GetString(
                    connection.StateObject.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                var content = connection.StateObject.Sb.ToString();
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);

                if (content.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);


                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(connection.StateObject.Buffer, 0, StateObject.BufferSize, 0,
                        ReadCallback, connection);
                }
            }
        }

        private static void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            var byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var handler = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.  
                var bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}