using System.Xml;

namespace CfdiSAT.Communication
{
    public class SoapMessagePart
    {
        public SoapMessagePart(XmlElement xmlElement)
        {
            XmlElement = xmlElement;
        }

        public XmlElement XmlElement { get; }
    }
}
