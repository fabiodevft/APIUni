using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NFe.Components.EL
{
    public static class ELGen
    {

        public static string GetValueXML(string file, string elementTag, string valueTag)
        {
            string value = "";
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNodeList nodes = doc.GetElementsByTagName(elementTag);
            XmlNode node = nodes[0];

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.Equals(valueTag))
                    {
                        value = n.InnerText;
                        break;
                    }
                }
            }

            return value;
        }

        public static string GetValueXML(XmlDocument doc, string elementTag, string valueTag)
        {
            string value = "";            
            XmlNodeList nodes = doc.GetElementsByTagName(elementTag);
            XmlNode node = nodes[0];

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.Equals(valueTag))
                    {
                        value = n.InnerText;
                        break;
                    }
                }
            }

            return value;
        }

        public static string XMLtoString(string arquivo)
        {
            XmlDocument docXML = new XmlDocument();
            docXML.Load(arquivo);
            return docXML.OuterXml;
        }

    }
}
