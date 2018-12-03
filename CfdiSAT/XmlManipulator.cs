using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace CfdiSAT
{
    public class XmlManipulator
    {
        public T Deserialize<T>(XmlElement xmlElement)
            where T : class
        {
            using (var reader = new StringReader(xmlElement.OuterXml))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                return xmlSerializer.Deserialize(reader) as T;
            }
        }

        public XmlElement Serialize<T>(T value, XmlSerializerNamespaces ns = null)
            where T : class
        {
            var xmlDocument = new XmlDocument();
            var navigator = xmlDocument.CreateNavigator();
            using (var writer = navigator.AppendChild())
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                if (ns != null)
                {
                    xmlSerializer.Serialize(writer, value, ns);
                }
                else
                {
                    xmlSerializer.Serialize(writer, value);
                }
            }
            return xmlDocument.DocumentElement;
        }
    }
}
