using NFe.Components.Abstract;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Xml;

namespace NFe.Components.SALVADOR_BA.Salvador_BA.p
{
    public class SalvadorP : EmiteNFSeBase
    {
        public override string NameSpaces
        {
            get
            {
                return "http://tempuri.org";
            }
        }

        NFe.Components.br.com.salvadorba.consultasituacao.p.ConsultaSituacaoNfse service = new br.com.salvadorba.consultasituacao.p.ConsultaSituacaoNfse();

        public SalvadorP(TipoAmbiente tpAmb, string pastaRetorno, string proxyuser, string proxypass, string proxyserver, X509Certificate2 certificado)
            : base(tpAmb, pastaRetorno)
        {
            if (!String.IsNullOrEmpty(proxyuser))
            {
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(proxyuser, proxypass, proxyserver);
                System.Net.WebRequest.DefaultWebProxy.Credentials = credentials;

                service.Proxy = System.Net.WebRequest.DefaultWebProxy;
                service.Proxy.Credentials = new System.Net.NetworkCredential(proxyuser, proxypass);
                service.Credentials = new System.Net.NetworkCredential(proxyuser, proxypass);
            }
            service.ClientCertificates.Add(certificado);
        }
        
        public override void CancelarNfse(string file)
        {
            throw new System.NotImplementedException();
        }

        public override void ConsultarLoteRps(string file)
        {
            throw new System.NotImplementedException();
        }

        public override void ConsultarNfse(string file)
        {
            throw new System.NotImplementedException();
        }

        public override void ConsultarNfsePorRps(string file)
        {
            throw new System.NotImplementedException();
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new System.NotImplementedException();
        }

        public override void ConsultarSituacaoNFSe(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            string strXmlEnvio = doc.OuterXml;

            string strResult = service.ConsultarSituacaoNfse(strXmlEnvio);

            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedStaNFse).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedStaNFse).RetornoXML);            

        }

        public override void EmiteNF(string file)
        {
            throw new System.NotImplementedException();
        }
    }
}
