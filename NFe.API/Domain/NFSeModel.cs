using NFe.API.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NFe.API.Domain
{
    [Serializable, Browsable(false)]
    public class NFSeModel
    {
        #region Variaveis

        #endregion Variaveis

        #region Construtor

        public NFSeModel()
        {
        }

        #endregion Construtor

        #region Propriedades

        public class TIdentificacaoRps
        {
            public TIdentificacaoRps(EnumAmbiente ambiente)
            {
                FAmbiente = ambiente;
            }

            public string FNumero { get; set; }
            public string FSerie { get; set; }
            public int FTipo { get; set; }
            public EnumAmbiente FAmbiente { get; }
        }

        public class TRpsSubstituido
        {
            public string FNumero { get; set; }
            public string FSerie { get; set; }
            public string FTipo { get; set; }
        }

        public class TValores
        {
            public decimal FValorServicos { get; set; }
            public decimal FValorDeducoes { get; set; }
            public decimal FValorPis { get; set; }
            public decimal FValorCofins { get; set; }
            public decimal FValorInss { get; set; }
            public decimal FValorIr { get; set; }
            public decimal FValorCsll { get; set; }
            public int FIssRetido { get; set; }
            public decimal FValorIss { get; set; }
            public decimal FOutrasRetencoes { get; set; }
            public decimal FBaseCalculo { get; set; }
            public decimal FAliquota { get; set; }
            public decimal FAliquotaPis { get; set; }
            public decimal FAliquotaCofins { get; set; }
            public decimal FAliquotaInss { get; set; }
            public decimal FAliquotaIr { get; set; }
            public decimal FAliquotaCsll { get; set; }
            public decimal FValorLiquidoNfse { get; set; }
            public decimal FValorIssRetido { get; set; }
            public decimal FDescontoCondicionado { get; set; }
            public decimal FDescontoIncondicionado { get; set; }
            public string FJustificativaDeducao { get; set; }
            public decimal FvalorOutrasRetencoes { get; set; }
            public string FDescricaoOutrasRetencoes { get; set; }
        }

        public class TItemServico
        {
            public string FDescricao { get; set; }
            public decimal FQuantidade { get; set; }
            public decimal FValorUnitario { get; set; }
            public decimal FValorTotal { get; set; }
            public decimal FAliquota { get; set; }
            public decimal FValorIss { get; set; }
            public decimal FBaseCalculo { get; set; }
            public decimal FValorDeducoes { get; set; }
            public decimal FValorServicos { get; set; }
            public decimal FDescontoCondicionado { get; set; }
            public decimal FDescontoIncondicionado { get; set; }
            public string FDiscriminacao { get; set; }
            public decimal FValorCsll { get; set; }
            public decimal FValorPis { get; set; }
            public decimal FValorCofins { get; set; }
            public decimal FValorInss { get; set; }
            public decimal FValorIr { get; set; }
            public string FCodigo { get; set; }
            public string FCodServ { get; set; }
            public string _FCodServDesc { get; set; }
            public string FCodLCServ { get; set; }
            public string FUnidade { get; set; }
            public decimal FAlicotaISSST { get; set; }
            public decimal FValorISSST { get; set; }
        }

        public class TServico
        {
            public TValores FValores { get; set; }
            public string FItemListaServico { get; set; }
            public string FCodigoCnae { get; set; }
            public string FCodigoTributacaoMunicipio { get; set; }
            public string FDiscriminacao { get; set; }
            public string FCodigoMunicipio { get; set; }
            public int FCodigoPais { get; set; }
            public int FExigibilidadeISS { get; set; }
            public string FMunicipioIncidencia { get; set; }
            public long _FMunicipioIncidenciaSedetec { get; set; }
            public string FxItemListaServico { get; set; }
            public List<TItemServico> TItemServico { get; set; }
            public int FResponsavelRetencao { get; set; }
            public string FDescricao { get; set; }
            public string FOperacao { get; set; }
            public string FTributacao { get; set; }
            public decimal FValorCargaTributaria { get; set; }
            public decimal FPercentualCargaTributaria { get; set; }
            public string FFonteCargaTributaria { get; set; }

        }

        public class TIdentificacaoTomador
        {
            public string FCpfCnpj { get; set; }
            public string FInscricaoMunicipal { get; set; }
            public string FInscricaoEstadual { get; set; }
            public string FPessoa { get; set; }
        }

        public class TEndereco
        {
            public string FEndereco { get; set; }
            public string FNumero { get; set; }
            public string FComplemento { get; set; }
            public string FBairro { get; set; }
            public string FCodigoMunicipio { get; set; }
            public long _FCodigoMunicipioSedetec { get; set; }
            public string FUF { get; set; }
            public string FCEP { get; set; }
            public string FxMunicipio { get; set; }
            public string FSIAFI { get; set; }
        }

        public class TContato
        {
            public string FDDD { get; set; }
            public string FFone { get; set; }
            public string FEmail { get; set; }
        }

        public class TTomador
        {
            public TIdentificacaoTomador TIdentificacaoTomador { get; set; }
            public string FRazaoSocial { get; set; }
            public string FNomeFantasia { get; set; }
            public TEndereco TEndereco { get; set; }
            public TContato TContato { get; set; }
        }

        public class TIntermediarioServico
        {
            public string FRazaoSocial { get; set; }
            public string FCpfCnpj { get; set; }
            public string FInscricaoMunicipal { get; set; }
            public string FIssRetido { get; set; }
            public string FEMail { get; set; }
        }

        public class TConstrucaoCivil
        {
            public string FCodigoObra { get; set; }
            public string FArt { get; set; }
            public string FLogradouroObra { get; set; }
            public string FComplementoObra { get; set; }
            public string FNumeroObra { get; set; }
            public string FBairroObra { get; set; }
            public string FCEPObra { get; set; }
            public string FCodigoMunicipioObra { get; set; }
            public string FUFObra { get; set; }
            public int FCodigoPaisObra { get; set; }
            public string FxPaisObra { get; set; }
            public string FnCei { get; set; }
            public string FnProj { get; set; }
            public string FnMatri { get; set; }
        }
        public class TCondicaoPagamento
        {
            public string FCondicao { get; set; }
            public int FQtdParcela { get; set; }
            public List<TParcelas> FParcelas { get; set; }
        }


        public class TParcelas
        {
            public int FParcela { get; set; }
            public DateTime DataVencimento { get; set; }
            public string FDataVencimento { get { return DataVencimento.ToString("dd/MM/yyyy"); } }
            public decimal FValor { get; set; }
        }

        public class TPrestador
        {
            public string FRazaoSocial { get; set; }
            public string FNomeFantasia { get; set; }
            public string FCCM { get; set; }
            public string FCnpj { get; set; }
            public string FInscricaoMunicipal { get; set; }
            public string FInscricaoEstadual { get; set; }
            public TIdentificacaoPrestador FIdentificacaoPrestador { get; set; }
            public TEndereco TEndereco { get; set; }
            public TContato TContato { get; set; }
        }

        public class TIdentificacaoPrestador
        {
            public long FId { get; set; }
            public int FEmitIBGEUF { get; set; }
            public string FSenha { get; set; }
            public string FFraseSecreta { get; set; }
            public string _FUsuario { get; set; }
        }

        public class TOrgaoGerador
        {
            public string FCodigoMunicipio { get; set; }
            public string FUf { get; set; }
        }

        public class TValoresNfse
        {
            public int FBaseCalculo { get; set; }
            public int FAliquota { get; set; }
            public int FValorIss { get; set; }
            public int FValorLiquidoNfse { get; set; }
        }

        public class TIdentificacaoNfse
        {
            public string FNumero { get; set; }
            public string FCnpj { get; set; }
            public string FInscricaoMunicipal { get; set; }
            public string FCodigoMunicipio { get; set; }
        }

        public class TPedido
        {
            public string FID { get; set; }
            public TIdentificacaoNfse FIdentificacaoNfse { get; set; }
            public string FCodigoCancelamento { get; set; }
        }

        public class TGerador3
        {
            public string FArquivoFormatoXML { get; set; }
            public string FArquivoFormatoTXT { get; set; }
            public string FTagNivel { get; set; }
            public string FIDNivel { get; set; }
            public string FPrefixo { get; set; }
            public string FIgnorarTagNivel { get; set; }
            public string FIgnorarTagIdentacao { get; set; }
        }
        public class TSignature3
        {
            public TGerador3 FGerador { get; set; }
            public string FURI { get; set; }
            public string FDigestValue { get; set; }
            public string FSignatureValue { get; set; }
            public string FX509Certificate { get; set; }
        }

        public class TNfseCancelamento
        {
            public string FID { get; set; }
            public TPedido FPedido { get; set; }
            public int FDataHora { get; set; }
            public TSignature3 FSignature { get; set; }
        }

        public class TTransportadora
        {
            public string FxNomeTrans { get; set; }
            public string FxCpfCnpjTrans { get; set; }
            public string FxInscEstTrans { get; set; }
            public string FxPlacaTrans { get; set; }
            public string FxEndTrans { get; set; }
            public int FcMunTrans { get; set; }
            public string FxMunTrans { get; set; }
            public string FxUFTrans { get; set; }
            public int FcPaisTrans { get; set; }
            public string FxPaisTrans { get; set; }
            public string FvTipoFreteTrans { get; set; }
        }

        public class Tide
        {
            private DateTime dataEmissao;
            private DateTime dataEmissaoRps;

            public Int64 FNumeroLote { get; set; }
            public string FnProtocolo { get; set; }
            public TIdentificacaoRps FIdentificacaoRps { get; set; }
            public DateTime DataEmissao
            {
                get { return dataEmissao; }
                set { dataEmissao = value; }
            }
            public string FDataEmissao { get { return dataEmissao.ToString("dd/MM/yyyy"); } }
            public DateTime DataEmissaoRps
            {
                get { return dataEmissaoRps; }
                set { dataEmissaoRps = value; }
            }
            public string FDataEmissaoRps { get { return dataEmissaoRps.ToString("dd/MM/yyyy"); } }
            public int FNaturezaOperacao { get; set; }
            public int FRegimeEspecialTributacao { get; set; }
            public int FOptanteSimplesNacional { get; set; }
            public int FIncentivadorCultural { get; set; }
            public EnumNFSeRPSStatus FStatus { get; set; }
            public string FOutrasInformacoes { get; set; }
            public string _FMsgGeradaPeloSistema { get; set; }
            public string _FMsgComplementares { get; set; }
        }

        public class TDFe
        {
            public Tide Tide { get; set; }
            public TPrestador TPrestador { get; set; }
            public TTomador TTomador { get; set; }
            public TServico TServico { get; set; }
            public TCondicaoPagamento TCondicaoPagamento { get; set; }
        }

        public class TArquivo
        {
            public string Conteudo { get; set; }
            public int Tamanho { get; set; }
            public string Nome { get; set; }
        }

        public TDFe Doc { get; set; }

        #endregion Propriedades
    }

}
