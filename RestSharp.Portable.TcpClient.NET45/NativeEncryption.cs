using System.Security.Cryptography;

namespace RestSharp.Portable.TcpClient
{
    internal static class NativeEncryption
    {
        public static byte[] HashMD5(byte[] data)
        {
            using (var hash = MD5.Create())
                return hash.ComputeHash(data);
        }
    }
}