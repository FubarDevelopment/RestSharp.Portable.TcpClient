using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using RestSharp.Portable.Authenticators;

namespace RestSharp.Portable.TcpClient.Authenticators
{
    public class NtlmProxyAuthenticator : IAuthenticator
    {
        private readonly AuthHeader _authHeader;

        /// <summary>Initializes a new instance of the <see cref="NtlmProxyAuthenticator"/> class.</summary>
        public NtlmProxyAuthenticator()
            : this(AuthHeader.Www)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="NtlmProxyAuthenticator"/> class.</summary>
        /// <param name="authHeader">Authentication/Authorization header</param>
        public NtlmProxyAuthenticator(AuthHeader authHeader)
        {
            _authHeader = authHeader;
        }

        public static byte[] HashLmPassword(string password, Encoding encoding)
        {
            var magic = new byte[] { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };

            var passwordBytes = encoding.GetBytes(password.ToUpperInvariant().Substring(0, Math.Min(14, password.Length)));

            var keys = new byte[21];
            var pass = new byte[14];
            Array.Copy(passwordBytes, pass, Math.Min(passwordBytes.Length, 14));

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

        /// <summary>
        /// Does the authentication module supports pre-authentication for the given <see cref="T:RestSharp.Portable.IRestRequest"/>?
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="credentials">The credentials to be used for the authentication</param>
        /// <returns>
        /// true when the authentication module supports pre-authentication
        /// </returns>
        public bool CanPreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Does the authentication module supports pre-authentication for the given <see cref="T:System.Net.Http.HttpRequestMessage"/>?
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="credentials">The credentials to be used for the authentication</param>
        /// <returns>
        /// true when the authentication module supports pre-authentication
        /// </returns>
        public bool CanPreAuthenticate(HttpClient client, HttpRequestMessage request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines if the authentication module can handle the challenge sent with the response.
        /// </summary>
        /// <param name="client">The HTTP client the response is assigned to</param>
        /// <param name="request">The HTTP request the response is assigned to</param>
        /// <param name="credentials">The credentials to be used for the authentication</param>
        /// <param name="response">The response that returned the authentication challenge</param>
        /// <returns>
        /// true when the authenticator can handle the sent challenge
        /// </returns>
        public bool CanHandleChallenge(
            HttpClient client,
            HttpRequestMessage request,
            ICredentials credentials,
            HttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="credentials">The credentials used for the authentication</param>
        /// <returns>
        /// The task the authentication is performed on
        /// </returns>
        public Task PreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="credentials">The credentials used for the authentication</param>
        /// <returns>
        /// The task the authentication is performed on
        /// </returns>
        public Task PreAuthenticate(HttpClient client, HttpRequestMessage request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will be called when the authentication failed
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="credentials">The credentials used for the authentication</param>
        /// <param name="response">Response of the failed request</param>
        /// <returns>
        /// Task where the handler for a failed authentication gets executed
        /// </returns>
        public Task HandleChallenge(
            HttpClient client,
            HttpRequestMessage request,
            ICredentials credentials,
            HttpResponseMessage response)
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
