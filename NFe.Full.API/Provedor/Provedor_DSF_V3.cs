using NFe.Full.API.Domain;
using NFe.Full.API.Enum;
using NFe.Full.API.Interface;
using NFe.Full.API.Provedor;
using NFe.Full.API.Util;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace FRGDocFiscal.Provedor
{
    public class Provedor_DSF_V3 : AbstractProvedor, IProvedor
    {
        internal Provedor_DSF_V3()
        {
            this.Nome = EnumProvedor.DSF_V3;
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

        #region FormataValor

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

        private static string FormataValor(decimal valor, int casasDecimais)
        {
            var retorno = valor.ToString();
            if (Extensions.PossuiCasasDecimais(valor))
            {
                valor = Math.Round(valor, casasDecimais);
                retorno = valor.ToString().Replace(",", ".");
            }
            else
            {
                retorno = decimal.Floor(valor).ToString("#0.00").Replace(",", ".");
            }

            return retorno;
        }

        #endregion

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

        /// <summary>
        /// Função para tratar o texto de tags xml.
        /// </summary>
        /// <param name="texto"></param>
        /// <returns>Retorna o nome da tag sem prefixos, indepentente do tipo do prefixo.</returns>
        private string TrataTextoLeituraXml(string texto)
        {
            string textoAux = string.Empty;

            if (!String.IsNullOrEmpty(texto))
            {
                textoAux = texto.Substring(texto.IndexOf(":") + 1);
            }

            return textoAux;
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.DSF_V3)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var bIdentificacaoRPS = false;
            var sucesso = false;
            var cancelamento = false;
            var numeroNF = "";
            var numeroRPS = "";
            DateTime? dataEmissaoRPS = null;
            var situacaoRPS = "";
            var codigoVerificacao = "";
            var protocolo = "";
            long numeroLote = 0;
            var descricaoProcesso = "";
            var descricaoErro = "";
            var area = EnumArea.Nenhum;
            var codigoErroOuAlerta = "";
            var _EnumResposta = EnumResposta.Nenhum;
            var LinkImpressaoAux = string.Empty;

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("UTF-8"));
                using (XmlReader x = XmlReader.Create(stream))
                {
                    while (x.Read())
                    {
                        if (x.NodeType == XmlNodeType.Element && area != EnumArea.Erro)
                        {
                            switch (_EnumResposta)
                            {
                                case EnumResposta.Nenhum:
                                    #region "EnumResposta"    
                                    {
                                        switch (TrataTextoLeituraXml(x.Name.ToString().ToLower()))
                                        {
                                            case "cancelarnfseresposta": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "consultarloterpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "gerarnfseresposta":
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;
                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (TrataTextoLeituraXml(x.Name.ToString().ToLower()))
                                        {

                                            case "identificacaorps":
                                                bIdentificacaoRPS = true;
                                                break;

                                            case "protocolo":
                                                protocolo = x.ReadString();
                                                sucesso = true;
                                                break;

                                            case "listamensagemretorno":

                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;

                                            case "codigoverificacao":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;

                                            case "numero":
                                                if (numeroNF.Equals(""))
                                                {
                                                    numeroNF = x.ReadString();
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    if (bIdentificacaoRPS)
                                                    {
                                                        numeroRPS = x.ReadString();
                                                        long.TryParse(numeroRPS, out numeroLote);

                                                        bIdentificacaoRPS = false;
                                                    }

                                                }
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                        }
                                        break;
                                    }
                                case EnumResposta.ConsultarNfseRpsResposta:
                                    {
                                        switch (TrataTextoLeituraXml(x.Name.ToString().ToLower()))
                                        {
                                            case "identificacaorps":
                                                bIdentificacaoRPS = true;
                                                break;

                                            case "codigoverificacao":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "numero":
                                                if (numeroNF.Equals(""))
                                                {
                                                    numeroNF = x.ReadString();
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    if (bIdentificacaoRPS)
                                                    {
                                                        numeroRPS = x.ReadString();
                                                        long.TryParse(numeroRPS, out numeroLote);

                                                        bIdentificacaoRPS = false;
                                                    }

                                                }
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;

                                            case "nfsecancelamento":
                                                cancelamento = true;
                                                break;
                                            case "datahora":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;
                                        }
                                        break;
                                    }

                                case EnumResposta.CancelarNfseResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "nfsecancelamento":
                                                cancelamento = true;
                                                break;

                                            case "datahoracancelamento":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "numero":
                                                if (numeroNF.Equals(""))
                                                    numeroNF = x.ReadString();

                                                break;
                                            case "confirmacao":
                                                sucesso = true;
                                                break;
                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;
                                        }
                                        break;

                                    }
                            }
                        }

                        #region Erro
                        if (area == EnumArea.Erro)
                        {
                            if (x.NodeType == XmlNodeType.Element && TrataTextoLeituraXml(x.Name) == "Codigo")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && TrataTextoLeituraXml(x.Name) == "Mensagem")
                            {
                                if (string.IsNullOrEmpty(descricaoErro))
                                {
                                    descricaoErro = string.Concat("[", codigoErroOuAlerta, "] ", TrataTextoLeituraXml(x.ReadString()));
                                    codigoErroOuAlerta = "";
                                }
                                else
                                {
                                    descricaoErro = string.Concat(descricaoErro, "\n", "[", codigoErroOuAlerta, "] ", TrataTextoLeituraXml(x.ReadString()));
                                    codigoErroOuAlerta = "";
                                }
                            }
                            else if (x.NodeType == XmlNodeType.Element && TrataTextoLeituraXml(x.Name) == "Correcao")
                            {
                                var correcao = x.ReadString().ToString().Trim() ?? "";
                                if (correcao != "") { descricaoErro = string.Concat(descricaoErro, " ( Sugestão: " + correcao + " ) "); }
                            }
                        }
                        #endregion Erro

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
                    if (protocolo != "")
                        error = "Não foi possível finalizar a transmissão. Aguarde alguns minutos e execute um consulta para finalizar a operação. Protocolo gerado: " + protocolo.ToString().Trim();
                    else
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



            if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
            {

                LinkImpressaoAux = @"https://notajoseense.sjc.sp.gov.br/notafiscal/qrCodeServlet?idMultiTenant=8&hash=" + codigoVerificacao;

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
                CodigoRetornoPref = codigoErroOuAlerta,
                LinkImpressao = LinkImpressaoAux

            };
        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);

            var nsAttributeXsd = doc.CreateAttribute("xmlns:xsd", "http://www.w3.org/2000/xmlns/");
            nsAttributeXsd.Value = "http://www.w3.org/2001/XMLSchema";
            gerarNotaNode.Attributes.Append(nsAttributeXsd);

            var nsAttributeXsi = doc.CreateAttribute("xmlns:xsi", "http://www.w3.org/2000/xmlns/");
            nsAttributeXsi.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttributeXsi);

            var nsAttributeXmlns = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeXmlns.Value = "http:/www.abrasf.org.br/nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttributeXmlns);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        #region XML

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region EnviarLoteRpsEnvio
            var enviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);

            #region LoteRps
            var loteRps = Extensions.CriarNo(doc, enviarLoteRpsEnvio, "LoteRps", "", "Id", "LOTE" + nota.Documento.TDFe.Tide.FNumeroLote);

            var vsAttribute = doc.CreateAttribute("versao");
            vsAttribute.Value = "3.00";
            loteRps.Attributes.Append(vsAttribute);

            Extensions.CriarNo(doc, loteRps, "NumeroLote", nota.Documento.TDFe.Tide.FNumeroLote.ToString());
            Extensions.CriarNoNotNull(doc, loteRps, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, loteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, loteRps, "QuantidadeRps", "1");

            #region ListaRps
            var listarps = Extensions.CriarNo(doc, loteRps, "ListaRps", "");

            #region Rps
            var nodeRps = Extensions.CriarNo(doc, listarps, "Rps", "");

            #region InfRps
            var nodeInfRps = Extensions.CriarNo(doc, nodeRps, "InfRps", "");

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, nodeInfRps, "IdentificacaoRps", "");

            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, nodeInfRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));
            Extensions.CriarNoNotNull(doc, nodeInfRps, "NaturezaOperacao", tsNaturezaOperacao(nota));
            Extensions.CriarNoNotNull(doc, nodeInfRps, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfRps, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfRps, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInfRps, "Servico", "");

            #region Valores
            var nodeServicoValores = Extensions.CriarNo(doc, nodeServico, "Valores", "");

            Extensions.CriarNo(doc, nodeServicoValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "BaseCalculo", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 4) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico));
            //Extensions.CriarNoNotNull(doc, nodeServico, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio);


            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao",
                            Generico.TrocarCaractercomAcentos(nota.Documento.TDFe.TServico.FDiscriminacao?.Replace("&amp;", "e").Replace("&", "e"))
                        + "\n" + "\n" + "\n" + "\n"
                        + Generico.TrocarCaractercomAcentos(nota.Documento.TDFe.Tide._FMsgComplementares?.Replace("&amp;", "e").Replace("&", "e")));

            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            #endregion FIM - Servico

            #region Prestador

            var nodePrestador = Extensions.CriarNo(doc, nodeInfRps, "Prestador", "");
            Extensions.CriarNo(doc, nodePrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInfRps, "Tomador", "");

            #region IdentificacaoTomador
            var nodeIdentificacaoTomador = Extensions.CriarNo(doc, nodeTomador, "IdentificacaoTomador", "");
            var CPFCNPJTomador = Extensions.CriarNo(doc, nodeIdentificacaoTomador, "CpfCnpj", "");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoTomador, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);
            
            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNo(doc, nodeTomador, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial);

            #region Endereco
            var nodeTomadorEndereco = Extensions.CriarNo(doc, nodeTomador, "Endereco", "");

            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato
            var nodeContato = Extensions.CriarNo(doc, nodeTomador, "Contato", "");

            Extensions.CriarNoNotNull(doc, nodeContato, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, nodeContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            #endregion FIM - InfRps

            #endregion FIM - Rps

            #endregion FIM - ListaRps

            #endregion FIM - LoteRps

            #endregion FIM - EnviarLoteRpsEnvio

            return doc;

        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {

            if ((nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "") == "")
            {
                throw new ArgumentException("Não foi possível finalizar a transmissão. Tente novamente mais tarde ou execute uma consulta.");
            }

            var doc = new XmlDocument();
            var nodeGerarConsulta = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            Extensions.CriarNoNotNull(doc, nodePrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            Extensions.CriarNoNotNull(doc, nodeGerarConsulta, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;            

        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            var doc = new XmlDocument();
            var nodeGerarConsulta = CriaHeaderXml("ConsultarNfseRpsEnvio", ref doc);

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, nodeGerarConsulta, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString());
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion fim - IdentificacaoRps

            #region Prestador            
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            Extensions.CriarNoNotNull(doc, nodePrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            var nodeCancelaNota = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            #region Pedido

            var nodePedido = Extensions.CriarNo(doc, nodeCancelaNota, "Pedido");

            #region InfPedidoCancelamento
            var nodeInfPedidoCancelamento = Extensions.CriarNo(doc, nodePedido, "InfPedidoCancelamento", "", "Id", "C" + numeroNFSe);

            #region IdentificacaoNfse
            var nodeIdentificacaoNfse = Extensions.CriarNo(doc, nodeInfPedidoCancelamento, "IdentificacaoNfse");

            Extensions.CriarNo(doc, nodeIdentificacaoNfse, "Numero", numeroNFSe);
            Extensions.CriarNo(doc, nodeIdentificacaoNfse, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeIdentificacaoNfse, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, nodeIdentificacaoNfse, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);

            #endregion FIM - IdentificacaoNfse

            var motivoAux = "2";
            switch (motivo.ToLower().Trim())
            {
                case "erro na emissão":
                    motivoAux = "1";
                    break;
                case "serviço não prestado":
                    motivoAux = "2";
                    break;
                case "duplicidade da nota":
                    motivoAux = "4";
                    break;
            }

            Extensions.CriarNo(doc, nodeInfPedidoCancelamento, "CodigoCancelamento", motivoAux);

            #endregion FIM - InfPedidoCancelamento

            #endregion FIM - Pedido

            return doc;
        }     

        #endregion
    }

}
