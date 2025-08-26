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
                    try
                    {
                        var headerBytes = await HttpRequestHeader.ReadHeadersAsync(stream, 8192, ct);
                        _ = headerBytes;

                        var body = "Hello";
                        var headers = "HTTP/1.1 200 OK\r\n" +
                            $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
                            "Content-Type: text/plain\r\n" +
                            "Connection: close\r\n" +
                            "\r\n";

                        var resp = Encoding.ASCII.GetBytes(headers + body);
                        await stream.WriteAsync(resp, 0, resp.Length, ct);
                        await stream.FlushAsync(ct);
                    }
                    catch (HttpRequestHeader.HeaderTooLargeException)
                    {
                        var msg = "headers too large";
                        var resp = "HTTP/1.1 431 Request Header Fields Too Large\r\n" +
                           $"Content-Length: {msg.Length}\r\n" +
                           "Content-Type: text/plain\r\n" +
                           "Connection: close\r\n" +
                           msg;
                        var b = Encoding.ASCII.GetBytes(resp);
                        await stream.WriteAsync(b, 0, b.Length, ct);
                    }
                    catch (IOException)
                    {

                    }
                }
            }
        }
    }
}
