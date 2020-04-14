using System;
using System.Threading.Tasks;
using Client.Helpers;
using Shared.Models;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var client = new AsynchronousClient();
                await client.StartClientAsync();

                await client.AuthenticateAsync(new AuthenticationCredentials
                {
                    UserName = "admin",
                    Password = "admin"
                });

                Console.ReadKey();

            }).Wait();
        }
    }
}
