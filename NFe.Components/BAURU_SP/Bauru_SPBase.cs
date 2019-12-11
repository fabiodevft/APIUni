using NFe.Components.Abstract;
using System.Xml;

namespace NFe.Components.BAURU_SP
{
    public abstract class Bauru_SPBase : EmiteNFSeBase
    {
        #region locais/ protegidos
        EmiteNFSeBase bauru_SPService;
        protected EmiteNFSeBase Bauru_SPService
        {
            get
            {
                if (bauru_SPService == null)
                {
                    if (tpAmb == TipoAmbiente.taHomologacao)
                        bauru_SPService = new BauruSP.h.BAURU_SPH(tpAmb, PastaRetorno);
                    else
                        bauru_SPService = new BauruSP.p.BAURU_SPP(tpAmb, PastaRetorno);
                }
                return bauru_SPService;
            }
        }
        #endregion

        #region Construtores
        public Bauru_SPBase(TipoAmbiente tpAmb, string pastaRetorno, int codMun)
            : base(tpAmb, pastaRetorno)
        {
        }
        #endregion

        #region Métodos
        public override void EmiteNF(string file)
        {
            Bauru_SPService.EmiteNF(file);
        }

        public override void CancelarNfse(string file)
        {
            Bauru_SPService.CancelarNfse(file);
        }

        public override void ConsultarLoteRps(string file)
        {
            Bauru_SPService.ConsultarLoteRps(file);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            Bauru_SPService.ConsultarSituacaoLoteRps(file);
        }

        public override void ConsultarNfse(string file)
        {
            Bauru_SPService.ConsultarNfse(file);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            Bauru_SPService.ConsultarNfsePorRps(file);
        }

        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            return Bauru_SPService.EmiteNF(xml);
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            return Bauru_SPService.CancelarNfse(xml);
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            return Bauru_SPService.ConsultarLoteRps(xml);
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            return Bauru_SPService.ConsultarSituacaoLoteRps(xml);
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            return Bauru_SPService.ConsultarNfse(xml);
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            return Bauru_SPService.ConsultarNfsePorRps(xml);
        }

        #endregion

        #endregion


    }
}
