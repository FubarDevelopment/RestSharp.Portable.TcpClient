using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public static class TcpStreamUtils
    {
        public static void WriteHttpHeader(
            this TextWriter writer,
            IDictionary<string, IList<string>> httpHeaderEntries)
        {
            foreach (var header in httpHeaderEntries)
                writer.WriteLine("{0}: {1}", header.Key, string.Join(",", header.Value));
        }

        public static async Task WriteHttpHeaderAsync(
            this TextWriter writer,
            IDictionary<string, IList<string>> httpHeaderEntries)
        {
            foreach (var header in httpHeaderEntries)
                await writer.WriteLineAsync(string.Format("{0}: {1}", header.Key, string.Join(",", header.Value)));
        }

        public static void WriteHttpHeader(
            this TextWriter writer,
            HttpHeaders headers)
        {
            foreach (var header in headers)
                writer.WriteLine("{0}: {1}", header.Key, string.Join(",", header.Value));
        }

        public static async Task WriteHttpHeaderAsync(
            this TextWriter writer,
            HttpHeaders headers)
        {
            foreach (var header in headers)
                await writer.WriteLineAsync(string.Format("{0}: {1}", header.Key, string.Join(",", header.Value)));
        }
    }
}
