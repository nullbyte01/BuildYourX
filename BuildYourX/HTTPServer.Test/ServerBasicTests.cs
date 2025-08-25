using System.Net.Sockets;
using System.Text;

namespace HTTPServer.Test
{
    public class ServerBasicTests
    {
        private const int Port = 6569;

        [Fact]
        public async Task RespondsHelloOnAnyBytes()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                var serverTask = HttpServer.App.Http.HttpServerLite.RunAsync(Port, cts.Token);

                await Task.Delay(100);

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", Port);
                    using (Stream stream = client.GetStream())
                    {
                        var req = "garbage\r\n\r\n";
                        var reqBytes = Encoding.ASCII.GetBytes(req);
                        await stream.WriteAsync(reqBytes, 0, reqBytes.Length);

                        var buf = new byte[1024];
                        var read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                        var resp = Encoding.ASCII.GetString(buf,0, read);

                        Assert.Contains("HTTP/1.1 200 OK", resp);
                        Assert.Contains("\r\n\r\nHello", resp);

                        cts.Cancel();
                        try
                        {
                           await serverTask;
                        }
                        catch { }
                    }
                }
            }
        }
    }
}