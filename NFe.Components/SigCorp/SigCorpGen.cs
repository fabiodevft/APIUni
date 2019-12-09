using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NFe.Components.SigCorp
{
    public static class SigCorpGen
    {
        public static T ReadXML<T>(string file)
           where T : new()
        {
            T result = new T();
            result = (T)ReadXML2(file, result, result.GetType().Name.Substring(2));
            return result;
        }

        public static T ReadXML<T>(XmlDocument doc)
            where T : new()
        {
            T result = new T();
            result = (T)ReadXML2(doc, result, result.GetType().Name.Substring(2));
            return result;
        }

        private static object ReadXML2(string file, object value, string tag)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNodeList nodes = doc.GetElementsByTagName(tag);
            XmlNode node = nodes[0];
            if (node == null)
                throw new Exception("Tag <" + tag + "> não encontrada");

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    SetProperrty(value, n.Name, n.InnerXml);
                }
            }
            return value;
        }

        private static object ReadXML2(XmlDocument doc, object value, string tag)
        {
            XmlNodeList nodes = doc.GetElementsByTagName(tag);
            XmlNode node = nodes[0];
            if (node == null)
                throw new Exception("Tag <" + tag + "> não encontrada");

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    SetProperrty(value, n.Name, n.InnerXml);
                }
            }
            return value;
        }

        public static int NumeroNota(string file, string tag)
        {
            int nNumeroNota = 0;
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNodeList nodes = doc.GetElementsByTagName(tag);
            XmlNode node = nodes[0];
            if (node == null)
                throw new Exception("Tag <" + tag + "> não encontrada");

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.Equals("Nota"))
                    {
                        nNumeroNota = Convert.ToInt32(n.InnerText);
                        break;
                    }
                }
            }
            return nNumeroNota;
        }

        public static int NumeroNota(XmlDocument doc, string tag)
        {
            int nNumeroNota = 0;

            XmlNodeList nodes = doc.GetElementsByTagName(tag);
            XmlNode node = nodes[0];
            if (node == null)
                throw new Exception("Tag <" + tag + "> não encontrada");

            foreach (XmlNode n in node)
            {
                if (n.NodeType == XmlNodeType.Element)
                {
                    if (n.Name.Equals("Nota"))
                    {
                        nNumeroNota = Convert.ToInt32(n.InnerText);
                        break;
                    }
                }
            }
            return nNumeroNota;
        }

        private static void SetProperrty(object result, string propertyName, object value)
        {
            PropertyInfo pi = result.GetType().GetProperty(propertyName);

            if (pi != null && !String.IsNullOrEmpty(value.ToString()))
            {
                value = Convert.ChangeType(value, pi.PropertyType);
                pi.SetValue(result, value, null);
            }
        }
    }
}
