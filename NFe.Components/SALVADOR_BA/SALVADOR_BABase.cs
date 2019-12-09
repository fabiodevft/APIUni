using NFe.Components.Abstract;
using System.Security.Cryptography.X509Certificates;

namespace NFe.Components.SALVADOR_BA
{
    public class SALVADOR_BABase : EmiteNFSeBase
    {
        public override string NameSpaces => throw new System.NotImplementedException();

        #region locais/ protegidos

        int CodigoMun = 0;
        string ProxyUser = "";
        string ProxyPass = "";
        string ProxyServer = "";
        X509Certificate2 Certificado = null;
        EmiteNFSeBase service;

        protected EmiteNFSeBase SALVADOR_BAService 
        {
            get
            {

                if (service == null)
                {                    
                    service = new Salvador_BA.p.SalvadorP(tpAmb, PastaRetorno, ProxyUser, ProxyPass, ProxyServer, Certificado);
                }
                return service;
            }
        }

        #endregion

        #region Construtores
        public SALVADOR_BABase(TipoAmbiente tpAmb, string pastaRetorno, int codMun, string proxyserver, string proxyuser, string proxypass, X509Certificate2 certificado)
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
            SALVADOR_BAService.EmiteNF(file);
        }

        public override void CancelarNfse(string file)
        {
            SALVADOR_BAService.CancelarNfse(file);
        }

        public override void ConsultarLoteRps(string file)
        {
            SALVADOR_BAService.ConsultarLoteRps(file);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            SALVADOR_BAService.ConsultarSituacaoLoteRps(file);
        }

        public override void ConsultarNfse(string file)
        {
            SALVADOR_BAService.ConsultarNfse(file);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            SALVADOR_BAService.ConsultarNfsePorRps(file);
        }        
    
        public void ConsultarSituacaoNFSe(string file)
        {
            SALVADOR_BAService.ConsultarSituacaoNFSe(file);
        }

        #endregion

    }
}
