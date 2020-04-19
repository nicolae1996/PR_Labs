namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new FileTransferServer();
            server.Start();
        }
    }
}
