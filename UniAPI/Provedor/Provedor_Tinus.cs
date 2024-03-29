﻿using UniAPI.Domain;
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
    internal class Provedor_Tinus : AbstractProvedor, IProvedor
    {
        internal Provedor_Tinus()
        {
            this.Nome = EnumProvedor.Tinus;
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
            if (nota.Provedor.Nome != EnumProvedor.Tinus)
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


            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("ISO-8859-1"));
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
                                            //case "consultarloterpsresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                            //case "consultarnfseresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarNfseResposta; break;
                                            case "consultarloterpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "nfse": // Consultar RPS
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
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    numeroRPS = x.ReadString();
                                                    long.TryParse(numeroRPS, out numeroLote);
                                                }
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString().Replace("Z", ""), out emissao);

                                                dataEmissaoRPS = emissao;
                                                break;

                                            case "codigocancelamento":
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
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "confirmacao":
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

                                if (x.ReadString().ToString().Contains("Essa NFS-e já está cancelada"))
                                {
                                    cancelamento = true;
                                    sucesso = true;
                                    situacaoRPS = "C";
                                    numeroNF = numNF;
                                }
                                else
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
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Correcao")
                            {
                                if (!(cancelamento == true && sucesso == true && situacaoRPS == "C"))
                                {
                                    var correcao = x.ReadString().ToString().Trim() ?? "";
                                    if (correcao != "") { descricaoErro = string.Concat(descricaoErro, " ( Sugestão: " + correcao + " ) "); }
                                }                                
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

            //PARAMETRO DO LINK DE IMPRESSÃO: inscrição municipal - numero nota - código de verificação
            if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
            {

                switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
                {
                    case "2606200": /*Goiana-PE*/
                        LinkImpressaoAux = "https://www.tinus.com.br/csp/GOIANA/portal/nfsepdf.csp?WPARAM=" + Generico.RemoverCaracterEspecial(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim()) + "-" + numeroNF.ToString().Trim() + "-" + codigoVerificacao;
                        break;

                    case "2503209": /*Cabedelo-PB*/
                        LinkImpressaoAux = "https://www.tinus.com.br/csp/CABEDELO/portal/nfsepdfImp.csp?WPARAM=" + Generico.RetornarApenasNumeros(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim()) + "-" + numeroNF.ToString().Trim() + "-" + codigoVerificacao;
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
                CodigoRetornoPref = codigoErroOuAlerta,
                LinkImpressao = LinkImpressaoAux

            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region EnviarLoteRpsEnvio

            var enviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);
            //Extensions.CriarNo(doc, argNode, "EnviarLoteRpsEnvio", "", "xmlns", nameSpace);

            #region LoteRps
            var loteRps = Extensions.CriarNo(doc, enviarLoteRpsEnvio, "LoteRps");
            //var loteRps = CriaHeaderXml("LoteRps", ref doc);

            var vsAttribute = doc.CreateAttribute("id");
            vsAttribute.Value = "LOTE" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero;
            loteRps.Attributes.Append(vsAttribute);

            Extensions.CriarNo(doc, loteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, loteRps, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, loteRps, "InscricaoMunicipal", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal));
            Extensions.CriarNo(doc, loteRps, "QuantidadeRps", "1");

            #region ListaRps

            var listarps = Extensions.CriarNo(doc, loteRps, "ListaRps");

            #region Rps
            var rpsNode = Extensions.CriarNo(doc, listarps, "Rps");

            #region InfRps 
            var infRpsNode = Extensions.CriarNo(doc, rpsNode, "InfRps");
            vsAttribute = doc.CreateAttribute("id");
            vsAttribute.Value = "ID" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero + nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie;
            infRpsNode.Attributes.Append(vsAttribute);

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, infRpsNode, "IdentificacaoRps");
            Extensions.CriarNo(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNo(doc, infRpsNode, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));
            Extensions.CriarNo(doc, infRpsNode, "NaturezaOperacao", tsNaturezaOperacao(nota));
            Extensions.CriarNoNotNull(doc, infRpsNode, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            Extensions.CriarNo(doc, infRpsNode, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNo(doc, infRpsNode, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNo(doc, infRpsNode, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #region Servico
            var servicoNode = Extensions.CriarNo(doc, infRpsNode, "Servico");

            #region Valores
            var servicoValoresNode = Extensions.CriarNo(doc, servicoNode, "Valores");

            Extensions.CriarNo(doc, servicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "BaseCalculo", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100) : "0");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado));

            #endregion FIM - Valores

            Extensions.CriarNo(doc, servicoNode, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio);
            Extensions.CriarNo(doc, servicoNode, "Discriminacao", nota.Documento.TDFe.TServico.FDiscriminacao);
            Extensions.CriarNo(doc, servicoNode, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            #endregion FIM - Servico

            #region Prestador

            var prestadorNode = Extensions.CriarNo(doc, infRpsNode, "Prestador");
            Extensions.CriarNo(doc, prestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal));

            #endregion FIM - Prestador

            #region Tomador

            var tomadorNode = Extensions.CriarNo(doc, infRpsNode, "Tomador");

            #region IdentificacaoTomador

            var identificacaoTomadorNode = Extensions.CriarNo(doc, tomadorNode, "IdentificacaoTomador");
            var CPFCNPJTomador = Extensions.CriarNo(doc, identificacaoTomadorNode, "CpfCnpj");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }

            Extensions.CriarNoNotNull(doc, identificacaoTomadorNode, "InscricaoMunicipal", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal));
            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNo(doc, tomadorNode, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial);

            #region Endereco

            var tomadorEnderecoNode = Extensions.CriarNo(doc, tomadorNode, "Endereco");
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Endereco", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Complemento", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
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

            #endregion InfRps - RPS

            #endregion FIM - Rps

            #endregion FIM - ListaRps

            #endregion FIM - LoteRps

            #endregion FIM - EnviarLoteRpsEnvio

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);

            var prestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");
            Extensions.CriarNoNotNull(doc, prestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal));
            Extensions.CriarNoNotNull(doc, gerarNotaNode, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;

        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);

            #region Pedido
            var pedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido");

            #region InfPedidoCancelamento
            var infPedidoCancelamentoNode = Extensions.CriarNo(doc, pedidoNode, "InfPedidoCancelamento", "", "id", "ID" + numeroNFSe);

            #region IdentificacaoNfse
            var identificacaoNfseNode = Extensions.CriarNo(doc, infPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, identificacaoNfseNode, "Numero", numeroNFSe);
            Extensions.CriarNo(doc, identificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, identificacaoNfseNode, "InscricaoMunicipal", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal));
            Extensions.CriarNo(doc, identificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);

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

            Extensions.CriarNo(doc, infPedidoCancelamentoNode, "CodigoCancelamento", motivoAux);

            #endregion FIM - InfPedidoCancelamento

            #endregion FIM - Pedido

            return doc;
        }

        #region CriaHeaderXml

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc, EnumAmbiente ambiente)
        {

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            string nameSpace;
            nameSpace = (ambiente == EnumAmbiente.Homologacao ? "http://www2.tinus.com.br" : "http://www.tinus.com.br");

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = nameSpace;
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);

            return gerarNotaNode;
        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            doc.AppendChild(gerarNotaNode);

            return gerarNotaNode;
        }

        #endregion
        private string tsNaturezaOperacao(NFSeNota nota)
        {
            /*tsNaturezaOperacao N Código de natureza da operação
                1 – Tributação no município
                2 - Tributação fora do município
                3 - Isenção
                4 - Imune
                5 – Exigibilidade suspensa por decisão judicial
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
