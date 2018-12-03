using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using CfdiSAT.Dto;

namespace CfdiSAT.Communication
{
    public enum SignAlgorithm
    {
        Sha1 = 0,
        Sha256 = 1
    }

    public class SoapSigner
    {
        public SoapSigner(Certificate certificate, SignAlgorithm signAlgorithm)
        {
            Certificate = certificate;
            SecurityToken = Convert.ToBase64String(certificate.X509Certificate2.GetRawCertData());
            SignatureMethod = GetSignatureMethodUri(signAlgorithm);
            DigestMethod = GetDigestMethod(signAlgorithm);
            RsaKey = certificate.X509Certificate2.GetRSAPrivateKey();
        }

        private string SecurityToken { get; }

        private string SignatureMethod { get; }

        private string DigestMethod { get; }

        private RSA RsaKey { get; }

        private Certificate Certificate { get; }

        public XmlDocument SignMessage(XmlDocument xmlDoc)
        {
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("u", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

            var soapHeaderNode = xmlDoc.DocumentElement?.SelectSingleNode("//s:Header", namespaceManager) as XmlElement;
            var bodyNode = xmlDoc.DocumentElement?.SelectSingleNode("//s:Body", namespaceManager) as XmlElement;

            if (bodyNode == null)
            {
                throw new Exception("No body tag found.");
            }

            var uuid = $"id-{Guid.NewGuid()}";
            var uuidSecurityToken = $"id-{Guid.NewGuid()}";

            var timestamp = xmlDoc.CreateElement("u", "Timestamp", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            var created = xmlDoc.CreateElement("u", "Created", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            var expires = xmlDoc.CreateElement("u", "Expires", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            timestamp.SetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", uuidSecurityToken);
            created.InnerText = DateTime.Now.ToString("yyyy-MM-ddTHH':'mm':'ss'.'fffZ");
            expires.InnerText = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-ddTHH':'mm':'ss'.'fffZ");
            timestamp.AppendChild(created);
            timestamp.AppendChild(expires);
            soapHeaderNode?.AppendChild(timestamp);

            var securityNode = xmlDoc.CreateElement(
                prefix: "o",
                localName: "Security",
                namespaceURI: "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"
            );
            securityNode.SetAttribute("mustUnderstand", "http://schemas.xmlsoap.org/soap/envelope/", "1");

            var binarySecurityTokenElement = xmlDoc.CreateElement("o", "BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            binarySecurityTokenElement.SetAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
            binarySecurityTokenElement.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            binarySecurityTokenElement.SetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", uuid);
            binarySecurityTokenElement.InnerText = SecurityToken;

            securityNode.AppendChild(binarySecurityTokenElement);
            soapHeaderNode?.AppendChild(securityNode);

            var signedXml = new SignedXmlWithId(xmlDoc);
            signedXml.SignedInfo.SignatureMethod = SignatureMethod;
            signedXml.SigningKey = RsaKey;
            
            var securityTokenRef = xmlDoc.CreateElement("o", "SecurityTokenReference", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            var oRef = xmlDoc.CreateElement("o", "Reference", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            oRef.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            oRef.SetAttribute("URI", $"#{uuid}");
            securityTokenRef.AppendChild(oRef);

            var keyInfo = new KeyInfo();
            var keyInfoData = new KeyInfoNode(securityTokenRef);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            var reference = new Reference
            {
                Uri = $"#{uuid}",
                DigestMethod = DigestMethod
            };

            reference.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();

            var signedElement = signedXml.GetXml();
            securityNode.AppendChild(signedElement);
            return xmlDoc;
        }

        private RSACryptoServiceProvider GetRsaKey(SignAlgorithm signAlgorithm, X509Certificate2 certificate)
        {
            if (signAlgorithm == SignAlgorithm.Sha1)
            {
                return certificate.PrivateKey as RSACryptoServiceProvider;
            }

            if (signAlgorithm == SignAlgorithm.Sha256)
            {
                var key = certificate.PrivateKey as RSACryptoServiceProvider;
                var cspKeyContainerInfo = new RSACryptoServiceProvider().CspKeyContainerInfo;
                var cspParameters = new CspParameters(cspKeyContainerInfo.ProviderType, cspKeyContainerInfo.ProviderName, key.CspKeyContainerInfo.KeyContainerName)
                {
                    Flags = Certificate.UseMachineKeyStore ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags
                };
                return new RSACryptoServiceProvider(cspParameters);
            }

            throw new InvalidEnumArgumentException($"Unsupported signing algorithm {signAlgorithm}.");
        }

        private string GetSignatureMethodUri(SignAlgorithm signAlgorithm)
        {
            if (signAlgorithm == SignAlgorithm.Sha1)
            {
                return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            }
            if (signAlgorithm == SignAlgorithm.Sha256)
            {
                return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            }

            throw new InvalidEnumArgumentException($"Unsupported signing algorithm {signAlgorithm}.");
        }

        private string GetDigestMethod(SignAlgorithm signAlgorithm)
        {
            if (signAlgorithm == SignAlgorithm.Sha1)
            {
                return "http://www.w3.org/2000/09/xmldsig#sha1";
            }
            if (signAlgorithm == SignAlgorithm.Sha256)
            {
                return "http://www.w3.org/2001/04/xmlenc#sha256";
            }

            throw new InvalidEnumArgumentException($"Unsupported signing algorithm {signAlgorithm}.");
        }
    }
}