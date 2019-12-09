using NFe.API.Enum;
using NFe.API.Provedor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static NFe.API.Domain.NFSeModel;
using static NFe.API.Domain.Notas;
using static NFe.API.Domain.XMLNFSe;
using static NFe.API.Util.Extensions;

namespace NFe.API.Domain
{
    [Serializable, Browsable(false)]
    public class NFSeNota
    {
        #region Variaveis

        private IProvedor _provedor;
        private ComandoTransmitir _documento;

        #endregion

        #region Construtor
        public NFSeNota(EnumProvedor provedor)
        {
            _provedor = new Provedor.Provedor(provedor);
        }

        #endregion Construtor

        #region Properties

        public IProvedor Provedor
        {
            get { return this._provedor; }
        }

        public ComandoTransmitir Documento
        {
            get { return this._documento; }
            set { this._documento = value; }
        }

        #endregion Properties


        #region Comando

        public class ComandoTransmitir
        {
            public string FnTerminal { get; set; }
            public int FCodigoCancelamento { get; set; }
            public TDFe TDFe { get; set; }
        }

        public class ComandoAcessar
        {
            public ComandoAcessar() { }

            public ComandoAcessar(string terminal, string chaveAcesso, string xml)
            {
                FnTerminal = terminal;
                FChaveAcesso = chaveAcesso;
                FXml = xml;
            }

            public string FnTerminal { get; set; }
            public string FChaveAcesso { get; set; }
            public string FXml { get; set; }
            //public TServico TServico { get; set; }
            public List<TItemServico> TItemServico { get; set; }
            public int FNaturezaOperacao { get; set; }
            public int FRegimeEspecialTributacao { get; set; }
            public string FOutrasInformacoes { get; set; }
            public TPrestador TPrestador { get; set; }
            public TTomador TTomador { get; set; }
        }

        public class ComandoCancelar
        {
            public string FnTerminal { get; set; }
            public string FChaveAcesso { get; set; }
            public int FCodigoCancelamento { get; set; }
            public string FXml { get; set; }
        }

        public class ComandoConsultarNFSeporRps
        {
            public string FnTerminal { get; set; }
            public TDFe TDFe { get; set; }
        }

        public class ComandoConfigurar
        {
            public string FnTerminal { get; set; }
            public string FNumCertificado { get; set; }
            public EnumAmbiente FAmbiente { get; set; }

            public string FPrefNome { get; set; }
            public string FPrefEndereco { get; set; }
            public string FPrefEndNumero { get; set; }
            public string FPrefBairro { get; set; }
            public string FPrefCep { get; set; }
            public string FPrefCidade { get; set; }
            public string FPrefUF { get; set; }
            public string FPrefFone { get; set; }
            public string FPrefEmail { get; set; }
            public TArquivo FPrefLogo { get; set; }
            public string FPrefShWebServices { get; set; }
            public string FPrefUsWebServices { get; set; }

            public string FEmitIBGEMunicipio { get; set; }
            public string FEmitRazaoSocial { get; set; }
            public string FEmitCNPJ { get; set; }
            public string FEmitIE { get; set; }
            public string FEmitIM { get; set; }
            public int FEmitIBGEUF { get; set; }
            public TArquivo FLogo { get; set; }
        }

        #endregion Comando

        public XmlDocument GeraXmlNota()
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GeraXmlNota(this);
        }

        public void TransmitirNota(//ServidorUniNFe Servidor, 
            XmlDocument xml, string arqret, string arqerr)
        {
            //if (File.Exists(arqret))
            //{
            //    var ret = LerRetorno(Servidor, ServidorUniNFe.EnumOperacao.Envio);
            //    if (ret.code == "S001")
            //    {
            //        return;
            //    }
            //    else
            //    {
            //        File.Delete(arqret);
            //    }
            //}

            //if (File.Exists(arqerr))
            //    File.Delete(arqerr);

            //xml.Save(Servidor.ArquivoEnvio(Documento.TDFe.Tide.FNumeroLote.ToString()));
        }

        public XmlDocument GerarXmlConsulta(string numeroNFSe)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlConsulta(this, numeroNFSe);
        }

        public XmlDocument GerarXmlConsulta(string numeroNFSe, DateTime emissao)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlConsulta(this, numeroNFSe, emissao);
        }

        public XmlDocument GerarXmlConsulta(long numeroLote)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlConsulta(this, numeroLote);
        }

        public RetornoTransmitir LerRetorno(XmlDocument xmlDocument, EnumOperacao op)
        {
            return LerRetorno(xmlDocument, op, "");
        }
        public RetornoTransmitir LerRetorno(XmlDocument servidorUniNFe, EnumOperacao op, string numeroNFSe)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            RetornoTransmitir retorno = null;

            //var arquivoEnvioRetornoErro = servidorUniNFe.ArquivoRetornoErro(this.Documento.TDFe.Tide.FNumeroLote.ToString(), op);
            //if (File.Exists(arquivoEnvioRetornoErro))
            //{
            //    retorno = new RetornoTransmitir("", "")
            //    {
            //        error = TrataMensagemRetornoErrorUniNfe(File.ReadAllText(arquivoEnvioRetornoErro))
            //    };

            //}
            //else
            //{
            //    if (Provedor.Nome == EnumProvedor.SigCorpSigISS)
            //    {
            //        retorno = this.Provedor.LerRetorno(this, servidorUniNFe.ArquivoRetorno(this.Documento.TDFe.Tide.FNumeroLote.ToString(), op));
            //    }
            //    else
            //    {
            //        retorno = this.Provedor.LerRetorno(this, servidorUniNFe.ArquivoRetorno(this.Documento.TDFe.Tide.FNumeroLote.ToString(), op), numeroNFSe);
            //    }
            //}

            return retorno;
        }

        public XmlDocument GerarXmlConsultaNotaValida(string numeroNFSe, string hash)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlConsultaNotaValida(this, numeroNFSe, hash);
        }

        public XmlDocument GerarXmlCancelaNota(string numeroNFSe, string motivo)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlCancelaNota(this, numeroNFSe, motivo);
        }

        public XmlDocument GerarXmlCancelaNota(string numeroNFSe)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlCancelaNota(this, numeroNFSe);
        }

        public XmlDocument GerarXmlCancelaNota(string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            if (this.Provedor == null)
                throw new Exception("Provedor não cadastrado.");

            return this.Provedor.GerarXmlCancelaNota(this, numeroNFSe, motivo, numeroLote, codigoVerificacao);
        }


        public byte[] MontarXmlRetorno(NFSeNota nota, string numeroNFSe, string codigoVerificacao)
        {
            var po = new NFe.API.Domain.XMLNFSe.NFe
            {
                Prefeitura = nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio,
                InscricaoPrestador = nota.Documento.TDFe.TPrestador.FInscricaoMunicipal,
                IEPrestador = nota.Documento.TDFe.TPrestador.FInscricaoEstadual
            };

            var cpfCNPJPrestador = new CPFCNPJPrestador
            {
                CNPJ = nota.Documento.TDFe.TPrestador.FCnpj
            };
            po.CPFCNPJPrestador = cpfCNPJPrestador;

            var chaveNFe = new ChaveNFe
            {
                NumeroNFe = numeroNFSe,
                SerieNFe = nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie,
                CodigoVerificacao = codigoVerificacao,
                DataEmissaoNFe = nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd")
            };
            po.ChaveNFe = chaveNFe;

            po.RazaoSocialPrestador = nota.Documento.TDFe.TPrestador.FRazaoSocial;

            var enderecoPrestador = new EnderecoPrestador
            {
                Logradouro = nota.Documento.TDFe.TPrestador.TEndereco.FEndereco,
                NumeroEndereco = nota.Documento.TDFe.TPrestador.TEndereco.FNumero,
                ComplementoEndereco = nota.Documento.TDFe.TPrestador.TEndereco.FComplemento,
                Bairro = nota.Documento.TDFe.TPrestador.TEndereco.FBairro,
                Cidade = nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio,
                UF = nota.Documento.TDFe.TPrestador.TEndereco.FUF,
                CEP = nota.Documento.TDFe.TPrestador.TEndereco.FCEP
            };
            po.EnderecoPrestador = enderecoPrestador;

            po.TelefonePrestador = Strings.FoneComDDD(nota.Documento.TDFe.TPrestador.TContato.FDDD ?? "", nota.Documento.TDFe.TPrestador.TContato.FFone ?? "");
            po.EmailPrestador = nota.Documento.TDFe.TPrestador.TContato.FEmail;

            var situacao = "";
            if (nota.Provedor.Nome == EnumProvedor.SigCorpSigISS)
            {
                po.StatusNFe = nota.Documento.TDFe.Tide.FStatus == EnumNFSeRPSStatus.srNormal ? "Ativa" : "Cancelada";
                switch (nota.Documento.TDFe.Tide.FNaturezaOperacao)
                {
                    case 1: { situacao = "Tributado no Prestador"; break; }
                    case 2: { situacao = "Tributado no Tomador"; break; }
                    case 3: { situacao = "Isenta"; break; }
                    case 4: { situacao = "imune"; break; }
                    default: { situacao = "Não tributada"; break; }
                }
                po.OpcaoSimples = (nota.Documento.TDFe.Tide.FOptanteSimplesNacional == 1 ? "SIM" : "NÃO");
                po.Operacao = nota.Documento.TDFe.Tide.FNaturezaOperacao.ToString();
            }
            else
            {
                po.StatusNFe = nota.Documento.TDFe.Tide.FStatus == EnumNFSeRPSStatus.srNormal ? "Normal" : "Cancelada";
                situacao = nota.Documento.TDFe.TServico.FTributacao;
                po.Operacao = nota.Documento.TDFe.TServico.FOperacao;
            }

            po.TributacaoNFe = situacao;
            po.ValorServicos = nota.Documento.TDFe.TServico.FValores.FValorServicos.ToString();
            po.ValorBase = nota.Documento.TDFe.TServico.FValores.FBaseCalculo.ToString();
            po.CodigoServico = nota.Documento.TDFe.TServico.FItemListaServico;
            po.AliquotaServicos = nota.Documento.TDFe.TServico.FValores.FAliquota.ToString();
            po.ValorINSS = nota.Documento.TDFe.TServico.FValores.FValorInss.ToString();
            po.ValorIR = nota.Documento.TDFe.TServico.FValores.FValorIr.ToString();
            po.ValorPIS = nota.Documento.TDFe.TServico.FValores.FValorPis.ToString();
            po.ValorCOFINS = nota.Documento.TDFe.TServico.FValores.FValorCofins.ToString();
            po.ValorCSLL = nota.Documento.TDFe.TServico.FValores.FValorCsll.ToString();
            po.ValorISS = nota.Documento.TDFe.TServico.FValores.FValorIss.ToString();
            po.ISSRetido = nota.Documento.TDFe.TServico.FValores.FValorIssRetido > 0 ? "SIM" : "NÃO";
            po.InscricaoTomador = nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal ?? "";

            var cpfCNPJTomador = new CPFCNPJTomador
            {
                CNPJ = nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj
            };
            po.CPFCNPJTomador = cpfCNPJTomador;

            po.IETomador = nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual ?? "";
            po.RazaoSocialTomador = nota.Documento.TDFe.TTomador.FRazaoSocial;

            var enderecoTomador = new EnderecoTomador
            {
                Logradouro = nota.Documento.TDFe.TTomador.TEndereco.FEndereco,
                NumeroEndereco = nota.Documento.TDFe.TTomador.TEndereco.FNumero,
                ComplementoEndereco = nota.Documento.TDFe.TTomador.TEndereco.FComplemento,
                Bairro = nota.Documento.TDFe.TTomador.TEndereco.FBairro,
                Cidade = nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio,
                UF = nota.Documento.TDFe.TTomador.TEndereco.FUF,
                CEP = nota.Documento.TDFe.TTomador.TEndereco.FCEP
            };
            po.EnderecoTomador = enderecoTomador;

            po.TelefoneTomador = Strings.FoneComDDD(nota.Documento.TDFe.TTomador.TContato.FDDD ?? "", nota.Documento.TDFe.TTomador.TContato.FFone ?? "");
            po.EmailTomador = nota.Documento.TDFe.TTomador.TContato.FEmail;
            po.Discriminacao = nota.Documento.TDFe.TServico.FDiscriminacao;

            var serializer = new XmlSerializer(typeof(NFe.API.Domain.XMLNFSe.NFe));
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream))
                {
                    serializer.Serialize(xmlWriter, po);
                    return memoryStream.ToArray();
                }
            }
        }

        public string TrataMensagemRetornoErrorUniNfe(string texto)
        {
            string textoRet = string.Empty;
            int pInicio;
            int pFim;

            pInicio = texto.IndexOf("Message|") + 8;

            if (pInicio == -1)
            {
                return texto == null ? "" : texto;
            }

            pFim = texto.IndexOf("StackTrace|") != -1 ? texto.IndexOf("StackTrace|") :
                    texto.IndexOf("Source|") != -1 ? texto.IndexOf("Source|") :
                    texto.IndexOf("Type|") != -1 ? texto.IndexOf("Type|") :
                    texto.IndexOf("TargetSite|") != -1 ? texto.IndexOf("TargetSite|") : texto.IndexOf("HashCode|");

            if (pFim == -1)
            {
                return texto == null ? "" : texto;
            }

            if (texto.Contains("The operation has timed out"))
            {
                textoRet = "O Webservice do município não retornou nenhuma resposta. Tente transmitir novamente mais tarde.";
            }
            else if (texto.Contains("The request failed with HTTP status 404: Not Found."))
            {
                textoRet = "O Webservice do município não está disponível. Entre em contato com a prefeitura ou tente transmitir novamente mais tarde.";
            }
            else if (texto.Contains("The request failed with HTTP status 503: Service Temporarily Unavailable."))
            {
                textoRet = "O Serviço de integração de notas do município está temporariamente indisponível. Entre em contato com a prefeitura ou tente transmitir novamente mais tarde.";
            }
            else
            {
                textoRet = texto.Substring(pInicio, (pFim - pInicio));
            }

            return textoRet;
        }


    }
}
