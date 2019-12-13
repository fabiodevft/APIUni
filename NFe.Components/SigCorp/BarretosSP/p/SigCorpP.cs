using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NFe.Components.Abstract;
using NFe.Components.br.com.sigiss.barretos.p;

namespace NFe.Components.SigCorp.BarretosSP.p
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

        #region construtores
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
            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                                            Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML);
        }

        public override void CancelarNfse(string file)
        {
            tcDadosCancelaNota oTcDadosCancela = SigCorpGen.ReadXML<tcDadosCancelaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            tcRetornoNota result = service.CancelarNota(oTcDadosCancela, out tcErros);
            string strResult = base.CreateXML(result, tcErros);
            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML);
        }

        public override void ConsultarLoteRps(string file)
        {

            tcDadosPrestador oTcDadosConsultaNota = SigCorpGen.ReadXML<tcDadosPrestador>(file);
            tcEstruturaDescricaoErros[] tcErros = null;

            string result = service.ConsultarNotaPrestador(oTcDadosConsultaNota, SigCorpGen.NumeroNota(file, "urn:ConsultarNotaPrestador"), out tcErros).ToString();
            string strResult = result;
            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                                            Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfse(string file)
        {

            tcDadosConsultaNota oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosConsultaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            string strResult = service.ConsultarNotaValida(oTcDadosPrestador, out tcErros).ToString();
            GerarRetorno(file, strResult, Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML,
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

            tcDadosPrestador oTcDadosConsultaNota = SigCorpGen.ReadXML<tcDadosPrestador>(xml);
            tcEstruturaDescricaoErros[] tcErros = null;

            string result = service.ConsultarNotaPrestador(oTcDadosConsultaNota, SigCorpGen.NumeroNota(xml, "urn:ConsultarNotaPrestador"), out tcErros).ToString();

            string strResult = result;

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarNfse(XmlDocument file)
        {

            tcDadosConsultaNota oTcDadosPrestador = SigCorpGen.ReadXML<tcDadosConsultaNota>(file);
            tcEstruturaDescricaoErros[] tcErros = null;
            string strResult = service.ConsultarNotaValida(oTcDadosPrestador, out tcErros).ToString();

            XmlDocument xmlRetorno = new XmlDocument();
            xmlRetorno.Load(strResult);

            return xmlRetorno;
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        #endregion
        
        #endregion
    }
}
