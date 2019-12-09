using NFe.API.Domain;
using NFe.API.Enum;
using NFe.API.Provedor;
using NFe.API.Util;
using System;
using System.IO;
using System.Text;
using System.Xml;
using static NFe.API.Util.Extensions;

namespace FRGDocFiscal.Provedor
{
    internal class Provedor_Paulistana : AbstractProvedor, IProvedor
    {
        internal Provedor_Paulistana()
        {
            this.Nome = EnumProvedor.Paulistana;
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

        private static string SituacaoRPS(EnumNFSeRPSStatus status)
        {
            var situacaoRPS = "N";
            if (status == EnumNFSeRPSStatus.srCancelado)
            {
                situacaoRPS = "C";
            }
            return situacaoRPS;
        }

        private static string TipoRecolhimento(EnumNFSeSituacaoTributaria situacao, int tipo = 0)
        {
            var tipoRecolhimento = "false";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "true";
            }

            return tipoRecolhimento;
        }

        private static string TipoTributacao(NFSeNota nota)
        {
            //T – Tributado em São Paulo
            //F – Tributado Fora de São Paulo
            string trib = "T";

            if(nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                trib = "F";
            }
            return trib;
        }

        private static string GerarAssinatura(NFSeNota nota)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TServico.FCodigoCnae))
                throw new ArgumentException("CNAE do emitente não pode ser nulo");

            var reg = new TRegistroEDI();
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 01, 08, 0, nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 09, 05, 0, nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie, ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 14, 12, 0, nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataAAAAMMDD_________, 26, 08, 0, nota.Documento.TDFe.Tide.DataEmissaoRps, ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 34, 01, 0, TipoTributacao(nota), ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 35, 01, 0, SituacaoRPS(nota.Documento.TDFe.Tide.FStatus), ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 36, 01, 0, TipoRecolhimento((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido) == "true" ? "S" : "N", ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 37, 15, 2, nota.Documento.TDFe.TServico.FValores.FValorServicos, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 52, 15, 2, nota.Documento.TDFe.TServico.FValores.FValorDeducoes, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 67, 05, 0, nota.Documento.TDFe.TServico.FItemListaServico, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 72, 01, 0, nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F" ? "1" : "2", '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 73, 14, 0, nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, '0'));
         
            reg.CodificarLinha();

            return reg.LinhaRegistro;
        }

        private static string GerarAssinaturaCancelamento(NFSeNota nota, string numeroNFSe)
        {
            var reg = new TRegistroEDI();
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 01, 8, 0, nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 09, 12, 0, numeroNFSe, '0'));
            reg.CodificarLinha();

            return reg.LinhaRegistro;
        }

        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.Paulistana)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var sucesso = false;
            var numeroNF = "";
            var numeroRPS = "";
            DateTime? dataEmissaoRPS = null;
            var situacaoRPS = "";
            var codigoVerificacao = "";
            long numeroLote = 0;
            var descricaoProcesso = "";
            var descricaoErro = "";
            var area = EnumArea.Nenhum;
            var codigoPrimeiroErroOuAlerta = "";
            var codigoErroOuAlerta = "";

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("UTF-8"));
                using (XmlReader x = XmlReader.Create(stream))
                {
                    while (x.Read())
                    {
                        if (x.NodeType == XmlNodeType.Element && x.Name == "RetornoCancelamentoNFe")
                        {
                            situacaoRPS = "C";
                        }
                        if (x.NodeType == XmlNodeType.Element && x.Name == "Cabecalho")
                        {
                            area = EnumArea.Cabecalho;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Alerta")
                        {
                            area = EnumArea.Alerta;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Erro")
                        {
                            area = EnumArea.Erro;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "NFe")
                        {
                            area = EnumArea.Nota;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "ChaveNFeRPS")
                        {
                            area = EnumArea.Nota;
                        }

                        #region Cabecalho
                        if (area == EnumArea.Cabecalho)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Sucesso")
                            {
                                sucesso = x.ReadString() == "true";
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroLote")
                            {
                                long.TryParse(x.ReadString(), out numeroLote);
                            }
                        }
                        #endregion Cabecalho
                        #region Alerta
                        else if (area == EnumArea.Alerta)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Codigo")
                            {
                                if (codigoPrimeiroErroOuAlerta == "")
                                {
                                    codigoPrimeiroErroOuAlerta = x.ReadString();
                                }
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Descricao")
                            {
                                if (string.IsNullOrEmpty(descricaoProcesso))
                                {
                                    descricaoProcesso = string.Concat("[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                                else
                                {
                                    descricaoProcesso = string.Concat(descricaoProcesso, "\n", "[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                            }
                        }
                        #endregion Alerta
                        #region Erro
                        else if (area == EnumArea.Erro)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Codigo")
                            {
                                if (codigoPrimeiroErroOuAlerta == "")
                                {
                                    codigoPrimeiroErroOuAlerta = x.ReadString();
                                }
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Descricao")
                            {
                                if (string.IsNullOrEmpty(descricaoErro))
                                {
                                    descricaoErro = string.Concat("[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                                else
                                {
                                    descricaoErro = string.Concat(descricaoErro, "\n", "[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                            }
                        }
                        #endregion Erro
                        #region Nota
                        else if (area == EnumArea.Nota)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroNFe")
                            {
                                numeroNF = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroLote" && MesmaNota(numeroNF, numNF))
                            {
                                long.TryParse(x.ReadString(), out numeroLote);
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "CodigoVerificacao" && MesmaNota(numeroNF, numNF))
                            {
                                codigoVerificacao = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroRPS" && MesmaNota(numeroNF, numNF))
                            {
                                numeroRPS = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "DataEmissaoRPS" && MesmaNota(numeroNF, numNF))
                            {
                                DateTime emissao;
                                DateTime.TryParse(x.ReadString(), out emissao);
                                dataEmissaoRPS = emissao;
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "StatusNFe" && MesmaNota(numeroNF, numNF))
                            {
                                situacaoRPS = x.ReadString();
                            }
                        }
                        #endregion Nota
                    }
                    x.Close();
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
                if (string.IsNullOrEmpty(xMotivo))
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
                var xmlRetorno = nota.MontarXmlRetorno(nota, numeroNF, codigoVerificacao);
                xml = System.Text.Encoding.GetEncoding("utf-8").GetString(xmlRetorno);
            }

            return new RetornoTransmitir(error, success)
            {
                chave = numeroNF != "" && numeroNF != "0" ?
                            GerarChaveNFSe(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FEmitIBGEUF, nota.Documento.TDFe.Tide.DataEmissaoRps, nota.Documento.TDFe.TPrestador.FCnpj, numeroNF, 56) : "",
                cStat = cStat,
                xMotivo = xMotivo,
                numero = numeroNF,
                LinkImpressao = @"https://nfe.prefeitura.sp.gov.br/contribuinte/notaprint.aspx?inscricao=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal + "&nf=" + numeroNF + "&verificacao=" + codigoVerificacao,
                xml = xml,
                digVal = codigoVerificacao,
                NumeroLote = numeroLote,
                NumeroRPS = numeroRPS,
                DataEmissaoRPS = dataEmissaoRPS,
                dhRecbto = dhRecbto,
                CodigoRetornoPref = codigoPrimeiroErroOuAlerta
            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement("PedidoEnvioLoteRPS", "http://localhost:8080/WsNFe2/lote");

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var shAttribute = doc.CreateAttribute("xmlns", "xsd", "http://www.w3.org/2000/xmlns/");
            shAttribute.Value = "http://www.w3.org/2001/XMLSchema";
            gerarNotaNode.Attributes.Append(shAttribute);

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.prefeitura.sp.gov.br/nfe";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");
            var vsAttribute = doc.CreateAttribute("Versao", null);
            vsAttribute.Value = "1";
            cabecalhoNode.Attributes.Append(vsAttribute);

            var CPFCNPJRemetente = Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente");
            Extensions.CriarNo(doc, CPFCNPJRemetente, "CNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "transacao", "false");
            Extensions.CriarNo(doc, cabecalhoNode, "dtInicio", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "dtFim", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "QtdRPS", "1");
            Extensions.CriarNo(doc, cabecalhoNode, "ValorTotalServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, cabecalhoNode, "ValorTotalDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));

            #endregion Cabecalho

            #region RPS

            var rpsNode = Extensions.CriarNo(doc, gerarNotaNode, "RPS");
            Extensions.CriarNo(doc, rpsNode, "Assinatura", GerarAssinatura(nota));

            var chaveRPSNode = Extensions.CriarNo(doc, rpsNode, "ChaveRPS");
            Extensions.CriarNo(doc, chaveRPSNode, "InscricaoPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, chaveRPSNode, "SerieRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, chaveRPSNode, "NumeroRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            Extensions.CriarNo(doc, rpsNode, "TipoRPS", "RPS");
            Extensions.CriarNo(doc, rpsNode, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, rpsNode, "StatusRPS", SituacaoRPS(nota.Documento.TDFe.Tide.FStatus));
            Extensions.CriarNo(doc, rpsNode, "TributacaoRPS", TipoTributacao(nota));
            Extensions.CriarNo(doc, rpsNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, rpsNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNo(doc, rpsNode, "ValorPIS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            Extensions.CriarNo(doc, rpsNode, "ValorCOFINS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));
            Extensions.CriarNo(doc, rpsNode, "ValorINSS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));
            Extensions.CriarNo(doc, rpsNode, "ValorIR", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));
            Extensions.CriarNo(doc, rpsNode, "ValorCSLL", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll));
            Extensions.CriarNo(doc, rpsNode, "CodigoServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNo(doc, rpsNode, "AliquotaServicos", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota/100) : "0");
            Extensions.CriarNo(doc, rpsNode, "ISSRetido", TipoRecolhimento((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));

            var CPFCNPJTomador = Extensions.CriarNo(doc, rpsNode, "CPFCNPJTomador");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNo(doc, CPFCNPJTomador, "CPF", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNo(doc, CPFCNPJTomador, "CNPJ", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            Extensions.CriarNo(doc, rpsNode, "RazaoSocialTomador", nota.Documento.TDFe.TTomador.FRazaoSocial);
            var enderecoTomador = Extensions.CriarNo(doc, rpsNode, "EnderecoTomador");
            Extensions.CriarNo(doc, enderecoTomador, "TipoLogradouro");
            Extensions.CriarNo(doc, enderecoTomador, "Logradouro", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNo(doc, enderecoTomador, "NumeroEndereco", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNo(doc, enderecoTomador, "ComplementoEndereco", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
            Extensions.CriarNo(doc, enderecoTomador, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
            Extensions.CriarNo(doc, enderecoTomador, "Cidade", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNo(doc, enderecoTomador, "UF", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNo(doc, enderecoTomador, "CEP", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            Extensions.CriarNo(doc, rpsNode, "EmailTomador", nota.Documento.TDFe.TTomador.TContato.FEmail);

            var discriminacao = nota.Documento.TDFe.TServico.FDiscriminacao + "| " + nota.Documento.TDFe.Tide.FOutrasInformacoes;

            Extensions.CriarNo(doc, rpsNode, "Discriminacao", discriminacao);
            Extensions.CriarNo(doc, rpsNode, "ValorCargaTributaria", FormataValor(nota.Documento.TDFe.TServico.FValorCargaTributaria));
            Extensions.CriarNo(doc, rpsNode, "PercentualCargaTributaria", FormataValor(nota.Documento.TDFe.TServico.FPercentualCargaTributaria/100));
            Extensions.CriarNo(doc, rpsNode, "FonteCargaTributaria", nota.Documento.TDFe.TServico.FFonteCargaTributaria);
            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                Extensions.CriarNo(doc, rpsNode, "MunicipioPrestacao", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            }
            
            #endregion RPS

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement("p1", "PedidoConsultaLote", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "p1", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.prefeitura.sp.gov.br/nfe";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");
            var vsAttribute = doc.CreateAttribute("Versao", null);
            vsAttribute.Value = "1";
            cabecalhoNode.Attributes.Append(vsAttribute);

            var CPFCNPJRemetente = Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente");
            Extensions.CriarNo(doc, CPFCNPJRemetente, "CNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "NumeroLote", numeroLote.ToString());

            #endregion Cabecalho

            return doc;
        }
        
        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement("p1", "PedidoConsultaNFe", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "p1", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.prefeitura.sp.gov.br/nfe";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");
            var vsAttribute = doc.CreateAttribute("Versao", null);
            vsAttribute.Value = "1";
            cabecalhoNode.Attributes.Append(vsAttribute);

            var CPFCNPJRemetente = Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente");
            Extensions.CriarNo(doc, CPFCNPJRemetente, "CNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            //Extensions.CriarNo(doc, cabecalhoNode, "NumeroLote", numeroLote.ToString());

            #endregion Cabecalho

            #region Detalhe
            var nodeDetalhe = Extensions.CriarNo(doc, gerarNotaNode, "Detalhe");

            #region ChaveRps
            var nodeChaveRps = Extensions.CriarNo(doc, nodeDetalhe, "ChaveRPS");

            Extensions.CriarNo(doc, nodeChaveRps, "InscricaoPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, nodeChaveRps, "SerieRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, nodeChaveRps, "NumeroRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            #endregion FIM - ChaveRps

            #endregion FIM - Detalhe

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement("PedidoCancelamentoNFe", "http://localhost:8080/WsNFe2/lote");
            
            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var shAttribute = doc.CreateAttribute("xmlns", "xsd", "http://www.w3.org/2000/xmlns/");
            shAttribute.Value = "http://www.w3.org/2001/XMLSchema";
            gerarNotaNode.Attributes.Append(shAttribute);

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.prefeitura.sp.gov.br/nfe";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");
            var vsAttribute = doc.CreateAttribute("Versao", null);
            vsAttribute.Value = "1";
            cabecalhoNode.Attributes.Append(vsAttribute);

            var CPFCNPJRemetente = Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente");
            Extensions.CriarNo(doc, CPFCNPJRemetente, "CNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "transacao", "true");

            #endregion Cabecalho

            #region Detalhe
            var detalhe = Extensions.CriarNo(doc, gerarNotaNode, "Detalhe");
            var chave = Extensions.CriarNo(doc, detalhe, "ChaveNFe");
            Extensions.CriarNo(doc, chave, "InscricaoPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, chave, "NumeroNFe", numeroNFSe);

            Extensions.CriarNo(doc, detalhe, "AssinaturaCancelamento", GerarAssinaturaCancelamento(nota, numeroNFSe));
            #endregion Detalhe
            return doc;
        }
    }
}
