using System.Threading.Tasks;
using Client.Helpers;

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


            }).Wait();
        }
    }
}
