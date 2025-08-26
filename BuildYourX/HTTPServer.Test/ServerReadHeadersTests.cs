using System.Net.Sockets;
using System.Text;

namespace HttpServer.Test
{
    public class ServerReadHeadersTests
    {
        private const int Port = 6569;

        [Fact]
        public async Task DoesNotRespondsBeforeHeadersComplete()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                var serverTask = HttpServer.App.Http.HttpServerLite.RunAsync(Port, cts.Token);
                await Task.Delay(80);

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", Port);
                    using (var stream = client.GetStream())
                    {
                        var part1 = "GET / HTTP/1.1\r\nHost: example";
                        var bytes1 = Encoding.ASCII.GetBytes(part1);
                        await stream.WriteAsync(bytes1, 0, bytes1.Length, cts.Token);

                        var probeCts = new CancellationTokenSource();

                        var readTask = stream.ReadAsync(new byte[1], 0, 1, probeCts.Token);
                        var completed = await Task.WhenAny(readTask, Task.Delay(150, cts.Token));
                        Assert.NotEqual(readTask, completed);
                        probeCts.Cancel();
                        try { await completed; } catch (OperationCanceledException) { }

                        var part2 = "\r\n\r\n";
                        var bytes2 = Encoding.ASCII.GetBytes(part2);
                        await stream.WriteAsync(bytes2, 0, bytes2.Length, cts.Token);

                        var buf = new byte[1024];
                        var read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                        var resp = Encoding.ASCII.GetString(buf, 0, read);
                        Assert.Contains("HTTP/1.1 200 OK", resp);

                        cts.Cancel();
                    }
                }
                try { await serverTask; } catch { }
            }
        }

        [Fact]
        public async Task HandlesFragmentedHeadersAcrossMultipleWrites()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                var serverTask = HttpServer.App.Http.HttpServerLite.RunAsync(Port, cts.Token);
                await Task.Delay(80);

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", Port);
                    using (var stream = client.GetStream())
                    {
                        string[] chunks =
                        {
                            "GET /path?q=1 HTTP/1.1\r\nHo",
                            "st: localhost\r\nUser-A",
                            "gent: Mini\r\n",
                            "\r\n"
                        };

                        foreach (var chunk in chunks)
                        {
                            var b = Encoding.ASCII.GetBytes(chunk);
                            await stream.WriteAsync(b, 0, b.Length, cts.Token);
                            await Task.Delay(20, cts.Token);
                        }

                        var buf = new byte[1024];
                        var read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                        var resp = Encoding.ASCII.GetString(buf, 0, read);
                        Assert.Contains("HTTP/1.1 200 OK", resp);

                        cts.Cancel();
                    }
                }
                try { await serverTask; } catch { }
            }
        }

        [Fact]
        public async Task Returns431WhenHeadersTooLarge()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                var serverTask = HttpServer.App.Http.HttpServerLite.RunAsync(Port, cts.Token);
                await Task.Delay(80);

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", Port);
                    using (var stream = client.GetStream())
                    {
                        var sb = new StringBuilder();
                        sb.Append("GET / HTTP/1.1\r\n");
                        sb.Append("Host: x\r\n");
                        sb.Append("x-Fill: ");
                        sb.Append(new string('A', 10000));
                        sb.Append("\r\n");

                        var bytes = Encoding.ASCII.GetBytes(sb.ToString());
                        await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token);

                        var buf = new byte[4096];
                        var read = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
                        var resp = Encoding.ASCII.GetString(buf, 0, read);
                        Assert.Contains("HTTP/1.1 431", resp);

                        cts.Cancel();
                    }
                }

                try { await serverTask; } catch { }
            }
        }
    }
}
