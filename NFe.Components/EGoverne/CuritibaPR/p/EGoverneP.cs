﻿using NFe.Components.br.gov.egoverne.isscuritiba.curitiba.p;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace NFe.Components.EGoverne.CuritibaPR.p
{
    public class EGoverneP : EGoverneSerialization
    {
        private WSNFSeV1001 service = new WSNFSeV1001();

        #region construtores

        public EGoverneP(TipoAmbiente tpAmb, string pastaRetorno, string usuarioProxy, string senhaProxy, string domainProxy, X509Certificate certificado)
            : base(tpAmb, pastaRetorno, usuarioProxy, senhaProxy, domainProxy)
        {
            service.Proxy = WebRequest.DefaultWebProxy;
            service.Proxy.Credentials = new NetworkCredential(usuarioProxy, senhaProxy);
            service.Credentials = new NetworkCredential(senhaProxy, senhaProxy);
            service.ClientCertificates.Add(certificado);
        }

        #endregion construtores

        #region Métodos

        public override void EmiteNF(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("RecepcionarLoteRps", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML,
                Encoding.UTF8);
        }

        public override void CancelarNfse(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("CancelarNfse", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarLoteRps(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("ConsultarLoteRps", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("ConsultarSituacaoLoteRps", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitLoteRps).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarNfse(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("ConsultarNfse", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string result = service.RecepcionarXml("ConsultarNfsePorRps", doc.InnerXml);
            GerarRetorno(file, result,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).RetornoXML,
                Encoding.UTF8);
        }
        
        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            string result = service.RecepcionarXml("RecepcionarLoteRps", xml.InnerXml);

            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            string result = service.RecepcionarXml("CancelarNfse", xml.InnerXml);
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            string result = service.RecepcionarXml("ConsultarLoteRps", xml.InnerXml);
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            string result = service.RecepcionarXml("ConsultarSituacaoLoteRps", xml.InnerXml);
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            string result = service.RecepcionarXml("ConsultarNfse", xml.InnerXml);
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            string result = service.RecepcionarXml("ConsultarNfsePorRps", xml.InnerXml);
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            return doc;
        }

        #endregion
        
        #endregion Métodos
    }
}