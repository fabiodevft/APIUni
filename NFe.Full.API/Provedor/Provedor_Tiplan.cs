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
    internal class Provedor_Tiplan : AbstractProvedor, IProvedor
    {
        internal Provedor_Tiplan()
        {
            this.Nome = EnumProvedor.Tiplan;
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
            if (nota.Provedor.Nome != EnumProvedor.Tiplan)
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
            var LinkImpressaoAux = "";
            bool bIdentificacaoRps = false;

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
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "cancelarnfseresposta": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "consultarloterpsresposta":
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;

                                        }
                                        break;

                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "protocolo":
                                                int protocoloAux;
                                                int.TryParse(x.ReadString(), out protocoloAux);
                                                protocolo = protocoloAux.ToString();
                                                break;
                                            case "numerolote":
                                                long.TryParse(x.ReadString(), out numeroLote);
                                                break;
                                            case "datarecebimento":
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
                                        switch (x.Name.ToString().ToLower())
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
                                                else if (numeroRPS.Equals("") && bIdentificacaoRps)
                                                {
                                                    numeroRPS = x.ReadString();
                                                    bIdentificacaoRps = false;
                                                }
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                            case "datahoracancelamento":
                                                situacaoRPS = "C";
                                                break;

                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;

                                            case "identificacaorps":
                                                bIdentificacaoRps = true;
                                                break;
                                        }
                                        break;
                                    }

                                case EnumResposta.CancelarNfseResposta:
                                    {

                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "datahoracancelamento":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "confirmacao":
                                            case "codigocancelamento":
                                                situacaoRPS = "C";
                                                sucesso = true;
                                                break;

                                            case "nfsecancelamento":
                                                cancelamento = true;
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
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Codigo")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Mensagem")
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
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Correcao")
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
                if (string.IsNullOrEmpty(xMotivo) || xMotivo.IndexOf("E89") != -1)
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

            //PARAMETRO DO LINK DE IMPRESSÃO: inscrição municipal - numero nota - código de verificação
            if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
            {
                switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.ToString().Trim())
                {
                    case "3303302": //Niterói-RJ
                        LinkImpressaoAux = "https://nfse.niteroi.rj.gov.br/nfse.aspx?ccm=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim() + "&nf=" + numeroNF.ToString().Trim() + "&cod=" + Generico.RemoverCaracterEspecial(codigoVerificacao);
                        break;

                    case "3301702": //Duque de Caxias-RJ
                        LinkImpressaoAux = "https://spe.duquedecaxias.rj.gov.br/nfse.aspx?ccm=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim() + "&nf=" + numeroNF.ToString().Trim() + "&cod=" + Generico.RemoverCaracterEspecial(codigoVerificacao);
                        break;

                    case "2611606": //Recife-PE
                        LinkImpressaoAux = "https://nfse.recife.pe.gov.br/contribuinte/notaprint.aspx?inscricao=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim() + "&nf=" + numeroNF.ToString().Trim() + "&verificacao=" + Generico.RemoverCaracterEspecial(codigoVerificacao);
                        break;

                }

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
                CodigoRetornoPref = codigoErroOuAlerta

            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            #region "EnviarLoteRpsEnvio"
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);

            #region LoteRps
            var nodeLoteRps = Extensions.CriarNo(doc, gerarNotaNode, "LoteRps", "", "Id", "L" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString().Trim());
                       
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "QuantidadeRps", "1");

            #region ListaRps
            var nodeListaRps = Extensions.CriarNo(doc, nodeLoteRps, "ListaRps");

            #region Rps
            var nodeRps = Extensions.CriarNo(doc, nodeListaRps, "Rps");

            #region InfRps
            var InfRps = Extensions.CriarNo(doc, nodeRps, "InfRps");
            var vsAttributeInfRps = doc.CreateAttribute("Id");
            vsAttributeInfRps.Value = "R" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString().Trim();
            InfRps.Attributes.Append(vsAttributeInfRps);

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, InfRps, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, InfRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));
            Extensions.CriarNoNotNull(doc, InfRps, "NaturezaOperacao", tsNaturezaOperacao(nota));
            Extensions.CriarNoNotNull(doc, InfRps, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, InfRps, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNoNotNull(doc, InfRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, InfRps, "Servico");

            #region Valores
            var servicoValoresNode = Extensions.CriarNo(doc, nodeServico, "Valores");

            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2));
            //Extensions.CriarNoNotNull(doc, servicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 4) : "0.0000");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico.ToString()));

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == "2611606") //RECIFE-PE
            {
                Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio.ToString()));
            }
            else
            {
                Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico.ToString()));
            }
            
            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FMunicipioIncidencia);
            
            #endregion FIM - Servico

            #region Prestador

            var prestadorNode = Extensions.CriarNo(doc, InfRps, "Prestador");            
            Extensions.CriarNoNotNull(doc, prestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            #region Tomador

            var tomadorNode = Extensions.CriarNo(doc, InfRps, "Tomador");

            #region "IdentificacaoTomador"
            var identificacaoTomadorNode = Extensions.CriarNo(doc, tomadorNode, "IdentificacaoTomador");

            var CPFCNPJTomador = Extensions.CriarNo(doc, identificacaoTomadorNode, "CpfCnpj");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F" || nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "R")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }


            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, tomadorNode, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region Endereco
            var tomadorEnderecoNode = Extensions.CriarNo(doc, tomadorNode, "Endereco");

            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato
            var tomadorContatoNode = Extensions.CriarNo(doc, tomadorNode, "Contato");

            Extensions.CriarNoNotNull(doc, tomadorContatoNode, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, tomadorContatoNode, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            #endregion FIM - InfDeclaracaoPrestacaoServico

            #endregion FIM - Rps

            #endregion FIM - ListaRps

            #endregion FIM - LoteRps

            #endregion FIM - EnviarLoteRpsEnvio

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            var prestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");            
            Extensions.CriarNoNotNull(doc, prestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            Extensions.CriarNoNotNull(doc, gerarNotaNode, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;

        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido", "", "xmlns", "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd");

            #region "InfPedidoCancelamento"
            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento", "", "Id", "Cancelamento_NF" + numeroNFSe);
            
            #region "tcIdentificacaoNfse"

            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);
            //var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, IdentificacaoNfseNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, IdentificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj); 
            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));
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

            Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "CodigoCancelamento", motivoAux);

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
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);

            var nsAttribute = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttribute);
          

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
