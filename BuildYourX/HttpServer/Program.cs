using HttpServer.App.Http;

namespace HttpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = 6569;
            Console.WriteLine($"Server listening on 127.0.0.1:{port}");
            _ = Task.Run(async () => await HttpServerLite.RunAsync(port));
            Console.ReadKey();
        }
    }
}
