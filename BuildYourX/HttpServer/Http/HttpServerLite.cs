using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HttpServer.App.Http
{
    public static class HttpServerLite
    {
        public static async Task RunAsync(int port, CancellationToken ct = default)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!listener.Pending())
                    {
                        await Task.Delay(10, ct);
                        continue;
                    }

                    _ = HandleOneAsync(await listener.AcceptTcpClientAsync(ct), ct);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleOneAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            {
                using (var stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    if (stream.DataAvailable)
                    {
                      _ = await stream.ReadAsync(buffer.AsMemory(0,Math.Min(buffer.Length, stream.DataAvailable ? buffer.Length:0)),ct);
                    }
                    else
                    {
                        await Task.Delay(10, ct);
                    }

                    var body = "Hello";
                    var headers="HTTP/1.1 200 OK\r\n"+
                        $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n"+
                        "Content-Type: text/plain\r\n"+
                        "Connection: close\r\n"+
                        "\r\n";

                    var resp=Encoding.ASCII.GetBytes(headers+body);
                    await stream.WriteAsync(resp,0,resp.Length, ct);
                    await stream.FlushAsync(ct);
                }
            }
        }
    }
}
