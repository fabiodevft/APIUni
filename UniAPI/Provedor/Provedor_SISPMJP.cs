using UniAPI.Domain;
using UniAPI.Enum;
using UniAPI.Interface;
using UniAPI.Provedor;
using UniAPI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace UniAPI.Provedor
{
    internal class Provedor_SISPMJP : AbstractProvedor, IProvedor
    {

        internal Provedor_SISPMJP()
        {
            this.Nome = EnumProvedor.SISPMJP;
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
            if (nota.Provedor.Nome != EnumProvedor.SISPMJP)
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
            bool bRpsNumero = false;
            var outrasInformacoes = string.Empty;

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

                                            case "consultarnfserpsresposta":
                                            case "nfse": // Consultar RPS
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
                                            case "rps":
                                                bRpsNumero = true;
                                                break;
                                            case "numero":
                                                if (numeroNF.Equals(""))
                                                {
                                                    numeroNF = x.ReadString();
                                                }
                                                else if (numeroRPS.Equals("") && bRpsNumero == true)
                                                {
                                                    numeroRPS = x.ReadString();
                                                    //long.TryParse(numeroRPS, out numeroLote);
                                                    bRpsNumero = false;
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
                                            case "status":
                                                if(x.ReadString().Trim() == "2")
                                                {
                                                    cancelamento = true;
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "outrasinformacoes":
                                                outrasInformacoes = x.ReadString();

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
                                            case "confirmacao":
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
                            if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name.ToString()) == "Codigo")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name.ToString()) == "Mensagem")
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
                            else if (x.NodeType == XmlNodeType.Element && Generico.ReplaceGenerico(x.Name.ToString()) == "Correcao")
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
                       
            //VERIFICAR SE A NOTA ESTA CANCELADA
            if (situacaoRPS != "C" && !string.IsNullOrEmpty(outrasInformacoes))
            {
                if (outrasInformacoes.ToUpper().Contains("NOTA FISCAL CANCELADA"))
                {
                    cancelamento = true;
                    situacaoRPS = "C";   
                }
            }

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
            var nodeEnviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);
            
            #region LoteRps
            var nodeLoteRps = Extensions.CriarNo(doc, nodeEnviarLoteRpsEnvio, "LoteRps", "", "versao", "1.02");

            Extensions.CriarNo(doc, nodeLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            #region CpfCnpj
            var CPFCNPJPrestador = Extensions.CriarNo(doc, nodeLoteRps, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            //Extensions.CriarNo(doc, nodeLoteRps, "InscricaoMunicipal" );
            Extensions.CriarNo(doc, nodeLoteRps, "QuantidadeRps", "1");

            #region ListaRps
            var nodeListaRps = Extensions.CriarNo(doc, nodeLoteRps, "ListaRps", "");

            #region Rps
            var nodeRps = Extensions.CriarNo(doc, nodeListaRps, "Rps", "");

            #region InfDeclaracaoPrestacaoServico
            var nodeInfDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, nodeRps, "InfDeclaracaoPrestacaoServico", "");
       
            #region RpsInfo
            var nodeRpsInfo = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Rps", "");

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, nodeRpsInfo, "IdentificacaoRps", "");

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, nodeRpsInfo, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNoNotNull(doc, nodeRpsInfo, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #endregion FIM - RpsInfo

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Servico", "");

            #region Valores
            var nodeValores = Extensions.CriarNo(doc, nodeServico, "Valores", "");

            Extensions.CriarNoNotNull(doc, nodeValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorDeducoes", nota.Documento.TDFe.TServico.FValores.FValorDeducoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorPis", nota.Documento.TDFe.TServico.FValores.FValorPis > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCofins", nota.Documento.TDFe.TServico.FValores.FValorCofins > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorInss", nota.Documento.TDFe.TServico.FValores.FValorInss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIr", nota.Documento.TDFe.TServico.FValores.FValorIr > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCsll", nota.Documento.TDFe.TServico.FValores.FValorCsll > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "OutrasRetencoes", nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIss", nota.Documento.TDFe.TServico.FValores.FValorIss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoIncondicionado", nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2) : null);
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoCondicionado", nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado) : null);
            
            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico));

            if (!string.IsNullOrEmpty(nota.Documento.TDFe.Tide._FMsgComplementares))
            {
                Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", 
                    nota.Documento.TDFe.TServico.FDiscriminacao + "\n\n\n" + nota.Documento.TDFe.Tide._FMsgComplementares);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", nota.Documento.TDFe.TServico.FDiscriminacao);
            }
                        
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeServico, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
            Extensions.CriarNoNotNull(doc, nodeServico, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            #endregion FIM - Servico

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Prestador", "");

            #region CpfCnpj
            var CPFCNPJPrestadorInfo = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorInfo, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            //Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Tomador", "");

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
            var nodeEndereco = Extensions.CriarNo(doc, nodeTomador, "Endereco", "");

            Extensions.CriarNoNotNull(doc, nodeEndereco, "Endereco", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Complemento", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nodeEndereco, "CodigoPais", nota.Documento.TDFe.TServico.FCodigoPais.ToString());
            Extensions.CriarNoNotNull(doc, nodeEndereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            //#region Contato
            //var nodeContato = Extensions.CriarNo(doc, nodeTomador, "Contato", "");

            //Extensions.CriarNoNotNull(doc, nodeContato, "Telefone", nota.Documento.TDFe.TTomador.TContato.FFone);
            //Extensions.CriarNoNotNull(doc, nodeContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            //#endregion FIM - Contato

            #endregion FIM - Tomador

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
                       
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
            var nodeGerarConsulta = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            #region CpfCnpj
            var CPFCNPJPrestadorInfo = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorInfo, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

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

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString().Trim());

            #endregion FIM - IdentificacaoRps

                       
            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            #region CpfCnpj
            var CPFCNPJPrestadorInfo = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorInfo, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            #endregion FIM - Prestador

            return doc;

        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            #region Pedido
            var pedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido");

            #region InfPedidoCancelamento
            var infPedidoCancelamentoNode = Extensions.CriarNo(doc, pedidoNode, "InfPedidoCancelamento", "", "Id", "C" + numeroNFSe);

            #region IdentificacaoNfse
            var identificacaoNfseNode = Extensions.CriarNo(doc, infPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, identificacaoNfseNode, "Numero", numeroNFSe);

            #region CpfCnpj
            var CPFCNPJPrestadorInfo = Extensions.CriarNo(doc, identificacaoNfseNode, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorInfo, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            
            #endregion FIM - CpfCnpj

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

            Extensions.CriarNo(doc, infPedidoCancelamentoNode, "CodigoCancelamento", motivoAux);

            #endregion FIM - InfPedidoCancelamento

            #endregion FIM - Pedido

            return doc;

        }

        #region CriaHeaderXml

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            doc.AppendChild(gerarNotaNode);

            var valAttribute = doc.CreateAttribute("xmlns");
            valAttribute.Value = "http://www.abrasf.org.br/nfse.xsd";
            gerarNotaNode.Attributes.Append(valAttribute);

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
