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
    internal class Provedor_PRONIM : AbstractProvedor, IProvedor
    {
        internal Provedor_PRONIM()
        {
            this.Nome = EnumProvedor.PRONIM;
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
            //nsAttributeTipos.Value = "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd";
            //gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.PRONIM)
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
                                        switch (x.Name.ToString().ToLower())
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
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "nfsecancelamento":
                                            case "cancelamento":
                                                cancelamento = true;
                                                break;
                                            case "datahora":
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
            var doc = new XmlDocument();

            string versao = string.Empty;
            
            switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                case "4303004":
                    versao = "2.02";
                    break;

                case "4118501":
                    versao = "2.03";
                    break;

                default:
                    versao = "2.02";
                    break;
            }

            #region EnviarLoteRpsEnvio
            var gerarNotaNode = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);

            #region LoteRps
            var nodeLoteRps = Extensions.CriarNo(doc, gerarNotaNode, "LoteRps", "", "Id", "LOTE" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            var vsAttribute = doc.CreateAttribute("versao");
            vsAttribute.Value = versao;
            nodeLoteRps.Attributes.Append(vsAttribute);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            #region CpfCnpj
            var CPFCNPJnota = Extensions.CriarNo(doc, nodeLoteRps, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJnota, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            Extensions.CriarNoNotNull(doc, nodeLoteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "QuantidadeRps", "1");

            #region ListaRps
            var nodeListaRps = Extensions.CriarNo(doc, nodeLoteRps, "ListaRps");

            #region RpsListaRps
            var nodeRpsLista = Extensions.CriarNo(doc, nodeListaRps, "Rps");

            #region InfDeclaracaoPrestacaoServico
            var nodeInfDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, nodeRpsLista, "InfDeclaracaoPrestacaoServico");

            vsAttribute = doc.CreateAttribute("Id");
            vsAttribute.Value = "RPS" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero + nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie;
            nodeInfDeclaracaoPrestacaoServico.Attributes.Append(vsAttribute);

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
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 4) : "0.00");
            //Extensions.CriarNoNotNull(doc, nodeValores, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio ?? ""));
            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FMunicipioIncidencia);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoPais", "1058");
            Extensions.CriarNoNotNull(doc, nodeServico, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
                       
            Extensions.CriarNoNotNull(doc, nodeServico, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FMunicipioIncidencia);
           
            #endregion Servico

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Prestador");

            #region CpfCnpj
            var CPFCNPJPrestador = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

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

            //Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "InscricaoMunicipal", Generico.RetornaApenasLetrasNumeros(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal));
            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, nodeTomador, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region Endereco
            var nodeEnderecoTomador = Extensions.CriarNo(doc, nodeTomador, "Endereco");

            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nodeEnderecoTomador, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato
            var nodeTomadorContato = Extensions.CriarNo(doc, nodeTomador, "Contato");

            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());

            #endregion FIM - InfDeclaracaoPrestacaoServico

            #endregion FIM - RpsListaRps

            #endregion FIM - ListaRps   

            #endregion FIM - LoteRps

            #endregion EnviarLoteRpsEnvio

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            var PrestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");
           
            #region CpfCnpj
            var CPFCNPJnota = Extensions.CriarNo(doc, PrestadorNode, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJnota, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj


            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            doc.AppendChild(gerarNotaNode);

            Extensions.CriarNoNotNull(doc, gerarNotaNode, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido", "");

            #region "InfPedidoCancelamento"
            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento", "", "Id",
                                                        "pedidoCancelamento_" +
                                                         nota.Documento.TDFe.TPrestador.FCnpj +
                                                         nota.Documento.TDFe.TPrestador.FInscricaoMunicipal +
                                                         nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            #region "tcIdentificacaoNfse"

            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);

            #region CpfCnpj
            var CPFCNPJnota = Extensions.CriarNo(doc, IdentificacaoNfseNode, "CpfCnpj", "");
            Extensions.CriarNoNotNull(doc, CPFCNPJnota, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            #endregion FIM - CpfCnpj

            long _FInscricaoMunicipal;

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", nota?.Documento?.TDFe?.TPrestador?.FInscricaoMunicipal?.ToString().Trim() ?? "");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
            #endregion "tcIdentificacaoNfse"

            Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "CodigoCancelamento", "2"); // tsCodigoCancelamentoNfse

            #endregion "InfPedidoCancelamento"
            return doc;
        }

    }
}
