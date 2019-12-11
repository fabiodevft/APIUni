using NFe.Components.Abstract;
using NFe.Components.br.com.fiorilli.mirassolandiasp.p;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace NFe.Components.Fiorilli.MirassolandiaSP.p
{
    public class FiorilliP : EmiteNFSeBase
    {
        private IssWebWS Service = new IssWebWS();
        private string UsuarioWs = "";
        private string SenhaWs = "";

        public override string NameSpaces
        {
            get
            {
                return "http://www.abrasf.org.br/nfse.xsd";
            }
        }

        #region construtores

        public FiorilliP(TipoAmbiente tpAmb, string pastaRetorno, string usuario, string senhaWs, string proxyuser, string proxypass, string proxyserver, X509Certificate2 certificado)
            : base(tpAmb, pastaRetorno)
        {
            ServicePointManager.Expect100Continue = false;
            UsuarioWs = usuario;
            SenhaWs = senhaWs;

            Service.ClientCertificates.Add(certificado);

            if (!String.IsNullOrEmpty(proxyuser))
            {
                NetworkCredential credentials = new NetworkCredential(proxyuser, proxypass, proxyserver);
                WebRequest.DefaultWebProxy.Credentials = credentials;

                Service.Proxy = WebRequest.DefaultWebProxy;
                Service.Proxy.Credentials = new NetworkCredential(proxyuser, proxypass);
                Service.Credentials = new NetworkCredential(proxyuser, proxypass);
            }
        }

        #endregion construtores

        #region Métodos

        public override void EmiteNF(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string strResult = string.Empty;
            switch (doc.DocumentElement.Name)
            {
                case "GerarNfseEnvio":
                    strResult = EnvioSincrono(file);
                    break;

                case "EnviarLoteRpsSincronoEnvio":
                    strResult = EnvioSincronoEmLote(file);
                    break;
            }

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                              Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML);
        }

        private string EnvioSincrono(string file)
        {
            GerarNfseEnvio envio = DeserializarObjeto<GerarNfseEnvio>(file);
            return SerializarObjeto(Service.gerarNfse(envio, UsuarioWs, SenhaWs));
        }

        private string EnvioSincronoEmLote(string file)
        {
            EnviarLoteRpsSincronoEnvio envio = DeserializarObjeto<EnviarLoteRpsSincronoEnvio>(file);
            return SerializarObjeto(Service.recepcionarLoteRpsSincrono(envio, UsuarioWs, SenhaWs));
        }

        public override void CancelarNfse(string file)
        {
            CancelarNfseEnvio envio = DeserializarObjeto<CancelarNfseEnvio>(file);
            string strResult = SerializarObjeto(Service.cancelarNfse(envio, UsuarioWs, SenhaWs));

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML);
        }

        public override void ConsultarLoteRps(string file)
        {
            ConsultarLoteRpsEnvio envio = DeserializarObjeto<ConsultarLoteRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.consultarLoteRps(envio, UsuarioWs, SenhaWs));

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfse(string file)
        {
            ConsultarNfseServicoPrestadoEnvio envio = DeserializarObjeto<ConsultarNfseServicoPrestadoEnvio>(file);
            string strResult = SerializarObjeto(Service.consultarNfseServicoPrestado(envio, UsuarioWs, SenhaWs));

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).RetornoXML);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            ConsultarNfseRpsEnvio envio = DeserializarObjeto<ConsultarNfseRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.consultarNfsePorRps(envio, UsuarioWs, SenhaWs));

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).RetornoXML);
        }

        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            string strResult = string.Empty;
            switch (xml.DocumentElement.Name)
            {
                case "GerarNfseEnvio":
                    strResult = EnvioSincrono(xml);
                    break;

                case "EnviarLoteRpsSincronoEnvio":
                    strResult = EnvioSincronoEmLote(xml);
                    break;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        private string EnvioSincrono(XmlDocument xml)
        {
            GerarNfseEnvio envio = DeserializarObjeto<GerarNfseEnvio>(xml);
            return SerializarObjeto(Service.gerarNfse(envio, UsuarioWs, SenhaWs));
        }

        private string EnvioSincronoEmLote(XmlDocument xml)
        {
            EnviarLoteRpsSincronoEnvio envio = DeserializarObjeto<EnviarLoteRpsSincronoEnvio>(xml);
            return SerializarObjeto(Service.recepcionarLoteRpsSincrono(envio, UsuarioWs, SenhaWs));
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            CancelarNfseEnvio envio = DeserializarObjeto<CancelarNfseEnvio>(xml);
            string strResult = SerializarObjeto(Service.cancelarNfse(envio, UsuarioWs, SenhaWs));

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            ConsultarLoteRpsEnvio envio = DeserializarObjeto<ConsultarLoteRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.consultarLoteRps(envio, UsuarioWs, SenhaWs));

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            ConsultarNfseServicoPrestadoEnvio envio = DeserializarObjeto<ConsultarNfseServicoPrestadoEnvio>(xml);
            string strResult = SerializarObjeto(Service.consultarNfseServicoPrestado(envio, UsuarioWs, SenhaWs));

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            ConsultarNfseRpsEnvio envio = DeserializarObjeto<ConsultarNfseRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.consultarNfsePorRps(envio, UsuarioWs, SenhaWs));

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        #endregion

        #endregion Métodos
    }
}