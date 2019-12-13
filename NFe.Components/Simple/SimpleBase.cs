using NFe.Components.Abstract;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace NFe.Components.Simple
{
    public abstract class SimpleBase : EmiteNFSeBase
    {
        #region locais/ protegidos

        int CodigoMun = 0;
        string ProxyUser = "";
        string ProxyPass = "";
        string ProxyServer = "";
        X509Certificate2 Certificado = null;
        EmiteNFSeBase simpleService;

        protected EmiteNFSeBase SimpleService
        {
            get
            {
                if (simpleService == null)
                {
                    //if (tpAmb == TipoAmbiente.taHomologacao)
                    //metropolisService = new LauroDeFreitasBA.h.MetropolisH(tpAmb, PastaRetorno, ProxyUser, ProxyPass, ProxyServer, Certificado);

                    //else
                    switch (CodigoMun)
                    {
                        case 4203204: //CAMBORIÚ-SC
                            simpleService = new CamboriuSC.p.SimpleP(tpAmb, PastaRetorno, ProxyUser, ProxyPass, ProxyServer, Certificado);
                            break;

                        default:
                            throw new Exceptions.ServicoInexistenteException();
                    }
                }
                return simpleService;
            }
        }
        #endregion

        #region Construtores
        public SimpleBase(TipoAmbiente tpAmb, string pastaRetorno, int codMun, string proxyserver, string proxyuser, string proxypass, X509Certificate2 certificado)
            : base(tpAmb, pastaRetorno)
        {
            CodigoMun = codMun;
            ProxyUser = proxyuser;
            ProxyPass = proxypass;
            ProxyServer = proxyserver;
            Certificado = certificado;
        }

        #endregion

        #region Métodos
        public override void EmiteNF(string file)
        {
            SimpleService.EmiteNF(file);
        }

        public override void CancelarNfse(string file)
        {
            SimpleService.CancelarNfse(file);
        }

        public override void ConsultarLoteRps(string file)
        {
            SimpleService.ConsultarLoteRps(file);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            SimpleService.ConsultarSituacaoLoteRps(file);
        }

        public override void ConsultarNfse(string file)
        {
            SimpleService.ConsultarNfse(file);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            SimpleService.ConsultarNfsePorRps(file);
        }


        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            return SimpleService.EmiteNF(xml);
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            return SimpleService.CancelarNfse(xml);
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            return SimpleService.ConsultarLoteRps(xml);
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            return SimpleService.ConsultarSituacaoLoteRps(xml);
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            return SimpleService.ConsultarNfse(xml);
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            return SimpleService.ConsultarNfsePorRps(xml);
        }

        #endregion


        #endregion

    }
}
