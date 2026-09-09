using System.Collections;
using System.Text;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace WinSub
{
    internal class BouncyTlsClient : DefaultTlsClient
    {
        private readonly string _hostName;

        public BouncyTlsClient(string hostName)
            : base(new BcTlsCrypto(new SecureRandom(new CryptoApiRandomGenerator())))
        {
            _hostName = hostName;
        }

        public override IDictionary GetClientExtensions()
        {
            IDictionary extensions = base.GetClientExtensions();
            if (extensions == null)
                extensions = new Hashtable();

            if (!string.IsNullOrEmpty(_hostName))
            {
                byte[] ascii = Encoding.ASCII.GetBytes(_hostName);
                ServerName serverName = new ServerName(0, ascii);
                ArrayList nameList = new ArrayList { serverName };
                TlsExtensionsUtilities.AddServerNameExtensionClient(extensions, nameList);
            }

            return extensions;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new AcceptAll();
        }

        private class AcceptAll : TlsAuthentication
        {
            public void NotifyServerCertificate(TlsServerCertificate serverCertificate) { }

            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                return null;
            }
        }
    }
}
