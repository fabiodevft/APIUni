using NFe.Components.Abstract;
using NFe.Components.br.com.sigiss.botucatu.h;
using System;
using System.Reflection;
using System.Text;
using System.Xml;

namespace NFe.Components.SigCorp.BotucatuSP.h
{
    public class SigCorpH : EmiteNFSeBase
    {
        private WebServiceSigISS service = new WebServiceSigISS();

        public override string NameSpaces
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        
        #region Construtores

        public SigCorpH(TipoAmbiente tpAmb, string pastaRetorno)
            : base(tpAmb, pastaRetorno)
        {
        }

        public SigCorpH(TipoAmbiente tpAmb)
           : base(tpAmb)
        {
        }

        #endregion Construtores

        #region Métodos

        public override void EmiteNF(string file)
        {
            tcDescricaoRps oTcDescricaoRps = SigCorpGen.ReadXML<tcDescricaoRps>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.GerarNota(oTcDescricaoRps, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML,
                Encoding.UTF8);
        }

        public override void CancelarNfse(string file)
        {
            tcDadosCancelaNota oTcDadosCancela = SigCorpGen.ReadXML<tcDadosCancelaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.CancelarNota(oTcDadosCancela, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarLoteRps(string file)
        {
            tcDadosConsultaNota oTcDadosConsultaNota = SigCorpGen.ReadXML<tcDadosConsultaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;

            tcRetornoNota result = service.ConsultarNotaValida(oTcDadosConsultaNota, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML,
                Encoding.UTF8);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfse(string file)
        {
            tcDadosPrestador oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosPrestador>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcDadosNota result = service.ConsultarNotaPrestador(oTcDadosPrestador, SigCorpGen.NumeroNota(file, "urn:ConsultarNotaPrestador"), out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).RetornoXML);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }


        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            tcDescricaoRps oTcDescricaoRps = SigCorpGen.ReadXML<tcDescricaoRps>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.GerarNota(oTcDescricaoRps, out tcErros);
            string strResult = base.CreateXML(result, tcErros);

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            tcDadosCancelaNota oTcDadosCancela = SigCorpGen.ReadXML<tcDadosCancelaNota>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.CancelarNota(oTcDadosCancela, out tcErros);
            string strResult = base.CreateXML(result, tcErros);

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            tcDadosConsultaNota oTcDadosConsultaNota = SigCorpGen.ReadXML<tcDadosConsultaNota>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;

            tcRetornoNota result = service.ConsultarNotaValida(oTcDadosConsultaNota, out tcErros);
            string strResult = base.CreateXML(result, tcErros);

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
            tcDadosPrestador oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosPrestador>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcDadosNota result = service.ConsultarNotaPrestador(oTcDadosPrestador, SigCorpGen.NumeroNota(file, "urn:ConsultarNotaPrestador"), out tcErros);
            string strResult = base.CreateXML(result, tcErros);

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }


        #endregion


        #endregion Métodos
    }
}