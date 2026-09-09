using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Org.BouncyCastle.Tls;

namespace WinSub
{
    public static class BouncyHttpClient
    {
        private const int MaxRedirects = 5;
        private const int ConnectTimeoutMs = 10000;

        private static readonly Dictionary<string, PooledConnection> _pool =
            new Dictionary<string, PooledConnection>();
        private static readonly object _poolLock = new object();

        private class PooledConnection
        {
            public TcpClient Tcp;
            public Stream Stream;
            public string Host;
            public int Port;
            public int InUse;
        }

        public static string DownloadString(string url)
        {
            byte[] data = DownloadDataInternal(url);
            return Encoding.UTF8.GetString(data);
        }

        public static void DownloadFile(string url, string path)
        {
            byte[] data = DownloadDataInternal(url);
            File.WriteAllBytes(path, data);
        }

        public static byte[] DownloadData(string url)
        {
            return DownloadDataInternal(url);
        }

        private static PooledConnection AcquireConnection(string host, int port, bool https)
        {
            string key = host + ":" + port;
            lock (_poolLock)
            {
                PooledConnection conn;
                if (_pool.TryGetValue(key, out conn))
                {
                    if (conn.Tcp != null && conn.Tcp.Connected)
                    {
                        if (Interlocked.CompareExchange(ref conn.InUse, 1, 0) == 0)
                            return conn;
                    }
                    TryClose(conn);
                    _pool.Remove(key);
                }
            }

            TcpClient tcp = ConnectTcp(host, port);
            tcp.ReceiveTimeout = 30000;
            tcp.SendTimeout = 15000;

            Stream stream = tcp.GetStream();
            if (https)
            {
                try
                {
                    TlsClientProtocol protocol = new TlsClientProtocol(stream);
                    protocol.Connect(new BouncyTlsClient(host));
                    stream = protocol.Stream;
                }
                catch (Exception ex)
                {
                    try { tcp.Close(); } catch { }
                    throw new WebException("TLS handshake failed to " + host + ": " + ex.Message, ex);
                }
            }

            PooledConnection pooled = new PooledConnection
            {
                Tcp = tcp,
                Stream = stream,
                Host = host,
                Port = port,
                InUse = 1
            };

            lock (_poolLock)
            {
                PooledConnection existing;
                if (_pool.TryGetValue(key, out existing))
                {
                    if (existing.InUse == 1)
                    {
                        pooled.Host = null;
                        return pooled;
                    }
                    TryClose(existing);
                }
                _pool[key] = pooled;
            }

            return pooled;
        }

        private static TcpClient ConnectTcp(string host, int port)
        {
            IPAddress[] addrs;
            try
            {
                addrs = Dns.GetHostAddresses(host);
            }
            catch (Exception ex)
            {
                throw new WebException("DNS resolution failed for " + host + ": " + ex.Message, ex);
            }
            if (addrs == null || addrs.Length == 0)
                throw new WebException("DNS returned no addresses for " + host);

            Array.Sort(addrs, delegate(IPAddress a, IPAddress b)
            {
                bool a6 = a.AddressFamily == AddressFamily.InterNetworkV6;
                bool b6 = b.AddressFamily == AddressFamily.InterNetworkV6;
                return (a6 ? 1 : 0) - (b6 ? 1 : 0);
            });

            string lastErr = null;
            foreach (IPAddress ip in addrs)
            {
                TcpClient tcp = new TcpClient();
                try
                {
                    IAsyncResult ar = tcp.BeginConnect(ip, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(ConnectTimeoutMs))
                    {
                        lastErr = ip + ": connect timed out (" + ConnectTimeoutMs + " ms)";
                        try { tcp.Close(); } catch { }
                        continue;
                    }
                    tcp.EndConnect(ar);
                    return tcp;
                }
                catch (SocketException se)
                {
                    lastErr = ip + ": " + se.Message;
                    try { tcp.Close(); } catch { }
                }
            }

            throw new WebException("TCP connect failed to " + host + ":" + port + " (" + lastErr + ")");
        }

        private static void ReleaseConnection(PooledConnection conn, bool keepAlive)
        {
            if (conn == null) return;
            if (conn.Host == null)
            {
                TryClose(conn);
                return;
            }
            if (!keepAlive)
            {
                string key = conn.Host + ":" + conn.Port;
                lock (_poolLock)
                {
                    _pool.Remove(key);
                }
                TryClose(conn);
                return;
            }
            Interlocked.Exchange(ref conn.InUse, 0);
        }

        private static void InvalidateConnection(PooledConnection conn)
        {
            if (conn == null) return;
            if (conn.Host == null)
            {
                TryClose(conn);
                return;
            }
            string key = conn.Host + ":" + conn.Port;
            lock (_poolLock)
            {
                PooledConnection existing;
                if (_pool.TryGetValue(key, out existing) && existing == conn)
                    _pool.Remove(key);
            }
            TryClose(conn);
        }

        private static byte[] DownloadDataInternal(string url)
        {
            for (int redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
            {
                Uri uri = new Uri(url);
                bool isHttps = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
                int port = uri.Port > 0 ? uri.Port : (isHttps ? 443 : 80);
                string host = uri.Host;
                string path = uri.AbsolutePath;
                if (string.IsNullOrEmpty(path)) path = "/";
                if (!string.IsNullOrEmpty(uri.Query))
                    path += uri.Query;

                PooledConnection conn = null;
                try
                {
                    conn = AcquireConnection(host, port, isHttps);

                    string request =
                        "GET " + path + " HTTP/1.1\r\n" +
                        "Host: " + host + "\r\n" +
                        "Connection: keep-alive\r\n" +
                        "\r\n";
                    byte[] requestBytes = Encoding.UTF8.GetBytes(request);
                    conn.Stream.Write(requestBytes, 0, requestBytes.Length);
                    conn.Stream.Flush();

                    HttpResult result = ReadHttpResponse(conn.Stream);

                    if (result.StatusCode <= 0)
                        throw new IOException("Invalid/empty HTTP response (connection closed)");

                    if (result.StatusCode >= 300 && result.StatusCode < 400)
                    {
                        string location = result.GetHeader("Location");
                        if (!string.IsNullOrEmpty(location))
                        {
                            ReleaseConnection(conn, false);
                            url = location;
                            continue;
                        }
                    }

                    bool keepAlive = result.IsConnectionKeepAlive;
                    ReleaseConnection(conn, keepAlive);
                    conn = null;
                    return result.Body;
                }
                catch
                {
                    if (conn != null) InvalidateConnection(conn);
                    throw;
                }
                finally
                {
                    if (conn != null) InvalidateConnection(conn);
                }
            }

            throw new WebException("Too many redirects");
        }

        private class HttpResult
        {
            public int StatusCode;
            public byte[] Body;
            public bool IsConnectionKeepAlive;
            private string[] _headers;

            public void SetHeaders(string[] headers) { _headers = headers; }

            public string GetHeader(string name)
            {
                if (_headers == null) return null;
                string prefix = name + ":";
                for (int i = 0; i < _headers.Length; i++)
                {
                    if (_headers[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return _headers[i].Substring(prefix.Length).Trim();
                }
                return null;
            }
        }

        private static HttpResult ReadHttpResponse(Stream stream)
        {
            MemoryStream headerBuf = new MemoryStream();
            int prev = -1, cur;
            while ((cur = stream.ReadByte()) != -1)
            {
                headerBuf.WriteByte((byte)cur);
                if (prev == '\r' && cur == '\n')
                {
                    long len = headerBuf.Length;
                    if (len >= 4)
                    {
                        byte[] arr = headerBuf.ToArray();
                        if (arr[len - 4] == '\r' && arr[len - 3] == '\n' &&
                            arr[len - 2] == '\r' && arr[len - 1] == '\n')
                            break;
                    }
                }
                prev = cur;
            }

            string headerStr = Encoding.ASCII.GetString(headerBuf.ToArray());
            string[] lines = headerStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            int statusCode = 0;
            if (lines.Length > 0)
            {
                string firstLine = lines[0];
                int spaceIdx = firstLine.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    string codeStr = firstLine.Substring(spaceIdx + 1, 3);
                    int.TryParse(codeStr, out statusCode);
                }
            }

            int contentLength = -1;
            bool chunked = false;
            bool connectionClose = false;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    string val = lines[i].Substring(15).Trim();
                    int.TryParse(val, out contentLength);
                }
                if (lines[i].IndexOf("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase) >= 0)
                    chunked = true;
                if (lines[i].IndexOf("Connection: close", StringComparison.OrdinalIgnoreCase) >= 0)
                    connectionClose = true;
            }

            HttpResult result = new HttpResult();
            result.StatusCode = statusCode;
            result.SetHeaders(lines);

            if (statusCode >= 300 && statusCode < 400)
            {
                result.Body = new byte[0];
                result.IsConnectionKeepAlive = false;
                return result;
            }

            byte[] body;
            if (contentLength > 0)
            {
                body = new byte[contentLength];
                int offset = 0;
                while (offset < contentLength)
                {
                    int read = stream.Read(body, offset, contentLength - offset);
                    if (read <= 0)
                        throw new IOException("Truncated HTTP body");
                    offset += read;
                }
            }
            else if (chunked)
            {
                body = ReadChunked(stream);
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                byte[] buf = new byte[8192];
                int n;
                while ((n = stream.Read(buf, 0, buf.Length)) > 0)
                    ms.Write(buf, 0, n);
                body = ms.ToArray();
            }

            result.Body = body;
            result.IsConnectionKeepAlive = !connectionClose;
            return result;
        }

        private static byte[] ReadChunked(Stream stream)
        {
            MemoryStream result = new MemoryStream();
            while (true)
            {
                string sizeLine = ReadLine(stream);
                if (string.IsNullOrEmpty(sizeLine)) break;
                int chunkSize;
                if (!int.TryParse(sizeLine.Trim(), NumberStyles.HexNumber, null, out chunkSize))
                    break;
                if (chunkSize == 0) break;

                byte[] chunk = new byte[chunkSize];
                int offset = 0;
                while (offset < chunkSize)
                {
                    int read = stream.Read(chunk, offset, chunkSize - offset);
                    if (read <= 0) break;
                    offset += read;
                }
                result.Write(chunk, 0, chunkSize);
                stream.ReadByte();
                stream.ReadByte();
            }
            return result.ToArray();
        }

        private static string ReadLine(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            int prev = -1, cur;
            while ((cur = stream.ReadByte()) != -1)
            {
                if (prev == '\r' && cur == '\n') break;
                ms.WriteByte((byte)cur);
                prev = cur;
            }
            return Encoding.ASCII.GetString(ms.ToArray()).TrimEnd('\r', '\n');
        }

        private static void TryClose(PooledConnection conn)
        {
            try { if (conn.Stream != null) conn.Stream.Close(); } catch { }
            try { if (conn.Tcp != null) conn.Tcp.Close(); } catch { }
        }
    }
}
