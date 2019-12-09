using NFe.Components.Abstract;
using System.Security.Cryptography.X509Certificates;

namespace NFe.Components.VersaTecnologia
{
    public abstract class VersaTecnologiaBase : EmiteNFSeBase
    {
        #region Locais/Protegidos

        private int CodigoMun = 0;
        private string UsuarioProxy = "";
        private string SenhaProxy = "";
        private string DomainProxy = "";
        private X509Certificate Certificado = null;
        private EmiteNFSeBase versaService;

        protected EmiteNFSeBase VersaTecnologiaService
        {
            get
            {
                if (versaService == null)
                {
                    switch (CodigoMun)
                    {
                        case 3169901: //Ubá - MG
                            versaService = tpAmb == TipoAmbiente.taHomologacao ?
                                new UbaMG.h.VersaTecnologiaH(tpAmb, PastaRetorno, UsuarioProxy, SenhaProxy, DomainProxy, Certificado) as EmiteNFSeBase :
                                    new UbaMG.p.VersaTecnologiaP(tpAmb, PastaRetorno, UsuarioProxy, SenhaProxy, DomainProxy, Certificado) as EmiteNFSeBase;

                            break;



                        default:
                            throw new Exceptions.ServicoInexistenteException();
                    }
                }
                return versaService;
            }
        }

        #endregion Locais/Protegidos

        #region Construtores

        public VersaTecnologiaBase(TipoAmbiente tpAmb, string pastaRetorno, int codMun, string usuarioProxy, string senhaProxy, string domainProxy, X509Certificate certificado)
            : base(tpAmb, pastaRetorno)
        {
            CodigoMun = codMun;
            UsuarioProxy = usuarioProxy;
            SenhaProxy = senhaProxy;
            DomainProxy = domainProxy;
            Certificado = certificado;
        }

        #endregion Construtores

        #region Métodos

        public override void EmiteNF(string file)
        {
            VersaTecnologiaService.EmiteNF(file);
        }

        public override void CancelarNfse(string file)
        {
            VersaTecnologiaService.CancelarNfse(file);
        }

        public override void ConsultarLoteRps(string file)
        {
            VersaTecnologiaService.ConsultarLoteRps(file);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            VersaTecnologiaService.ConsultarSituacaoLoteRps(file);
        }

        public override void ConsultarNfse(string file)
        {
            VersaTecnologiaService.ConsultarNfse(file);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            VersaTecnologiaService.ConsultarNfsePorRps(file);
        }

        #endregion Métodos

    }

}
