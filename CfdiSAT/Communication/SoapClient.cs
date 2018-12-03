using System;
using System.Security;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CfdiSAT.Dto;
using CfdiSAT.Events;

namespace CfdiSAT.Communication
{
    public class SoapClient
    {
        public SoapClient(Uri endpointUri, Certificate certificate, TimeSpan httpTimeout, SignAlgorithm signAlgorithm = SignAlgorithm.Sha256, Logger logger = null)
        {
            HttpClient = new SoapHttpClient(endpointUri, httpTimeout, logger);
            Certificate = certificate;
            SignAlgorithm = signAlgorithm;
            XmlManipulator = new XmlManipulator();
            Logger = logger;
            HttpClient.HttpRequestFinished += (sender, args) => HttpRequestFinished?.Invoke(this, args);
        }

        public event EventHandler<HttpRequestFinishedEventArgs> HttpRequestFinished;

        public event EventHandler<XmlMessageSerializedEventArgs> XmlMessageSerialized;

        private SoapHttpClient HttpClient { get; }

        private Certificate Certificate { get; }

        private SignAlgorithm SignAlgorithm { get; }

        private XmlManipulator XmlManipulator { get; }

        private Logger Logger { get; }

        public async Task<TOut> SendAsync<TIn, TOut>(TIn messageBodyObject, string operation)
            where TIn : class, new()
            where TOut : class, new()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "http://DescargaMasivaTerceros.gob.mx");

            var messageBodyXmlElement = XmlManipulator.Serialize(messageBodyObject, ns);
            var mesasgeBodyXmlString = messageBodyXmlElement.OuterXml;
            Logger?.Info("Created XML document from DTOs.", new { XmlString = mesasgeBodyXmlString });
            XmlMessageSerialized?.Invoke(this, new XmlMessageSerializedEventArgs(messageBodyXmlElement, "Autenticando"));

            var soapMessage = new SoapMessage(new SoapMessagePart(messageBodyXmlElement));
            var xmlDocument = Certificate == null ? soapMessage.GetXmlDocument() : soapMessage.GetSignedXmlDocument(Certificate, SignAlgorithm);

            var xml = xmlDocument.OuterXml;
            Logger?.Debug("Created signed XML.", new { SoapString = xml });

            var response = await HttpClient.SendAsync(xml, operation).ConfigureAwait(false);

            Logger?.Debug("Received RAW response from SAT servers.", new { HttpResponseBody = response });

            var soapBody = GetSoapBody(response);
            return XmlManipulator.Deserialize<TOut>(soapBody);
        }

        private XmlElement GetSoapBody(string soapXmlString)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(soapXmlString);
            //return xmlDocument.DocumentElement;
            var soapMessage = SoapMessage.FromSoapXml(xmlDocument);
            if (!soapMessage.VerifySignature())
            {
                throw new SecurityException("The SOAP message signature is not valid.");
            }
            return soapMessage.Body.XmlElement.FirstChild as XmlElement;
        }
    }
}
