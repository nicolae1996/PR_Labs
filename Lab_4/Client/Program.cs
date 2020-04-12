using System;
using Client.Helpers;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new AsynchronousClient();
            client.StartClient();
        }
    }
}
