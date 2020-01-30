using UniAPI.Domain;
using UniAPI.Enum;
using UniAPI.Interface;
using UniAPI.Provedor;
using UniAPI.Util;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace UniAPI.Provedor
{
    internal class Provedor_IssOnline : AbstractProvedor, IProvedor
    {
        internal Provedor_IssOnline()
        {
            this.Nome = EnumProvedor.IssOnline;
        }

        private enum EnumArea
        {
            Nenhum = 0,
            Cabecalho = 1,
            Alerta = 2,
            Erro = 3,
            NFSe = 4,
            Nota = 5
        }

        private enum EnumResposta
        {
            Nenhum,
            EnviarLoteRpsResposta,
            ConsultarNfseRpsResposta,
            ConsultarNfseResposta,
            ConsultarLoteRpsResposta,
            CancelarNfseResposta
        }

        private static string FormataValor(decimal valor)
        {
            var retorno = valor.ToString();
            if (Extensions.PossuiCasasDecimais(valor))
            {
                retorno = valor.ToString().Replace(",", ".");
            }
            else
            {
                retorno = decimal.Floor(valor).ToString();
            }

            return retorno;
        }

        private static string ImpostoRetido(EnumNFSeSituacaoTributaria situacao, int tipo = 0)
        {
            var tipoRecolhimento = "2";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "1";
            }

            return tipoRecolhimento;
        }

        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.IssOnline)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var sucesso = false;
            var numeroNF = "";
            var numeroRPS = "";
            DateTime? dataEmissaoRPS = null;
            var situacaoRPS = "";
            var codigoVerificacao = "";
            var protocolo = "";
            var linkImpressao = "";
            long numeroLote = 0;
            var descricaoProcesso = "";
            var descricaoErro = "";
            var area = EnumArea.Nenhum;
            var codigoErroOuAlerta = "";
            var _EnumResposta = EnumResposta.Nenhum;
            var isValidXML = true;

            if (arquivo.ToString().ToLower().Contains("cannfse"))
            { _EnumResposta = EnumResposta.CancelarNfseResposta; }
            else if (arquivo.ToString().ToLower().Contains("sitnfse"))
            { _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; }
            else if (arquivo.ToString().ToLower().Contains("ret-loterps"))
            { _EnumResposta = EnumResposta.EnviarLoteRpsResposta; }

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("UTF-8"));
                using (XmlReader x = XmlReader.Create(stream))
                {
                    try { x.Read(); } catch { isValidXML = false; }
                    if (isValidXML)
                    {
                        while (x.Read())
                        {
                            if (x.NodeType == XmlNodeType.Element && area != EnumArea.Erro) { 
                                if (x.Name.ToString().ToLower().Equals("inconsistencia"))
                                {
                                    area = EnumArea.Erro;
                                }
                                else
                                {
                                    switch (_EnumResposta)
                                    {
                                        case EnumResposta.ConsultarNfseRpsResposta:
                                            {

                                                switch (x.Name.ToString().ToLower())
                                                {
                                                    case "cod":
                                                        numeroNF = x.ReadString();
                                                        break;
                                                    case "link":
                                                        linkImpressao = x.ReadString();
                                                        break;
                                                    case "sitabrev":
                                                        if (x.ReadString().ToString().Trim().ToLower().Equals("n"))
                                                        {
                                                            sucesso = true;
                                                        }
                                                        else {
                                                            sucesso = true;
                                                            situacaoRPS = "C";
                                                        }
                                                        break;
                                                    case "data":
                                                        DateTime emissao;
                                                        DateTime.TryParse(x.ReadString(), out emissao);
                                                        dataEmissaoRPS = emissao;
                                                        break;
                                                }
                                                break;                                                
                                            }
                                        case EnumResposta.CancelarNfseResposta:
                                            {

                                                switch (x.Name.ToString().ToLower())
                                                {
                                                    case "nota":
                                                        {
                                                            descricaoErro = x.ReadString();
                                                            if (descricaoErro.ToString().Trim().ToLower().Contains("nota cancelada com sucesso"))
                                                            {
                                                               descricaoErro = "";
                                                               //situacaoRPS = "C";
                                                               //sucesso = true;
                                                            }
                                                            else
                                                            {
                                                                codigoErroOuAlerta = descricaoErro;
                                                            }

                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                if (x.NodeType == XmlNodeType.Element && x.Name == "ERRO") {
                                    if (string.IsNullOrEmpty(descricaoErro))
                                    {
                                        descricaoErro = x.ReadString();
                                    }
                                    else
                                    {
                                        descricaoErro = string.Concat(descricaoErro, "\n", x.ReadString());
                                    }
                                }

                            }
                        }
                        x.Close();
                    }
                    else
                    {
                        x.Close();
                        stream.Dispose();
                        using (stream = new StreamReader(arquivo))
                        {
                            protocolo = stream.ReadToEnd().ToString().TrimStart().Trim();

                        }
                        if (protocolo.ToString().Trim().Length == 0)
                        {
                            descricaoErro = "ERRO DESCONHECIDO";
                        }
                    }
                }
                stream.Dispose();
            }

            var dhRecbto = "";
            var error = "";
            var success = "";

            if (dataEmissaoRPS != null && dataEmissaoRPS.Value != null)
            {
                nota.Documento.TDFe.Tide.DataEmissaoRps = dataEmissaoRPS.Value;
                nota.Documento.TDFe.Tide.DataEmissao = dataEmissaoRPS.Value;
                dhRecbto = dataEmissaoRPS.Value.ToString();
            }

            var xMotivo = descricaoErro != "" ? string.Concat(descricaoProcesso, "[", descricaoErro, "]") : descricaoProcesso;
            if ((sucesso && !string.IsNullOrEmpty(numeroNF)) || (!string.IsNullOrEmpty(numNF) && MesmaNota(numeroNF, numNF) && situacaoRPS != ""))
            {
                sucesso = true;
                success = "Sucesso";
            }
            else
            {
                error = xMotivo;
                if (string.IsNullOrEmpty(xMotivo) || xMotivo.IndexOf("E89") != -1)
                {
                    error = "Não foi possível finalizar a transmissão. Tente novamente mais tarde ou execute uma consulta.";
                }
            }

            var cStat = "";
            var xml = "";

            if (sucesso && situacaoRPS != "C")
            {
                cStat = "100";
                nota.Documento.TDFe.Tide.FStatus = EnumNFSeRPSStatus.srNormal;
                xMotivo = "NFSe Normal";
            }
            else if (sucesso && situacaoRPS == "C")
            {
                cStat = "101";
                nota.Documento.TDFe.Tide.FStatus = EnumNFSeRPSStatus.srCancelado;
                xMotivo = "NFSe Cancelada";
            }
            if (cStat == "100" || cStat == "101")
            {
                var xmlRetorno = nota.MontarXmlRetorno(nota, numeroNF, protocolo);
                xml = System.Text.Encoding.GetEncoding("utf-8").GetString(xmlRetorno);
            }

            return new RetornoTransmitir(error, success)
            {

                chave = numeroNF != "" && numeroNF != "0" ?
                            GerarChaveNFSe(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FEmitIBGEUF, nota.Documento.TDFe.Tide.DataEmissaoRps, nota.Documento.TDFe.TPrestador.FCnpj, numeroNF, 56) : "",
                cStat = cStat,
                xMotivo = xMotivo,
                numero = numeroNF,
                nProt = protocolo,
                xml = xml,
                digVal = codigoVerificacao,
                NumeroLote = numeroLote,
                NumeroRPS = numeroRPS,
                DataEmissaoRPS = dataEmissaoRPS,
                dhRecbto = dhRecbto,
                LinkImpressao = linkImpressao,
                CodigoRetornoPref = codigoErroOuAlerta

            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("NFSE", ref doc);

            #region "NFSE"

            #region "IDENTIFICAÇÃO"
            var IDENTIFICACAONode = Extensions.CriarNo(doc, gerarNotaNode, "IDENTIFICACAO");

            Extensions.CriarNoNotNull(doc, IDENTIFICACAONode, "MESCOMP", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("MM"));
            Extensions.CriarNoNotNull(doc, IDENTIFICACAONode, "ANOCOMP", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy"));

            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IDENTIFICACAONode, "INSCRICAO", _FInscricaoMunicipal.ToString("d9"));

            Extensions.CriarNoNotNull(doc, IDENTIFICACAONode, "VERSAO", "1.00");

            #endregion IDENTIFICAÇÃO

            #region "NOTAS"
            var NOTASNode = Extensions.CriarNo(doc, gerarNotaNode, "NOTAS");
            #region "NOTA"
            var NOTANode = Extensions.CriarNo(doc, NOTASNode, "NOTA");

            Extensions.CriarNo(doc, NOTANode, "LOTE", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, NOTANode, "SEQUENCIA", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, NOTANode, "DATAEMISSAO", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd/MM/yyyy"));
            Extensions.CriarNo(doc, NOTANode, "HORAEMISSAO", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("HH:mm:ss"));
            Extensions.CriarNo(doc, NOTANode, "LOCAL", tsLocal(nota));
            Extensions.CriarNo(doc, NOTANode, "SITUACAO", "1");
            Extensions.CriarNo(doc, NOTANode, "RETIDO", nota.Documento.TDFe.TServico.FValores.FIssRetido == 1 ? "S" : "N");
            Extensions.CriarNo(doc, NOTANode, "ATIVIDADE", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio);
            Extensions.CriarNo(doc, NOTANode, "ALIQUOTAAPLICADA", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota));
            Extensions.CriarNo(doc, NOTANode, "DEDUCAO", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNo(doc, NOTANode, "IMPOSTO", FormataValor(nota.Documento.TDFe.TServico.FValorCargaTributaria));
            Extensions.CriarNo(doc, NOTANode, "RETENCAO", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido));
            Extensions.CriarNo(doc, NOTANode, "OBSERVACAO", Generico.TratarString(nota.Documento.TDFe.Tide.FOutrasInformacoes));
            Extensions.CriarNo(doc, NOTANode, "CPFCNPJ", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, NOTANode, "NOMERAZAO", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNo(doc, NOTANode, "NOMEFANTASIA", Generico.TratarString(nota.Documento.TDFe.TTomador.FNomeFantasia));
            Extensions.CriarNo(doc, NOTANode, "MUNICIPIO", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNo(doc, NOTANode, "BAIRRO", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNo(doc, NOTANode, "CEP", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            Extensions.CriarNo(doc, NOTANode, "PREFIXO", "*");
            Extensions.CriarNo(doc, NOTANode, "LOGRADOURO", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNo(doc, NOTANode, "COMPLEMENTO", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNo(doc, NOTANode, "NUMERO", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNo(doc, NOTANode, "DENTROPAIS", "S");

            #region SERVICOS
            var SERVICOSNode = Extensions.CriarNo(doc, NOTANode, "SERVICOS");
            #region SERVICO

            foreach (var x in nota.Documento.TDFe.TServico.TItemServico)
            {

                var SERVICONode = Extensions.CriarNo(doc, SERVICOSNode, "SERVICO");

                Extensions.CriarNoNotNull(doc, SERVICONode, "DESCRICAO", Generico.TratarString(x.FDescricao));
                Extensions.CriarNoNotNull(doc, SERVICONode, "VALORUNIT", FormataValor(x.FValorUnitario));
                Extensions.CriarNoNotNull(doc, SERVICONode, "QUANTIDADE", FormataValor(x.FQuantidade));
            }

            #endregion SERVICO
            #endregion SERVICOS

            #endregion NOTA
            #endregion NOTAS

            #endregion NFSE
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("NFSE", ref doc);

            var IdentificacaoRpsNode = Extensions.CriarNo(doc, gerarNotaNode, "IDENTIFICACAO");

            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "INSCRICAO", _FInscricaoMunicipal.ToString("d9"));

            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "LOTE", nota.Documento.TDFe.Tide.FNumeroLote.ToString());

            doc.AppendChild(gerarNotaNode);
            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("NFSE", ref doc);

            var IdentificacaoRpsNode = Extensions.CriarNo(doc, gerarNotaNode, "IDENTIFICACAO");

            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "INSCRICAO", _FInscricaoMunicipal.ToString("d9"));

            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "LOTE", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "SEQUENCIA", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            doc.AppendChild(gerarNotaNode);
            return doc;
        }


        /// <summary>
        /// Cria o documento xml e retorna a TAG principal
        /// </summary>
        /// <param name="strNomeMetodo">Ex.: ConsultarNfseRpsEnvio</param>
        /// <param name="doc">Referencia do objeto que será o documento</param>
        /// <returns>retorna o node principal</returns>
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "iso-8859-1", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        private string tsNaturezaOperacao(NFSeNota nota)
        {
            /*tsNaturezaOperacao N Código de natureza da operação
                1 – Tributação no município
                2 - Tributação fora do município
                3 - Isenção
                4 - Imune
                5 –Exigibilidade suspensa por decisão judicial
                6 – Exigibilidade suspensa por procedimento
                administrativo*/

            var retorno = nota.Documento.TDFe.Tide.FNaturezaOperacao.ToString();

            if (retorno.Equals("1"))
            {
                if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
                {
                    retorno = "2";
                }
            }

            return retorno;

        }

        private string tsLocal(NFSeNota nota)
        {
            /*
             * D para dentro do município
             * F para fora do município 
             * P para fora do paíse 
             * */

            var retorno = "D";

            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                retorno = "F";
            }

            return retorno;

        }
    }
}