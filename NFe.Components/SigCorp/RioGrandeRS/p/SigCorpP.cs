using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NFe.Components.Abstract;
using NFe.Components.br.com.sigiss.riogrande.p;

namespace NFe.Components.SigCorp.RioGrandeRS.p
{
    public class SigCorpP : EmiteNFSeBase
    {
        WebServiceSigISS service = new WebServiceSigISS();
        public override string NameSpaces
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region constrututores
        public SigCorpP(TipoAmbiente tpAmb, string pastaRetorno)
            : base(tpAmb, pastaRetorno)
        {

        }

        public SigCorpP(TipoAmbiente tpAmb)
           : base(tpAmb)
        {

        }

        #endregion

        #region Métodos
        public override void EmiteNF(string file)
        {
            tcDescricaoRps oTcDescricaoRps = SigCorpGen.ReadXML<tcDescricaoRps>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.GerarNota(oTcDescricaoRps, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,   Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML, 
                                            Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML);
        }

        public override void CancelarNfse(string file)
        {
            tcDadosPrestador oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosPrestador>(file);
            tcDescricaoCancelaNota oTcDadosCancela = SigCorpGen.ReadXML<tcDescricaoCancelaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.CancelarNota(oTcDadosPrestador, oTcDadosCancela, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                                          Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML);
        }

        public override void ConsultarLoteRps(string file)
        {
            tcDadosConsultaNota oTcDadosConsultaNota = SigCorpGen.ReadXML<tcDadosConsultaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;

            tcRetornoNota result = service.ConsultarNotaValida(oTcDadosConsultaNota, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,   Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML, 
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfse(string file)
        {
            tcDadosPrestador oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosPrestador>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcDadosNota result = service.ConsultarNotaPrestador(oTcDadosPrestador, SigCorpGen.NumeroNota(file, "ConsultarNotaPrestador"), "", out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult,   Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML, 
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).RetornoXML);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }


        #region API

        public override XmlDocument EmiteNF(XmlDocument file)
        {
            tcDescricaoRps oTcDescricaoRps = SigCorpGen.ReadXML<tcDescricaoRps>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.GerarNota(oTcDescricaoRps, out tcErros);

            string strResult = base.CreateXML(result, tcErros);

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            tcDadosPrestador oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosPrestador>(xml);
            tcDescricaoCancelaNota oTcDadosCancela = SigCorpGen.ReadXML<tcDescricaoCancelaNota>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.CancelarNota(oTcDadosPrestador, oTcDadosCancela, out tcErros);

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
            tcDadosNota result = service.ConsultarNotaPrestador(oTcDadosPrestador, SigCorpGen.NumeroNota(xml, "ConsultarNotaPrestador"), "", out tcErros);

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

        #endregion
    }
}
