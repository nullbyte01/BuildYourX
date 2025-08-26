using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.App.Http
{
    public static class HttpRequestHeader
    {
        private static readonly byte[] Terminator = Encoding.ASCII.GetBytes("\r\n\r\n");

        public static async Task<byte[]> ReadHeadersAsync(Stream stream, int maxHeaderBytes, CancellationToken ct)
        {
            var buffer = new byte[2048];
            using (var ms = new MemoryStream(Math.Min(maxHeaderBytes, 8192)))
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                    if (read == 0)
                    {
                        throw new IOException("Connection closed before headers completed.");
                    }

                    ms.Write(buffer, 0, read);

                    if (ms.Length > maxHeaderBytes)
                    {
                        throw new HeaderTooLargeException();
                    }

                    if (EndsWith(ms, Terminator))
                    {
                        return ms.ToArray();
                    }
                }
            }
        }

        public static bool EndsWith(MemoryStream ms, byte[] suffix)
        {
            var len = (int)ms.Length;
            if (len < suffix.Length) return false;

            var buf = ms.GetBuffer();

            for (int i = 0; i < suffix.Length; i++)
            {
                if (buf[len - suffix.Length + i] != suffix[i]) return false;
            }
            return true;
        }

        public sealed class HeaderTooLargeException : Exception { }
    }
}
