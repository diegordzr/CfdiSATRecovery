using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using CfdiSAT.Dto;
using CfdiSAT.Dto.Wsdl;
using CfdiSAT.Events;

namespace CfdiSAT.Communication
{
    public class SatSoapClient
    {

        static SatSoapClient()
        {
            CryptoConfig.AddAlgorithm(typeof(RsaPkCs1Sha256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }

        public SatSoapClient(Certificate certificate, TimeSpan httpTimeout, Logger logger)
        {
            var endpointUri = new Uri("https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/Autenticacion/Autenticacion.svc");
            SoapClient = new SoapClient(endpointUri, certificate, httpTimeout, SignAlgorithm.Sha1);
            Logger = logger;
            SoapClient.HttpRequestFinished += (sender, args) => HttpRequestFinished?.Invoke(this, args);
            SoapClient.XmlMessageSerialized += (sender, args) => XmlMessageSerialized?.Invoke(this, args);
        }

        public event EventHandler<HttpRequestFinishedEventArgs> HttpRequestFinished;

        public event EventHandler<XmlMessageSerializedEventArgs> XmlMessageSerialized;


        private SoapClient SoapClient { get; }

        private Logger Logger { get; }

        public async Task<XmlDocument> SendRevenueAsync(Autentica message)
        {
            return await SoapClient.SendAsync<Autentica, XmlDocument>(message, operation: "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/Autenticacion/Autenticacion.svc").ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
