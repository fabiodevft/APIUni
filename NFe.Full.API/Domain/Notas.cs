using System;
using System.Collections.Generic;

namespace NFe.Full.API.Domain
{
    public class Notas: List<NFSeNota>
    {
        #region Variáveis

        private const string S001 = "S001";                              // Sucesso
        private const string E001 = "E001";                              // Erro

        #endregion

        #region Propriedades

        public Provedor.Provedor Provedor { get; set; }

        #endregion

        #region Métodos

        private new void Add(NFSeNota item)
        {
            if (item.Provedor == null)
                throw new Exception("Nota não possui Provedor.");

            item.GeraXmlNota();
            this.Add(item);
        }

        #endregion

        #region Retorno

        public class RetornoBase
        {
            public string code
            {
                get
                {
                    return (!string.IsNullOrEmpty(success) ? S001 : E001);
                }
            }
            public string error { get; set; }
            public string success { get; set; }
        }

        public class RetornoTransmitir : RetornoBase
        {
            public RetornoTransmitir()
            {
            }

            public RetornoTransmitir(string _error, string _success)
            {
                error = _error;
                success = _success;
            }

            public string chave { get; set; }
            public string dhRecbto { get; set; }
            public string nProt { get; set; }
            public string digVal { get; set; }
            public string cStat { get; set; }
            public string xMotivo { get; set; }
            public string numero { get; set; }
            public string xml { get; set; }
            public string xmlEvento { get; set; }
            public string LinkImpressao { get; set; }
            public long NumeroLote { get; set; }
            public string NumeroRPS { get; set; }
            public DateTime? DataEmissaoRPS { get; set; }
            public string CodigoRetornoPref { get; set; }
            public long CodigoEvento { get; set; }
            public string hash { get; set; }
            public string Danfe { get; set; }
        }

        public class RetornoObterPDF : RetornoBase
        {
            public string PDF { get; set; }
        }

        #endregion Retorno

        public string GerarChaveNFSe(int CodigoUF, DateTime DataEmissao, string emitenteCNPJ, string numeroNF, int modelo)
        {
            if (CodigoUF == 0)
                throw new ArgumentException("Código ibge da unidade federal não pode ser nulo");

            long numNota = 0;
            long.TryParse(numeroNF, out numNota);

            if (numNota == 0)
                throw new ArgumentException("Número da nota não pode ser nulo");

            return string.Concat(CodigoUF.ToString("D2"), DataEmissao.ToString("yyMM"), emitenteCNPJ, modelo.ToString("D2"), numNota.ToString("D15"));
        }

    }
}
