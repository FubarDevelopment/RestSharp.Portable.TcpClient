using System;
using System.Diagnostics;
using System.Net;
using System.Text;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace RestSharp.Portable.TcpClient.ProxyAuthenticators
{
    public class NtlmProxyAuthenticator : IProxyAuthenticationModule
    {
        private readonly string _authHeaderName;

        private readonly NetworkCredential _credential;

        /// <summary>Initializes a new instance of the <see cref="NtlmProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        /// <param name="authorizationData">Data from the Proxy-Authenticate header</param>
        public NtlmProxyAuthenticator(NetworkCredential credential, string authorizationData)
            : this(credential, authorizationData, "Proxy-Authorization")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="NtlmProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        /// <param name="authorizationData">Data from the Proxy-Authenticate header</param>
        /// <param name="headerEntryName">The header entry name that will be used to store the authentication value</param>
        public NtlmProxyAuthenticator(NetworkCredential credential, string authorizationData, string headerEntryName)
        {
            _credential = credential;
            _authHeaderName = headerEntryName;
            throw new NotImplementedException();
        }

        public static byte[] HashLmPassword(string password, Encoding encoding)
        {
            if (!encoding.IsSingleByte)
                throw new ArgumentException("LanManager password encoding must be SBCS", "encoding");

            var magic = new byte[] { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };

            var passwordBytes = encoding.GetBytes(password.ToUpperInvariant().Substring(0, Math.Min(14, password.Length)));
            Debug.Assert(passwordBytes.Length <= 14, "The password bytes must not be longer than 14 bytes.");

            var keys = new byte[21];
            var pass = new byte[14];
            Array.Copy(passwordBytes, pass, passwordBytes.Length);

            {
                var engine = CipherUtilities.GetCipher("DES/ECB");
                engine.Init(true, new KeyParameter(GetNtlmKey(pass, 0)));
                engine.ProcessBytes(magic, keys, 0);
            }

            {
                var engine = CipherUtilities.GetCipher("DES/ECB");
                engine.Init(true, new KeyParameter(GetNtlmKey(pass, 7)));
                engine.ProcessBytes(magic, keys, 8);
            }

            var result = new byte[16];
            Array.Copy(keys, result, 16);

            return result;
        }

        public static byte[] HashNtlmPassword(string password)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            var result = DigestUtilities.CalculateDigest("MD4", passwordBytes);
            return result;
        }

        public static byte[] HashNtlm2Password(string username, string password, string domain)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            var key = DigestUtilities.CalculateDigest("MD4", passwordBytes);
            var data = (username + domain).ToUpperInvariant();
            var dataBytes = Encoding.Unicode.GetBytes(data);
            var mac = MacUtilities.GetMac("HMAC-MD5");
            mac.Init(new KeyParameter(key));
            mac.BlockUpdate(dataBytes, 0, dataBytes.Length);
            var result = MacUtilities.DoFinal(mac);
            return result;
        }

        /// <summary>Modifies the request to ensure that the authentication requirements are met.</summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            throw new NotImplementedException();
        }

        private static byte[] GetNtlmKey(byte[] src, int offset)
        {
            Debug.Assert((src.Length - offset) >= 7, "At least 7 bytes must be available to create the key bytes.");
            var result = new byte[8];
            result[0] = src[offset + 0];
            result[1] = (byte)((byte)((src[offset + 0] << 7) & 0xFF) | (byte)(src[offset + 1] >> 1));
            result[2] = (byte)((byte)((src[offset + 1] << 6) & 0xFF) | (byte)(src[offset + 2] >> 2));
            result[3] = (byte)((byte)((src[offset + 2] << 5) & 0xFF) | (byte)(src[offset + 3] >> 3));
            result[4] = (byte)((byte)((src[offset + 3] << 4) & 0xFF) | (byte)(src[offset + 4] >> 4));
            result[5] = (byte)((byte)((src[offset + 4] << 3) & 0xFF) | (byte)(src[offset + 5] >> 5));
            result[6] = (byte)((byte)((src[offset + 5] << 2) & 0xFF) | (byte)(src[offset + 6] >> 6));
            result[7] = (byte)((src[offset + 6] << 1) & 0xFF);
            return result;
        }
    }
}
