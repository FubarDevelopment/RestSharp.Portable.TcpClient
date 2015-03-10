using System;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public class NetworkConverterTests
    {
        [Theory(DisplayName = "NetworkConverter.ParseIPv6")]
        [InlineData("2001:0db8:85a3:08d3:1319:8a2e:0370:7344", new ushort[] { 0x2001, 0x0db8, 0x85a3, 0x08d3, 0x1319, 0x8a2e, 0x0370, 0x7344 })]
        [InlineData("2001:db8:0:8d3:0:8a2e:70:7344", new ushort[] { 0x2001, 0xdb8, 0x0, 0x8d3, 0x0, 0x8a2e, 0x70, 0x7344 })]
        [InlineData("2001:db8::1428:57ab", new ushort[] { 0x2001, 0x0db8, 0x0, 0x0, 0x0, 0x0, 0x1428, 0x57ab })]
        [InlineData("2001:db8:0:0:8d3::", new ushort[] { 0x2001, 0x0db8, 0x0, 0x0, 0x8d3, 0x0, 0x0, 0x0 })]
        [InlineData("2001:db8::8d3:0:0:0", new ushort[] { 0x2001, 0x0db8, 0x0, 0x0, 0x8d3, 0x0, 0x0, 0x0 })]
        [InlineData("::ffff:127.0.0.1", new ushort[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0xffff, 0x7f00, 0x1 })]
        [InlineData("::ffff:7f00:1", new ushort[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0xffff, 0x7f00, 0x1 })]
        public void TestParseIPv6(string ipv6Text, ushort[] ipv6Data)
        {
            var data = NetworkConverter.GetWordsForIPv6(ipv6Text);
            Assert.Equal(ipv6Data, data);
        }

        [Theory(DisplayName = "NetworkConverter.IsLocalhost")]
        [InlineData("::1", true)]
        [InlineData("::ffff:127.0.0.1", true)]
        [InlineData("::ffff:7f00:1", true)]
        [InlineData("::ffff:127.0.0.2", false)]
        [InlineData("::ffff:7f01:1", false)]
        [InlineData("::ffff:7f00:2", false)]
        [InlineData("127.0.0.1", true)]
        [InlineData("127.0.0.2", false)]
        [InlineData("127.0.1.1", false)]
        [InlineData("127.1.0.1", false)]
        public void TestIsLocalhost(string ipv6Text, bool isLoopBack)
        {
            Assert.Equal(isLoopBack, NetworkConverter.IsLoopBack(ipv6Text));
        }

        [Theory(DisplayName = "NetworkConverter.ConvertToIPv6")]
        [InlineData("2001:db8:85a3:8d3:1319:8a2e:370:7344", new ushort[] { 0x2001, 0x0db8, 0x85a3, 0x08d3, 0x1319, 0x8a2e, 0x0370, 0x7344 })]
        [InlineData("2001:db8::8d3:0:8a2e:70:7344", new ushort[] { 0x2001, 0xdb8, 0x0, 0x8d3, 0x0, 0x8a2e, 0x70, 0x7344 })]
        [InlineData("2001:db8::1428:57ab", new ushort[] { 0x2001, 0x0db8, 0x0, 0x0, 0x0, 0x0, 0x1428, 0x57ab })]
        [InlineData("2001:db8:0:0:8d3::", new ushort[] { 0x2001, 0x0db8, 0x0, 0x0, 0x8d3, 0x0, 0x0, 0x0 })]
        [InlineData("::ffff:7f00:1", new ushort[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0xffff, 0x7f00, 0x1 })]
        public void TestConvertToIPv6(string ipv6Text, ushort[] ipv6Data)
        {
            var ipv6 = NetworkConverter.ToIPv6(ipv6Data);
            Assert.Equal(ipv6Text, ipv6);
            var words = NetworkConverter.GetWordsForIPv6(ipv6);
            Assert.Equal(ipv6Data, words);
        }
    }
}
