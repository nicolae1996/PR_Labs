using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new AsynchronousSocketListener();
            server.StartListening();
            Console.WriteLine("Hello World!");
        }
    }
}
