namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new ServerAsynchronousSocketListener();
            server.Start();
        }
    }
}
