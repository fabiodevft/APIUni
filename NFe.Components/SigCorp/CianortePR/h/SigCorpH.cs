using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NFe.Components.Abstract;
using NFe.Components.br.com.sigiss.cianorte.p;

namespace NFe.Components.SigCorp.CianortePR.h
{
    public class SigCorpH : EmiteNFSeBase
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
        public SigCorpH(TipoAmbiente tpAmb, string pastaRetorno)
            : base(tpAmb, pastaRetorno)
        {
        }

        public SigCorpH(TipoAmbiente tpAmb)
            : base(tpAmb)
        {
        }

        #endregion

        #region Métodos
        public override void EmiteNF(string file)
        {
           throw new Exceptions.ServicoInexistenteException();
        }

        public override void CancelarNfse(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfse(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override void ConsultarNfsePorRps(string file)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            throw new Exceptions.ServicoInexistenteException();
        }


        #endregion
        
        #endregion

    }
}
