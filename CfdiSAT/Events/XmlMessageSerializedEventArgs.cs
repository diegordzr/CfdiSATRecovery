using System;
using System.Xml;

namespace CfdiSAT.Events
{
    public class XmlMessageSerializedEventArgs : EventArgs
    {
        public XmlMessageSerializedEventArgs(XmlElement xmlElement, string billNumber)
        {
            XmlElement = xmlElement;
            BillNumber = billNumber;
        }

        public XmlElement XmlElement { get; }

        public string BillNumber { get; }
    }
}
