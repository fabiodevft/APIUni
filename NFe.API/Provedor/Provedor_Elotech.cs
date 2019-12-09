using NFe.API.Domain;
using NFe.API.Enum;
using NFe.API.Provedor;
using NFe.API.Util;
using System;
using System.IO;
using System.Xml;

namespace FRGDocFiscal.Provedor
{
    class Provedor_Elotech : AbstractProvedor, IProvedor
    {
        internal Provedor_Elotech()
        {
            this.Nome = EnumProvedor.Elotech;
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
            CancelarNfseResposta,
            GerarNfseResposta
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
                retorno = decimal.Floor(valor).ToString("#0.00").Replace(",", ".");
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
            if (nota.Provedor.Nome != EnumProvedor.Elotech)
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

            string codigoAcesso = string.Empty;


            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo);
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
                                                _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "gerarnfseresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.GerarNfseResposta; break;

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
                                                break;
                                            case "numerolote":
                                                long.TryParse(x.ReadString(), out numeroLote);
                                                break;
                                            case "chaveacesso":
                                                codigoVerificacao = x.ReadString();
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
                                            case "chaveacesso":
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
                                                    //long.TryParse(numeroRPS.Substring(4), out numeroLote);
                                                }
                                                break;
                                            case "nfsecancelamento":
                                                situacaoRPS = "C";
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
                                        }
                                        break;
                                    }
                                //case EnumResposta.ConsultarNfseResposta:
                                //    {
                                //        break;
                                //    }
                                case EnumResposta.ConsultarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "chaveacesso":
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
                                                    //long.TryParse(numeroRPS.Substring(4), out numeroLote);
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

                                            case "datahora":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "codigocancelamento":
                                                sucesso = true;
                                                break;
                                            case "listamensagemretorno":
                                            case "mensagemretorno":
                                                area = EnumArea.Erro;
                                                break;
                                        }
                                        break;
                                    }
                                case EnumResposta.GerarNfseResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "compnfse":
                                                sucesso = true;
                                                break;
                                            case "numero":
                                                if (sucesso)
                                                {
                                                    if (numeroNF.Equals(""))
                                                    {
                                                        numeroNF = x.ReadString();
                                                    }
                                                    else if (numeroRPS.Equals(""))
                                                    {
                                                        numeroRPS = x.ReadString();
                                                    }
                                                }

                                                break;
                                            case "chaveacesso":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                            case "numerolote":
                                                long.TryParse(x.ReadString(), out numeroLote);
                                                break;
                                            case "datarecebimento":
                                                break;
                                            case "listamensagemretorno":
                                                break;
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
                CodigoRetornoPref = codigoErroOuAlerta                

            };
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region EnviarLoteRpsEnvio
            var nodeEnviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);
            
            #region IdentificacaoRequerente
            var nodeIdentificacaoRequerente = Extensions.CriarNo(doc, nodeEnviarLoteRpsEnvio, "IdentificacaoRequerente");

            var nodeCpfCnpjRequerente = Extensions.CriarNo(doc, nodeIdentificacaoRequerente, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, nodeCpfCnpjRequerente, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
                     
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Homologa", nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao ? "true" : "false");

            #endregion FIM - IdentificacaoRequerente

            #region LoteRps
            var nodeLoteRps = Extensions.CriarNo(doc, nodeEnviarLoteRpsEnvio, "LoteRps");

            Extensions.CriarNoNotNull(doc, nodeLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FNumeroLote.ToString());
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "QuantidadeRps", "1");

            #region ListaRps
            var nodeListaRps = Extensions.CriarNo(doc, nodeLoteRps, "ListaRps");

            #region DeclaracaoPrestacaoServico
            var nodeDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, nodeListaRps, "DeclaracaoPrestacaoServico");

            #region InfDeclaracaoPrestacaoServico
            var nodeInfDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, nodeDeclaracaoPrestacaoServico, "InfDeclaracaoPrestacaoServico");

            #region Rps            
            var nodeRps = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Rps");

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, nodeRps, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, nodeRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNoNotNull(doc, nodeRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #endregion FIM - Rps

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Servico");

            #region Valores
            var nodeValores = Extensions.CriarNo(doc, nodeServico, "Valores");

            Extensions.CriarNoNotNull(doc, nodeValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorDeducoes", nota.Documento.TDFe.TServico.FValores.FValorDeducoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorPis", nota.Documento.TDFe.TServico.FValores.FValorPis > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCofins", nota.Documento.TDFe.TServico.FValores.FValorCofins > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorInss", nota.Documento.TDFe.TServico.FValores.FValorInss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIr", nota.Documento.TDFe.TServico.FValores.FValorIr > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCsll", nota.Documento.TDFe.TServico.FValores.FValorCsll > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "OutrasRetencoes", nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 4) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIss", nota.Documento.TDFe.TServico.FValores.FValorIss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            //Extensions.CriarNoNotNull(doc, nodeServico, "ResponsavelRetencao", nota.Documento.TDFe.TServico.FResponsavelRetencao.ToString());
            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            //Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio.ToString());
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeServico, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
            Extensions.CriarNoNotNull(doc, nodeServico, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            #region ListaItensServico
            var nodeListaItensServico = Extensions.CriarNo(doc, nodeServico, "ListaItensServico");

            #region ItemServico

            foreach (var x in nota.Documento.TDFe.TServico.TItemServico)
            {
                var nodeItemServico = Extensions.CriarNo(doc, nodeListaItensServico, "ItemServico");

                Extensions.CriarNoNotNull(doc, nodeItemServico, "ItemListaServico", Generico.RetornaApenasLetrasNumeros(x.FCodigo));
                Extensions.CriarNoNotNull(doc, nodeItemServico, "CodigoCnae", x.FCodLCServ);
                Extensions.CriarNoNotNull(doc, nodeItemServico, "Descricao", Generico.TratarString(x.FDescricao));
                Extensions.CriarNoNotNull(doc, nodeItemServico, "Tributavel", x.FAliquota > 0 ? "1" : "2");
                Extensions.CriarNoNotNull(doc, nodeItemServico, "Quantidade", FormataValor(x.FQuantidade));
                Extensions.CriarNoNotNull(doc, nodeItemServico, "ValorUnitario", FormataValor(x.FValorUnitario));
                Extensions.CriarNoNotNull(doc, nodeItemServico, "ValorDesconto", "0");
                Extensions.CriarNoNotNull(doc, nodeItemServico, "ValorLiquido", FormataValor(x.FValorTotal));
            }

            #endregion FIM - ItemServico

            #endregion FIM - ListaItensServico

            #endregion FIM - Servico

            #region DadosPrestador
            var nodeDadosPrestador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "DadosPrestador");

            #region IdentificacaoPrestador
            var nodeIdentificacaoPrestador = Extensions.CriarNo(doc, nodeDadosPrestador, "IdentificacaoPrestador");

            var nodePrestadorCnpjLoteRps = Extensions.CriarNo(doc, nodeIdentificacaoPrestador, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, nodePrestadorCnpjLoteRps, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoPrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - IdentificacaoPrestador

            Extensions.CriarNoNotNull(doc, nodeDadosPrestador, "RazaoSocial", nota.Documento.TDFe.TPrestador.FRazaoSocial);

            #region Endereco
            var nodeEnderecoPrestador = Extensions.CriarNo(doc, nodeDadosPrestador, "Endereco");

            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "Endereco", nota.Documento.TDFe.TPrestador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "Numero", nota.Documento.TDFe.TPrestador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "Bairro", nota.Documento.TDFe.TPrestador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "CidadeNome", nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio.ToString().Trim());
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "Uf", nota.Documento.TDFe.TPrestador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nodeEnderecoPrestador, "Cep", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TPrestador.TEndereco.FCEP));

            #endregion FIM - Endereco 

            #endregion FIM - Prestador

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Tomador");

            #region IdentificacaoTomador
            var nodeIdentificacaoTomador = Extensions.CriarNo(doc, nodeTomador, "IdentificacaoTomador");

            var CPFCNPJTomador = Extensions.CriarNo(doc, nodeIdentificacaoTomador, "CpfCnpj");
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

            Extensions.CriarNoNotNull(doc, nodeTomador, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial);

            #region Endereco
            var nodeEndereco = Extensions.CriarNo(doc, nodeTomador, "Endereco");

            Extensions.CriarNoNotNull(doc, nodeEndereco, "Endereco", nota.Documento.TDFe.TPrestador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Numero", nota.Documento.TDFe.TPrestador.TEndereco.FNumero);
            //Extensions.CriarNoNotNull(doc, nodeEndereco, "Complemento", nota.Documento.TDFe.TPrestador.TEndereco.FComplemento);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Bairro", nota.Documento.TDFe.TPrestador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "CidadeNome", nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio.ToString().Trim());
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Uf", nota.Documento.TDFe.TPrestador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Cep", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TPrestador.TEndereco.FCEP));

            #endregion FIM - Endereco

            #region Contato
            var nodeTomadorContato = Extensions.CriarNo(doc, nodeTomador, "Contato");

            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            Extensions.CriarNoNotNull(doc, nodeTomador, "InscricaoEstadual", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual);

            #endregion FIM - Tomador

            //Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao != 0 ? nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString() : "");
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
                       
            #endregion FIM - InfDeclaracaoPrestacaoServico

            #endregion FIM - DeclaracaoPrestacaoServico

            #endregion FIM - ListaRps

            #endregion FIM - LoteRps

            #endregion FIM - EnviarLoteRpsEnvio

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var consultarNfseRpsEnvio = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            #region IdentificacaoRequerente
            var nodeIdentificacaoRequerente = Extensions.CriarNo(doc, consultarNfseRpsEnvio, "IdentificacaoRequerente");

            var nodeCpfCnpjRequerente = Extensions.CriarNo(doc, nodeIdentificacaoRequerente, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, nodeCpfCnpjRequerente, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Homologa", nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao ? "true" : "false");

            #endregion FIM - IdentificacaoRequerente           
            
            Extensions.CriarNoNotNull(doc, consultarNfseRpsEnvio, "NumeroLote", numeroLote.ToString());                     

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            var doc = new XmlDocument();

            #region CancelarNfseEnvio
            var cancelarNfseEnvio = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            #region IdentificacaoRequerente
            var nodeIdentificacaoRequerente = Extensions.CriarNo(doc, cancelarNfseEnvio, "IdentificacaoRequerente");

            var nodeCpfCnpjRequerente = Extensions.CriarNo(doc, nodeIdentificacaoRequerente, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, nodeCpfCnpjRequerente, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRequerente, "Homologa", nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao ? "true" : "false");

            #endregion FIM - IdentificacaoRequerente    

            #region Pedido
            var pedidoNode = Extensions.CriarNo(doc, cancelarNfseEnvio, "Pedido");

            #region InfPedidoCancelamento
            var infPedidoCancelamentoNode = Extensions.CriarNo(doc, pedidoNode, "InfPedidoCancelamento");

            #region IdentificacaoNfse
            var identificacaoNfseNode = Extensions.CriarNo(doc, infPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, identificacaoNfseNode, "Numero", numeroNFSe);

            var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, identificacaoNfseNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNo(doc, identificacaoNfseNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
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

            Extensions.CriarNo(doc, infPedidoCancelamentoNode, "ChaveAcesso", codigoVerificacao);
            Extensions.CriarNo(doc, infPedidoCancelamentoNode, "CodigoCancelamento", motivoAux); 

            #endregion FIM - InfPedidoCancelamento

            #endregion FIM - Pedido

            #endregion FIM - CancelarNfseEnvio

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
            //XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            //doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://shad.elotech.com.br/schemas/iss/nfse_v1_2.xsd";
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
