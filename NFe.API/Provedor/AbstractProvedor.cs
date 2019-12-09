using NFe.API.Domain;
using NFe.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace NFe.API.Provedor
{
    public class AbstractProvedor : Notas
    {
        #region Variaveis

        const string classeNaoImplementar = "Função não implementada na classe filha. Implemente na classe que está sendo criada.";
        private EnumProvedor _nome = EnumProvedor.NaoDefinido;

        #endregion Variaveis

        #region Propriedades

        public virtual EnumProvedor Nome
        {
            get { return _nome; }
            set { _nome = value; }
        }

        #endregion Propriedades

        # region Métodos

        public virtual XmlDocument GeraXmlNota(NFSeNota nota)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }

        public virtual RetornoTransmitir ValidarNFSe(NFSeNota nota)
        {
            throw new NotImplementedException(classeNaoImplementar);
        }
        # endregion
    }
}
