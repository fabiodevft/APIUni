using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace UniAPI.Domain
{
    [Serializable, Browsable(false)]
    public class XMLNFSe
    {
        #region Variaveis

        #endregion Variaveis

        #region Construtor
        public XMLNFSe()
        {
        }

        #endregion Construtor

        #region Propriedades

        [XmlRootAttribute("NFe", Namespace = "", IsNullable = false)]
        public class NFe
        {
            public string Prefeitura;
            public string InscricaoPrestador;
            public string IEPrestador;
            public CPFCNPJPrestador CPFCNPJPrestador;
            public ChaveNFe ChaveNFe;
            public string RazaoSocialPrestador;
            public EnderecoPrestador EnderecoPrestador;
            public string TelefonePrestador;
            public string EmailPrestador;
            public string StatusNFe;
            public string TributacaoNFe;
            public string Operacao;
            public string OpcaoSimples;
            public string ValorServicos;
            public string ValorBase;
            public string CodigoServico;
            public string AliquotaServicos;
            public string ValorINSS;
            public string ValorIR;
            public string ValorPIS;
            public string ValorCOFINS;
            public string ValorCSLL;
            public string ValorISS;
            public string ISSRetido;
            public string InscricaoTomador;
            public CPFCNPJTomador CPFCNPJTomador;
            public string IETomador;
            public string RazaoSocialTomador;
            public EnderecoTomador EnderecoTomador;
            public string TelefoneTomador;
            public string EmailTomador;
            public string Discriminacao;
        }

        public class CPFCNPJPrestador
        {
            public string CNPJ;
        }

        public class CPFCNPJTomador
        {
            public string CNPJ;
        }

        public class ChaveNFe
        {
            public string NumeroNFe;
            public string SerieNFe;
            public string CodigoVerificacao;
            public string DataEmissaoNFe;
        }

        public class EnderecoPrestador
        {
            public string Logradouro;
            public string NumeroEndereco;
            public string ComplementoEndereco;
            public string Bairro;
            public string Cidade;
            public string UF;
            public string CEP;
        }

        public class EnderecoTomador
        {
            public string Logradouro;
            public string NumeroEndereco;
            public string ComplementoEndereco;
            public string Bairro;
            public string Cidade;
            public string UF;
            public string CEP;
        }

        #endregion Propriedades
    }
}
