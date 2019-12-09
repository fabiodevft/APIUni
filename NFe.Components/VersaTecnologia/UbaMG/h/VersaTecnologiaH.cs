using NFe.Components.Abstract;
using NFe.Components.br.com.versatecnologia.homolog.uba.h;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace NFe.Components.VersaTecnologia.UbaMG.h
{
    public class VersaTecnologiaH : EmiteNFSeBase
    {
        /// <summary>
        /// Objeto de conexão com o Webservice
        /// </summary>
        private NfseWSService Service = new NfseWSService();

        /// <summary>
        /// Cabeçalho do soap
        /// </summary>
        private string CabecMsg = "<nfseCabecMsg><?xml version=\"2.01\" encoding=\"UTF-8\"?><cabecalho xmlns=\"http://homologacaouba.versatecnologia.com.br/schema/nfse_v201.xsd\" versao=\"2.01\"><versaoDados>2.01</versaoDados></cabecalho></nfseCabecMsg><nfseDadosMsg>";


        public override string NameSpaces
        {
            get
            {
                return "http://www.abrasf.org.br/nfse.xsd";
            }
        }

        #region Construtores

        public VersaTecnologiaH(TipoAmbiente tpAmb, string pastaRetorno, string proxyuser, string proxypass, string proxyserver, X509Certificate certificado)
            : base(tpAmb, pastaRetorno)
        {
            if (!String.IsNullOrEmpty(proxyuser))
            {
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(proxyuser, proxypass, proxyserver);
                System.Net.WebRequest.DefaultWebProxy.Credentials = credentials;

                Service.Proxy = WebRequest.DefaultWebProxy;
                Service.Proxy.Credentials = new NetworkCredential(proxyuser, proxypass);
                Service.Credentials = new NetworkCredential(proxyuser, proxypass);
            }
            Service.ClientCertificates.Add(certificado);
        }

        #endregion

        #region Métodos

        public override void EmiteNF(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);

            string result = string.Empty;

            switch (xml.DocumentElement.Name)
            {
                case "EnviarLoteRpsEnvio":
                    result = Service.RecepcionarLoteRps(CabecMsg, xml.InnerXml);
                    break;

                case "GerarNfseEnvio":
                    result = Service.GerarNfse(CabecMsg, xml.InnerXml);                    
                    break;
            }                                            

            GerarRetorno(file, result, Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML);

        }

        public override void CancelarNfse(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);

            string result = string.Empty;

            result = Service.CancelarNfse(CabecMsg, xml.InnerXml);

            GerarRetorno(file, result, Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML);
        }

        public override void ConsultarLoteRps(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);

            string result = string.Empty;

            result = Service.ConsultarLoteRps(CabecMsg, xml.OuterXml);

            GerarRetorno(file, result, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new NotImplementedException();
        }

        public override void ConsultarNfse(string file)
        {
            throw new NotImplementedException();
        }

        public override void ConsultarNfsePorRps(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);

            string result = string.Empty;

            result = Service.ConsultarNfsePorRps(CabecMsg, xml.InnerXml);

            GerarRetorno(file, result, Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).RetornoXML);
        }


        #endregion

    }

}
