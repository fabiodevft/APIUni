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
    public class Provedor_DBSeller : AbstractProvedor, IProvedor
    {
        internal Provedor_DBSeller()
        {
            this.Nome = EnumProvedor.DBSeller;
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
            if (nota.Provedor.Nome != EnumProvedor.DBSeller)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

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
            var linkImpressaoAux = string.Empty;

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
                                        switch (Generico.ReplaceGenerico(x.Name.ToString().ToLower()))
                                        {
                                            case "cancelarnfseresposta": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "consultarloterpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;

                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (Generico.ReplaceGenerico(x.Name.ToString().ToLower()))
                                        {
                                            case "protocolo":
                                                protocolo = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;
                                        }
                                        break;
                                    }
                                case EnumResposta.ConsultarNfseRpsResposta:
                                    {
                                        switch (Generico.ReplaceGenerico(x.Name.ToString().ToLower()))
                                        {
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
                                                    numeroRPS = x.ReadString();
                                                    long.TryParse(numeroRPS, out numeroLote);
                                                }
                                                break;

                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                            case "datahora":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
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
                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;
                                        }
                                        break;
                                    }

                                case EnumResposta.CancelarNfseResposta:
                                    {
                                        switch (Generico.ReplaceGenerico(x.Name.ToString().ToLower()))
                                        {
                                            case "cancelamento":
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
                                                {
                                                    numeroNF = x.ReadString();
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    numeroRPS = x.ReadString();
                                                    long.TryParse(numeroRPS, out numeroLote);
                                                }
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
                            if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name) == "Codigo")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name) == "Mensagem")
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
                            else if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name) == "Correcao")
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
                var msgRetornoAux = xMotivo;

                if ((msgRetornoAux.Contains("O numero do lote do contribuinte informado, já existe.") ||
                        msgRetornoAux.Contains("O número do lote do contribuinte informado, já existe."))
                        && msgRetornoAux.Contains("Protocolo:"))
                {
                    var protocoloAux = msgRetornoAux.Substring(msgRetornoAux.LastIndexOf("Protocolo: ") + 10);
                    protocoloAux = Generico.RetornarApenasNumeros(protocoloAux);

                    if (!String.IsNullOrEmpty(protocoloAux))
                    {
                        protocolo = protocoloAux;
                        xMotivo = String.Empty;
                    }

                }

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
            
            //if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
            //{
            //    linkImpressaoAux = "https://notacarioca.rio.gov.br/contribuinte/notaprint.aspx?inscricao=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim() + "&nf=" + numeroNF.ToString().Trim() + "&cod=" + codigoVerificacao.Replace("-", "").Trim();
            //}

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
                LinkImpressao = linkImpressaoAux

            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            #region "EnviarLoteRpsEnvio"
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);

            #region "tcLoteRps"                   

            var CabLoteRps = Extensions.CriarNo(doc, gerarNotaNode, "LoteRps", "");

            var vsAttribute = doc.CreateAttribute("versao");
            vsAttribute.Value = "1.00";
            //CabLoteRps.Attributes.Append(vsAttribute);

            Extensions.CriarNoNotNull(doc, CabLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, CabLoteRps, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, CabLoteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, CabLoteRps, "QuantidadeRps", "1");

            var Listarps = Extensions.CriarNo(doc, CabLoteRps, "ListaRps");

            #region "TcRps=>TcInfRps"
            var rpsNode = Extensions.CriarNo(doc, Listarps, "Rps");

            var ListarpsNode = Extensions.CriarNo(doc, rpsNode, "InfRps");
            //vsAttribute = doc.CreateAttribute("Id");
            //vsAttribute.Value = "ID" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero + nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie;
            //ListarpsNode.Attributes.Append(vsAttribute);

            #region "TcIdentificacaoRps"
            var IdentificacaoRpsNode = Extensions.CriarNo(doc, ListarpsNode, "IdentificacaoRps");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());
            #endregion

            Extensions.CriarNoNotNull(doc, ListarpsNode, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));
            Extensions.CriarNoNotNull(doc, ListarpsNode, "NaturezaOperacao", tsNaturezaOperacao(nota));
            //Extensions.CriarNoNotNull(doc, ListarpsNode, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            Extensions.CriarNoNotNull(doc, ListarpsNode, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, ListarpsNode, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNoNotNull(doc, ListarpsNode, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());
            //RpsSubstituido TcIdentificacaoRps 0-1 

            #region "Servico -> TcDadosServico"

            var ServicoNode = Extensions.CriarNo(doc, ListarpsNode, "Servico");

            #region "tcValores"
            var ServicoValoresNode = Extensions.CriarNo(doc, ServicoNode, "Valores");
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 4) : "0.00");
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));
            #endregion tcValores

            Extensions.CriarNoNotNull(doc, ServicoNode, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);

            if (!string.IsNullOrEmpty(nota.Documento.TDFe.Tide._FMsgComplementares))
            {
                Extensions.CriarNoNotNull(doc, ServicoNode, "Discriminacao",
                    Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao) + "\n\n\n"
                    + Generico.TratarString(nota.Documento.TDFe.Tide._FMsgComplementares));
            }
            else
            {
                Extensions.CriarNoNotNull(doc, ServicoNode, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            }
            //Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoTributacaoMunicipio", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio));
            Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoMunicipio", nota.Documento.TDFe.TServico.FMunicipioIncidencia);

            #endregion Servico

            #region "Prestador -> TcIdentificacaoPrestador"
            var PrestadorNode = Extensions.CriarNo(doc, ListarpsNode, "Prestador");

            Extensions.CriarNoNotNull(doc, PrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            #endregion Prestador

            #region "Tomador -> tcDadosTomador"
            var TomadorNode = Extensions.CriarNo(doc, ListarpsNode, "Tomador");

            #region "TcIdentificacaoTomador"
            var IdentificacaoTomadorNode = Extensions.CriarNo(doc, TomadorNode, "IdentificacaoTomador");

            var CPFCNPJTomador = Extensions.CriarNo(doc, IdentificacaoTomadorNode, "CpfCnpj");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }

            //Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "InscricaoMunicipal", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal));
            #endregion "TcIdentificacaoTomador"

            Extensions.CriarNoNotNull(doc, TomadorNode, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region "TcEndereco"
            var TomadorEnderecoNode = Extensions.CriarNo(doc, TomadorNode, "Endereco");
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            #endregion "TcEndereco"

            #region "TcContato"
            var TomadorContatoNode = Extensions.CriarNo(doc, TomadorNode, "Contato");
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            #endregion "TcContato"

            #endregion "Tomador -> tcDadosTomador"

            #endregion "TcRps=>TcInfRps"

            #endregion "tcLoteRps"   

            #region "Signature => dsig:Signature "
            #endregion

            #endregion EnviarLoteRpsEnvio
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            var PrestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");
            Extensions.CriarNoNotNull(doc, PrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            doc.AppendChild(gerarNotaNode);

            Extensions.CriarNoNotNull(doc, gerarNotaNode, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;
        }
        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido", "");

            #region "InfPedidoCancelamento"
            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento", "");
            
            #region "tcIdentificacaoNfse"

            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            long _FInscricaoMunicipal;

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", nota?.Documento?.TDFe?.TPrestador?.FInscricaoMunicipal?.ToString().Trim() ?? "");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
            #endregion "tcIdentificacaoNfse"

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

            Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "CodigoCancelamento", motivoAux); // tsCodigoCancelamentoNfse


            #endregion "InfPedidoCancelamento"
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
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

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

    }
}
