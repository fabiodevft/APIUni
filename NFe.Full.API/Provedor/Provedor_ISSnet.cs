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
    public class Provedor_ISSnet : AbstractProvedor, IProvedor
    {
        internal Provedor_ISSnet()
        {
            this.Nome = EnumProvedor.ISSnet;
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
            if (nota.Provedor.Nome != EnumProvedor.ISSnet)
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
            var LinkImpressaoAux = "";

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
                                        switch (TrataTextoLeituraXml(x.Name.ToString().ToLower()))
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

            //if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
            //{
            //    string strReportVersao = string.Empty;

            //    switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            //    {
            //        case "2304400": /*Fortaleza-CE*/
            //            LinkImpressaoAux = "https://iss.fortaleza.ce.gov.br/grpfor/pagesPublic/consultarNota.seam?codigo=" + codigoVerificacao + "&consultarNota=true&chave=" + nota.Documento.TDFe.TPrestador.FInscricaoMunicipal.ToString().Trim() + "&numero=" + numeroNF.ToString().Trim() + "";
            //            break;

            //        case "2704302": /*Maceió-AL*/
            //            strReportVersao = "nfs_ver21";
            //            LinkImpressaoAux = "http://visualizar.ginfes.com.br/report/consultarNota?__report=" + strReportVersao + "&cdVerificacao=" + codigoVerificacao + "&numNota=" + numeroNF.ToString().Trim() + "&cnpjPrestador=null";
            //            break;

            //        default:
            //            strReportVersao = "nfs_ver4";
            //            LinkImpressaoAux = "http://visualizar.ginfes.com.br/report/consultarNota?__report=" + strReportVersao + "&cdVerificacao=" + codigoVerificacao + "&numNota=" + numeroNF.ToString().Trim() + "&cnpjPrestador=null";
            //            break;

            //    }

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
                LinkImpressao = LinkImpressaoAux

            };
        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc, string vlAtributo)
        {

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");

            nsAttributeTipos.Value = vlAtributo;
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        #region XML

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region EnviarLoteRpsEnvio
            var enviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc, "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_enviar_lote_rps_envio.xsd");

            string[] prefixo = { "tc", "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd" };

            #region LoteRps
            var loteRps = Extensions.CriarNo(doc, enviarLoteRpsEnvio, "LoteRps", "");

            var vsAttribute = doc.CreateAttribute("xmlns:tc");
            loteRps.Attributes.Append(vsAttribute);
            vsAttribute.Value = "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd";

            Extensions.CriarNo(doc, loteRps, "NumeroLote", nota.Documento.TDFe.Tide.FNumeroLote.ToString(), prefixo);

            #region CpfCnpj
            var CPFCNPJnota = Extensions.CriarNo(doc, loteRps, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJnota, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);

            #endregion FIM - CpfCnpj

            Extensions.CriarNoNotNull(doc, loteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);
            Extensions.CriarNoNotNull(doc, loteRps, "QuantidadeRps", "1", prefixo);

            #region ListaRps
            var listarps = Extensions.CriarNo(doc, loteRps, "ListaRps", "", prefixo);

            #region Rps
            var nodeRps = Extensions.CriarNo(doc, listarps, "Rps", "", prefixo);

            #region InfRps
            var nodeInfRps = Extensions.CriarNo(doc, nodeRps, "InfRps", "", prefixo);

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, nodeInfRps, "IdentificacaoRps", "", prefixo);

            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, prefixo);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie, prefixo);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString(), prefixo);

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, nodeInfRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"), prefixo);
            Extensions.CriarNoNotNull(doc, nodeInfRps, "NaturezaOperacao", tsNaturezaOperacao(nota), prefixo);
            Extensions.CriarNoNotNull(doc, nodeInfRps, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString(), prefixo);
            Extensions.CriarNoNotNull(doc, nodeInfRps, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString(), prefixo);
            Extensions.CriarNoNotNull(doc, nodeInfRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString(), prefixo);
            Extensions.CriarNoNotNull(doc, nodeInfRps, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString(), prefixo);                        

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInfRps, "Servico", "", prefixo);

            #region Valores
            var nodeServicoValores = Extensions.CriarNo(doc, nodeServico, "Valores", "", prefixo);

            Extensions.CriarNo(doc, nodeServicoValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "BaseCalculo", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 4) : "0.00", prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2), prefixo);

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico, prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae, prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio, prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao.Replace("&amp;", "e").Replace("&", "e")), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "MunicipioPrestacaoServico", nota.Documento.TDFe.TServico.FCodigoMunicipio, prefixo);

            #endregion FIM - Servico


            #region Prestador

            var nodePrestador = Extensions.CriarNo(doc, nodeInfRps, "Prestador", "", prefixo);
                        
            #region CpfCnpj
            var CPFCNPJnotaPrestador = Extensions.CriarNo(doc, loteRps, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJnotaPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);
            #endregion FIM - CpfCnpj

            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);

            #endregion FIM - Prestador

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInfRps, "Tomador", "", prefixo);

            #region IdentificacaoTomador
            var nodeIdentificacaoTomador = Extensions.CriarNo(doc, nodeTomador, "IdentificacaoTomador", "", prefixo);
            var CPFCNPJTomador = Extensions.CriarNo(doc, nodeIdentificacaoTomador, "CpfCnpj", "", prefixo);
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, prefixo);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, prefixo);
            }

            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNo(doc, nodeTomador, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial, prefixo);

            #region Endereco
            var nodeTomadorEndereco = Extensions.CriarNo(doc, nodeTomador, "Endereco", "", prefixo);

            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco), prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro), prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP, prefixo);

            #endregion FIM - Endereco

            //#region Contato
            //var nodeContato = Extensions.CriarNo(doc, nodeTomador, "Contato", "", prefixo);

            //Extensions.CriarNoNotNull(doc, nodeContato, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone), prefixo);
            //Extensions.CriarNoNotNull(doc, nodeContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail, prefixo);

            //#endregion FIM - Contato

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

            string[] prefixo = { "tipos", "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd" };

            var doc = new XmlDocument();
            var nodeGerarConsulta = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc, "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_consultar_lote_rps_envio.xsd");

            var valAttribute = doc.CreateAttribute("xmlns:tipos");
            valAttribute.Value = "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd";
            nodeGerarConsulta.Attributes.Append(valAttribute);

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            #region CpfCnpj
            var CPFCNPJnotaPrestador = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJnotaPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);
            #endregion FIM - CpfCnpj

            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);

            #endregion FIM - Prestador

            Extensions.CriarNoNotNull(doc, nodeGerarConsulta, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {

            string[] prefixo = { "tc", "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd" };


            var doc = new XmlDocument();
            var nodeGerarConsulta = CriaHeaderXml("ConsultarNfseRpsEnvio", ref doc, "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_consultar_nfse_rps_envio.xsd");

            var vsAttribute = doc.CreateAttribute("xmlns:tc");
            vsAttribute.Value = "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd";

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, nodeGerarConsulta, "IdentificacaoRps");
            nodeIdentificacaoRps.Attributes.Append(vsAttribute);

            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString(), prefixo);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie, prefixo);
            Extensions.CriarNoNotNull(doc, nodeIdentificacaoRps, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString(), prefixo);

            #endregion fim - IdentificacaoRps

            #region Prestador            
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            #region CpfCnpj
            var CPFCNPJnotaPrestador = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJnotaPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);
            #endregion FIM - CpfCnpj
                       
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);

            #endregion FIM - Prestador

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            string[] prefixoTC = { "tc", "http://www.issnetonline.com.br/webserviceabrasf/vsd/tipos_complexos.xsd"};
            string[] prefixoP1 = { "p1", "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_cancelar_nfse_envio.xsd" };

            var nodeCancelaNota = CriaHeaderXml("CancelarNfseEnvio", ref doc, "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_cancelar_nfse_envio.xsd");

            var valAttribute = doc.CreateAttribute("xmlns:p1");
            valAttribute.Value = "http://www.issnetonline.com.br/webserviceabrasf/vsd/servico_cancelar_nfse_envio.xsd";
            nodeCancelaNota.Attributes.Append(valAttribute);

            #region Pedido
            var nodePedido = Extensions.CriarNo(doc, nodeCancelaNota, "Pedido");

            #region InfPedidoCancelamento
            var nodeInfPedidoCancelamento = Extensions.CriarNo(doc, nodePedido, "Pedido", "", prefixoTC);

            Extensions.CriarNoNotNull(doc, nodeInfPedidoCancelamento, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString(), prefixoTC);
            Extensions.CriarNoNotNull(doc, nodeInfPedidoCancelamento, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie, prefixoTC);
            Extensions.CriarNoNotNull(doc, nodeInfPedidoCancelamento, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString(), prefixoTC);

            #endregion fim - InfPedidoCancelamento

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

            Extensions.CriarNo(doc, nodePedido, "CodigoCancelamento", motivoAux, prefixoTC);
            Extensions.CriarNo(doc, nodePedido, "MotivoCancelamentoNfse", motivo.ToLower().Trim(), prefixoTC);
            
            #endregion fim - Pedido

            return doc;
        }

        #endregion
    }
}
