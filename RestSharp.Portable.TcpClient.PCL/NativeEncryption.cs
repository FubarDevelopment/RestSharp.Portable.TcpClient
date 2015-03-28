using System;

using Org.BouncyCastle.Security;

namespace RestSharp.Portable.TcpClient
{
    internal static class NativeEncryption
    {
        public static byte[] HashMD5(byte[] data)
        {
            return DigestUtilities.CalculateDigest("md5", data);
        }
    }
}