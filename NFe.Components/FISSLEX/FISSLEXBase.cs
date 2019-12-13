using NFe.Components.Abstract;
using System.Xml;

namespace NFe.Components.FISSLEX
{
    public abstract class FISSLEXBase : EmiteNFSeBase
    {
        #region locais/ protegidos
        int CodigoMun = 0;
        string Usuario = "";
        string SenhaWs = "";
        string ProxyUser = "";
        string ProxyPass = "";
        string ProxyServer = "";
        EmiteNFSeBase fisslexService;

        protected EmiteNFSeBase FISSLEXService
        {
            get
            {
                if (fisslexService == null)
                {
                    if (tpAmb == TipoAmbiente.taHomologacao)
                        fisslexService = new h.FISSLEXH(tpAmb, PastaRetorno, Usuario, SenhaWs, ProxyUser, ProxyPass, ProxyServer);
                    else
                        switch (CodigoMun)
                        {
                            case 5105259: //Lucas do Rio Verde-MT
                                fisslexService = new LucasDoRioVerdeMT.p.FISSLEXP(tpAmb, PastaRetorno, Usuario, SenhaWs, ProxyUser, ProxyPass, ProxyServer);
                                break;

                            default:
                                throw new Exceptions.ServicoInexistenteException();
                        }
                }
                return fisslexService;
            }
        }
        #endregion

        #region Construtores
        public FISSLEXBase(TipoAmbiente tpAmb, string pastaRetorno, int codMun, string usuario, string senhaWs, string proxyuser, string proxypass, string proxyserver)
            : base(tpAmb, pastaRetorno)
        {
            CodigoMun = codMun;
            Usuario = usuario;
            SenhaWs = senhaWs;
            ProxyUser = proxyuser;
            ProxyPass = proxypass;
            ProxyServer = proxyserver;
        }
        #endregion

        #region Métodos
        public override void EmiteNF(string file)
        {
            FISSLEXService.EmiteNF(file);
        }

        public override void CancelarNfse(string file)
        {
            FISSLEXService.CancelarNfse(file);
        }

        public override void ConsultarLoteRps(string file)
        {
            FISSLEXService.ConsultarLoteRps(file);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            FISSLEXService.ConsultarSituacaoLoteRps(file);
        }

        public override void ConsultarNfse(string file)
        {
            FISSLEXService.ConsultarNfse(file);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            FISSLEXService.ConsultarNfsePorRps(file);
        }

        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            return FISSLEXService.EmiteNF(xml);
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            return FISSLEXService.CancelarNfse(xml);
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            return FISSLEXService.ConsultarLoteRps(xml);
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            return FISSLEXService.ConsultarSituacaoLoteRps(xml);
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            return FISSLEXService.ConsultarNfse(xml);
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            return FISSLEXService.ConsultarNfsePorRps(xml);
        }

        #endregion

        #endregion
    }
}
