using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using RestSharp.Portable.TcpClient.Pooling;

namespace RestSharp.Portable.TcpClient
{
    internal class TcpClientResponseMessage : HttpResponseMessage
    {
        private static readonly Dictionary<string, HttpHeaderDelegate> _httpHeaderActions =
            new Dictionary<string, HttpHeaderDelegate>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Content-Length", (values, response, handler) =>
                        {
                            var v = GetContentLength(values);
                            if (v == null)
                                return;
                            response.Content.Headers.ContentLength = v.Value;
                        }
                    },
                    { "Set-Cookie", SetCookies },
                    { "Set-Cookie2", SetCookies },
                };

        private static readonly Regex _statusLineRegex = new Regex(@"^HTTP/(?<version>\d.\d)\s(?<code>\d{3})(\s(?<reason>.*)?)?$", RegexOptions.IgnoreCase);

        private static readonly WebHeaderCollection _emptyHeaders = new WebHeaderCollection();

        private readonly TcpClientMessageHandler _handler;

        private readonly Uri _requestUri;

        private readonly IPooledConnection _connection;

        private bool _disposed;

        private TcpClientStream _networkStream;

        public TcpClientResponseMessage(HttpRequestMessage request, Uri requestUri, TcpClientMessageHandler handler, IPooledConnection connection)
        {
            _connection = connection;
            _requestUri = requestUri;
            _handler = handler;
            RequestMessage = request;
        }

        private delegate void HttpHeaderDelegate(string[] values, TcpClientResponseMessage response, TcpClientMessageHandler handler);

        public async Task Parse(Stream inputStream, CancellationToken cancellationToken, int? maximumStatusLineLength)
        {
            var statusLineData = await ReadBuffer(inputStream, maximumStatusLineLength, cancellationToken);

            Match statusLineMatch;
            if (!statusLineData.Item3)
            {
                // Simple response
                statusLineMatch = Match.Empty;
            }
            else
            {
                try
                {
                    // Is it a status line?
                    var line = Encoding.UTF8.GetString(statusLineData.Item1, 0, statusLineData.Item2);
                    statusLineMatch = _statusLineRegex.Match(line);
                }
                catch
                {
                    // Decoding failed -> Simple response
                    statusLineMatch = Match.Empty;
                }
            }

            TcpClientStream networkStream;
            WebHeaderCollection headers;
            if (!statusLineMatch.Success)
            {
                // Simple response
                var data = statusLineData.Item1;
                networkStream = new TcpClientStream(data, inputStream, null);
                Version = WellKnownHttpVersions.Version09;
                headers = _emptyHeaders;
            }
            else
            {
                // Full response
                StatusCode = (HttpStatusCode)int.Parse(statusLineMatch.Groups["code"].Value);
                Version = Version.Parse(statusLineMatch.Groups["version"].Value);
                var reasonGroup = statusLineMatch.Groups["reason"];
                if (reasonGroup.Success && !string.IsNullOrEmpty(reasonGroup.Value))
                    ReasonPhrase = reasonGroup.Value;

                headers = await ReadHeaders(inputStream, cancellationToken);
                var contentLength = GetContentLength(headers.GetValues("Content-Length"));
                networkStream = new TcpClientStream(new byte[0], inputStream, contentLength);
            }

            _networkStream = networkStream;
            Content = new StreamContent(new NonDisposableStream(networkStream));
            SetHeaders(headers);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_connection != null && Headers.ConnectionClose.GetValueOrDefault())
                {
                    _connection.Dispose();
                }
                else if (Version >= WellKnownHttpVersions.Version10)
                {
                    // Ensure that all content is read!
                    _networkStream.CopyTo(Stream.Null);
                }

                Content.Dispose();
                _disposed = true;
            }

            base.Dispose(disposing);
        }

        private static bool IsBufferFull(ICollection<byte> buffer, int? maxLength)
        {
            if (!maxLength.HasValue)
                return false;
            return buffer.Count >= maxLength.Value;
        }

        private static async Task<string> ReadLine(Stream stream, CancellationToken cancellationToken)
        {
            var info = await ReadBuffer(stream, null, cancellationToken);
            return Encoding.UTF8.GetString(info.Item1, 0, info.Item2);
        }

        private static async Task<Tuple<byte[], int, bool>> ReadBuffer(Stream stream, int? maxLength, CancellationToken cancellationToken)
        {
            var buffer = new List<byte>(maxLength ?? 100);
            var bufferLength = 0;
            var eolFound = false;

            var tmp = new byte[1];
            while (!IsBufferFull(buffer, maxLength) && (await stream.ReadAsync(tmp, 0, 1, cancellationToken)) != 0)
            {
                var b = tmp[0];
                buffer.Add(b);
                if (b == 10)
                {
                    eolFound = true;
                    break;
                }

                if (b == 13)
                    continue;
                bufferLength = buffer.Count;
            }

            return new Tuple<byte[], int, bool>(buffer.ToArray(), bufferLength, eolFound);
        }

        private static long? GetContentLength(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;
            long v;
            if (!long.TryParse(values[0], out v))
                return null;
            return v;
        }

        private static void SetCookies(
            IEnumerable<string> values,
            TcpClientResponseMessage response,
            TcpClientMessageHandler handler)
        {
            if (response._handler == null || !response._handler.UseCookies)
                return;
            foreach (var value in values)
            {
                response._handler.CookieContainer.SetCookies(response._requestUri, value);
            }
        }

        private static KeyValuePair<string, string> GetKeyValue(IEnumerable<string> lines)
        {
            var entry = string.Join("\r\n", lines);
            var idx = entry.IndexOf(':');
            var key = entry.Substring(0, idx).TrimEnd();
            var value = entry.Substring(idx + 1);
            return new KeyValuePair<string, string>(key, value);
        }

        private void SetHeaders(IDictionary<string, IList<string>> headers)
        {
            foreach (var header in headers)
            {
                var key = header.Key;
                var values = header.Value.ToArray();
                HttpHeaderDelegate httpHeaderDelegate;
                if (_httpHeaderActions.TryGetValue(key, out httpHeaderDelegate))
                {
                    httpHeaderDelegate(values, this, _handler);
                }
                else
                {
                    if (!Headers.TryAddWithoutValidation(key, values))
                        Content.Headers.TryAddWithoutValidation(key, values);
                }
            }
        }

        private async Task<WebHeaderCollection> ReadHeaders(Stream stream, CancellationToken cancellationToken)
        {
            var result = new WebHeaderCollection();
            var header = new List<string>();
            string line;
            while (!string.IsNullOrEmpty(line = await ReadLine(stream, cancellationToken)))
            {
                if (line.StartsWith(" ") || line.StartsWith("\t"))
                {
                    header.Add(line);
                }
                else if (header.Count != 0)
                {
                    var kvp = GetKeyValue(header);
                    result.Add(kvp.Key, kvp.Value);

                    header.Clear();
                    header.Add(line);
                }
                else
                {
                    header.Add(line);
                }
            }

            if (header.Count != 0)
            {
                var kvp = GetKeyValue(header);
                result.Add(kvp.Key, kvp.Value);
            }

            return result;
        }
    }
}
