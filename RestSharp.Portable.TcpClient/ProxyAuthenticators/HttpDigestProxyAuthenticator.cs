﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RestSharp.Portable.TcpClient.ProxyAuthenticators
{
    public class HttpDigestProxyAuthenticator : IProxyAuthenticationModule
    {
        private readonly string _authHeaderName;

        private readonly NetworkCredential _credential;

        private string _realm;

        private string _nonce;

        private QualityOfProtection _qop = QualityOfProtection.Undefined;

        private string _cnonce;

        private string _opaque;

        private Algorithm _algorithm = Algorithm.Undefined;

        private DateTime _cnonceDate;

        private int _nc;

        /// <summary>Initializes a new instance of the <see cref="HttpDigestProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        /// <param name="authorizationData">Data from the Proxy-Authenticate header</param>
        public HttpDigestProxyAuthenticator(NetworkCredential credential, string authorizationData)
            : this(credential, authorizationData, "Proxy-Authorization")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="HttpDigestProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        /// <param name="authorizationData">Data from the Proxy-Authenticate header</param>
        /// <param name="headerEntryName">The header entry name that will be used to store the authentication value</param>
        public HttpDigestProxyAuthenticator(NetworkCredential credential, string authorizationData, string headerEntryName)
        {
            _credential = credential;
            _authHeaderName = headerEntryName;
            ParseResponseHeader(authorizationData);
        }

        [Flags]
        private enum QualityOfProtection
        {
            Undefined = 0,
            Auth = 1,
            AuthInt = 2,
        }

        private enum Algorithm
        {
            Undefined = 0,
            [SuppressMessage("StyleCopPlus.StyleCopPlusRules", "SP0100:AdvancedNamingRules", Justification = "Reviewed. Suppression is OK here.")]
            MD5,
            // ReSharper disable once InconsistentNaming
            [SuppressMessage("StyleCopPlus.StyleCopPlusRules", "SP0100:AdvancedNamingRules", Justification = "Reviewed. Suppression is OK here.")]
            MD5sess,
        }

        private bool HasDigestHeader
        {
            get
            {
                return !string.IsNullOrEmpty(_cnonce) && DateTime.Now.Subtract(_cnonceDate).TotalHours < 1.0;
            }
        }

        /// <summary>Modifies the request to ensure that the authentication requirements are met.</summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            if (HasDigestHeader)
            {
                var digestHeader = GetDigestHeader(client, request);
                request.AddParameter(_authHeaderName, digestHeader, ParameterType.HttpHeader);
            }
        }

        private static string CalculateMd5Hash(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            return CalculateMd5Hash(inputBytes);
        }

        private static string CalculateMd5Hash(byte[] inputBytes)
        {
            var hash = NativeEncryption.HashMD5(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string GrabHeaderVar(string varName, string header, string defaultValue = null)
        {
            var regHeader = new Regex(string.Format(@"{0}\s*=\s*((""(?<qval>[^""]*)"")|(?<val>[^,]+))", varName));
            var matchHeader = regHeader.Match(header);
            if (matchHeader.Success)
            {
                var qval = matchHeader.Groups["qval"];
                if (qval.Success)
                    return qval.Value;
                var val = matchHeader.Groups["val"];
                return val.Value.Trim();
            }

            if (defaultValue == null)
                throw new WebException(string.Format("Header {0} not found", varName), WebExceptionStatus.UnknownError);
            return defaultValue;
        }

        private string GetDigestHeader(IRestClient client, IRestRequest restRequest)
        {
            _nc = _nc + 1;

            var uri = client.BuildUri(restRequest);

            var pathAndQuery = uri.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped);

            if (_algorithm == Algorithm.Undefined)
                throw new InvalidOperationException("Algorithm not set");

            string ha2, digestResponse;

            var ha1 = CalculateMd5Hash(string.Format("{0}:{1}:{2}", _credential.UserName, _realm, _credential.Password));

            string algorithm;
            switch (_algorithm)
            {
                case Algorithm.MD5sess:
                    ha1 = CalculateMd5Hash(string.Format("{0}:{1}:{2}", ha1, _nonce, _cnonce));
                    algorithm = "MD5-sess";
                    break;
                default:
                    algorithm = "MD5";
                    break;
            }

            string qop;
            switch (_qop)
            {
                case QualityOfProtection.Auth:
                    qop = "auth";
                    break;
                case QualityOfProtection.AuthInt:
                    qop = "auth-int";
                    break;
                default:
                    qop = null;
                    break;
            }

            switch (_qop)
            {
                case QualityOfProtection.AuthInt:
                    {
                        var content = client.GetContent(restRequest);
                        byte[] entityBody;
                        if (content == null)
                        {
                            entityBody = new byte[0];
                        }
                        else
                        {
                            var readTask = content.ReadAsByteArrayAsync();
                            readTask.Wait();
                            entityBody = readTask.Result;
                        }

                        ha2 = CalculateMd5Hash(entityBody);
                    }

                    ha2 = CalculateMd5Hash(string.Format("{0}:{1}:{2}", client.GetEffectiveHttpMethod(restRequest).Method, pathAndQuery, ha2));
                    break;
                default:
                    ha2 = CalculateMd5Hash(string.Format("{0}:{1}", client.GetEffectiveHttpMethod(restRequest).Method, pathAndQuery));
                    break;
            }

            switch (_qop)
            {
                case QualityOfProtection.AuthInt:
                case QualityOfProtection.Auth:
                    digestResponse = CalculateMd5Hash(string.Format("{0}:{1}:{2:D8}:{3}:{4}:{5}", ha1, _nonce, _nc, _cnonce, qop, ha2));
                    break;
                default:
                    digestResponse = CalculateMd5Hash(string.Format("{0}:{1}:{2}", ha1, _nonce, ha2));
                    break;
            }

            var result = new StringBuilder();
            result
                .AppendFormat("Digest username=\"{0}\"", _credential.UserName)
                .AppendFormat(", realm=\"{0}\"", _realm)
                .AppendFormat(", nonce=\"{0}\"", _nonce)
                .AppendFormat(", uri=\"{0}\"", pathAndQuery)
                .AppendFormat(", nc={0:D08}", _nc);
            if (algorithm != "MD5")
                result.AppendFormat(", algorithm=\"{0}\"", algorithm);
            if (!string.IsNullOrEmpty(qop))
            {
                result
                    .AppendFormat(", cnonce=\"{0}\"", _cnonce)
                    .AppendFormat(", qop={0}", qop);
            }

            if (!string.IsNullOrEmpty(_opaque))
                result
                    .AppendFormat(", opaque=\"{0}\"", _opaque);
            result
                .AppendFormat(", response=\"{0}\"", digestResponse);
            return result.ToString();
        }

        private void ParseResponseHeader(string authenticateHeader)
        {
            _realm = GrabHeaderVar("realm", authenticateHeader);
            _nonce = GrabHeaderVar("nonce", authenticateHeader);

            var algorithm = GrabHeaderVar("algorithm", authenticateHeader, "MD5");
            switch (algorithm.ToLower())
            {
                case "md5":
                    _algorithm = Algorithm.MD5;
                    break;
                case "md5-sess":
                    _algorithm = Algorithm.MD5sess;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Unsupported algorithm {0}", algorithm));
            }

            var qopParts = GrabHeaderVar("qop", authenticateHeader, string.Empty)
                .Split(',');
            _qop = QualityOfProtection.Undefined;
            foreach (var qopPart in qopParts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLower()))
            {
                switch (qopPart)
                {
                    case "auth":
                        _qop |= QualityOfProtection.Auth;
                        break;
                    case "auth-int":
                        _qop |= QualityOfProtection.AuthInt;
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Unsupported QOP {0}", qopPart));
                }
            }

            _nc = 0;
            _opaque = GrabHeaderVar("opaque", authenticateHeader, string.Empty);
            _cnonce = new Random().Next(123400, 9999999).ToString(CultureInfo.InvariantCulture);
            _cnonceDate = DateTime.Now;
        }
    }
}
