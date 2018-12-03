using System;
using System.Threading.Tasks;
using CfdiSAT.Communication;
using CfdiSAT.Dto;
using CfdiSAT.Dto.Wsdl;
using CfdiSAT.Events;

namespace CfdiSAT
{
    public class CfdiRecoveryClient
    {
        public CfdiRecoveryClient(Certificate certificate, TimeSpan? httpTimeout = null, Logger logger = null)
        {
            var effectiveTimeout = httpTimeout ?? TimeSpan.FromSeconds(2);
            SatSoapClient = new SatSoapClient(certificate, effectiveTimeout, logger);
            Logger = logger;
            SatSoapClient.HttpRequestFinished += (sender, args) => HttpRequestFinished?.Invoke(this, args);
            SatSoapClient.XmlMessageSerialized += (sender, args) => XmlMessageSerialized?.Invoke(this, args);
        }

        public event EventHandler<HttpRequestFinishedEventArgs> HttpRequestFinished;

        public event EventHandler<XmlMessageSerializedEventArgs> XmlMessageSerialized;

        private SatSoapClient SatSoapClient { get; }

        private Logger Logger { get; }

        public async Task<object> Autentica()
        {
            var authentica = new Autentica();
            var result = await SatSoapClient.SendRevenueAsync(authentica).ConfigureAwait(continueOnCapturedContext: false);
            Logger?.Debug("Result received and successfully deserialized from XML DTOs.", result);
            return result;
        }
    }
}
