using NFe.Full.API.Domain;
using NFe.Full.API.Enum;
using NFe.Full.API.Interface;
using NFe.Full.API.Provedor;
using NFe.Full.API.Util;
using System;
using System.IO;
using System.Xml;

namespace FRGDocFiscal.Provedor
{
    class Provedor_BSITBR : AbstractProvedor, IProvedor
    {
        internal Provedor_BSITBR()
        {
            this.Nome = EnumProvedor.BSITBR;
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
            if (nota.Provedor.Nome != EnumProvedor.BSITBR)
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
                                        switch (x.Name.ToString().ToLower().Replace("ns2:", ""))
                                        {
                                            case "cancelarnfseresposta": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "consultarloterpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "nfse": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "gerarnfseresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;

                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower().Replace("ns2:", ""))
                                        {
                                            case "protocolo":
                                                protocolo = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "codigoverificacao":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "dataemissao":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString().Replace("Z", ""), out emissao);

                                                dataEmissaoRPS = emissao;
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
                                case EnumResposta.ConsultarNfseRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower().Replace("ns2:", ""))
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
                                        switch (x.Name.ToString().ToLower().Replace("ns2:", ""))
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

            string[] prefixo = { "p", "http://www.abrasf.org.br/nfse.xsd"};

            var gerarNfseEnvio = CriaHeaderXml("GerarNfseEnvio", ref doc, "http://www.abrasf.org.br/nfse.xsd");

            #region GerarNfseEnvio
            
            var dsAttribute = doc.CreateAttribute("xmlns", "ds", "http://www.w3.org/2000/xmlns/");
            dsAttribute.Value = "http://www.w3.org/2000/09/xmldsig";
            gerarNfseEnvio.Attributes.Append(dsAttribute);

            var pAttribute = doc.CreateAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
            pAttribute.Value = "http://www.abrasf.org.br/nfse.xsd";
            gerarNfseEnvio.Attributes.Append(pAttribute);

            var xsiAttributeTipos = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            xsiAttributeTipos.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNfseEnvio.Attributes.Append(xsiAttributeTipos);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://www.abrasf.org.br/nfse.xsd nfse-v2.xsd ";
            gerarNfseEnvio.SetAttributeNode(schemaLocation);


            #region credenciais
            var nodeCredenciais = Extensions.CriarNo(doc, gerarNfseEnvio, "credenciais", "", prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "usuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario, prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha, prefixo);
            Extensions.CriarNoNotNull(doc, nodeCredenciais, "chavePrivada", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim(), prefixo);

            #endregion FIM - credenciais

            #region Rps
            var nodeRps = Extensions.CriarNo(doc, gerarNfseEnvio, "Rps", "", prefixo);

            #region InfDeclaracaoPrestacaoServico
            var nodeInfDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, nodeRps, "InfDeclaracaoPrestacaoServico", "", prefixo);

            #region Rps
            var nodeInfRps = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Rps", "", prefixo);

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, nodeInfRps, "IdentificacaoRps", "", prefixo);

            Extensions.CriarNo(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, prefixo);

            /* Suporta os valores R1, R2 e R3  
             * R1 = Recibo Provisório de Serviço  
             * R2 = Nota Fiscal Conjugada 
             * R3 = Cupom 
             * Para geração deve ser enviado sempre o valor R1. */
            Extensions.CriarNo(doc, nodeIdentificacaoRps, "Tipo", "R1", prefixo);

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNo(doc, nodeInfRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"), prefixo);
            
            /* Suporta os valores CO e CA 
             * CO = Convertida 
             * CA = Cancelada
             * Para geração deve ser enviado sempre o valor CO. */
            Extensions.CriarNo(doc, nodeInfRps, "Status", "CO", prefixo);

            #endregion FIM - Rps

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Servico", "", prefixo);

            #region Valores
            var nodeServicoValores = Extensions.CriarNo(doc, nodeServico, "Valores", "", prefixo);

            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll), prefixo);
            //Extensions.CriarNoNotNull(doc, nodeServicoValores, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100) : "0", prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado), prefixo);
            Extensions.CriarNoNotNull(doc, nodeServicoValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado), prefixo);

            #endregion FIM - Valores

            Extensions.CriarNo(doc, nodeServico, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico, prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae, prefixo);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio, prefixo);
            Extensions.CriarNo(doc, nodeServico, "Discriminacao", nota.Documento.TDFe.TServico.FDiscriminacao, prefixo);
            Extensions.CriarNo(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio, prefixo);
            Extensions.CriarNo(doc, nodeServico, "ExigibilidadeISS", tsNaturezaOperacao(nota), prefixo);

            #endregion FIM - Servico

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Prestador", "", prefixo);

            var CPFCNPJPrestador = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);            

            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);

            #endregion FIM - Prestador

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Tomador", "", prefixo);

            #region IdentificacaoTomador
            var identificacaoTomadorNode = Extensions.CriarNo(doc, nodeTomador, "IdentificacaoTomador", "", prefixo);
            var CPFCNPJTomador = Extensions.CriarNo(doc, identificacaoTomadorNode, "CpfCnpj", "", prefixo);
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, prefixo);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomador, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, prefixo);
            }
            Extensions.CriarNoNotNull(doc, identificacaoTomadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal, prefixo);

            #endregion FIM - IdentificacaoTomador
            Extensions.CriarNo(doc, nodeTomador, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial, prefixo);

            #region Endereco
            var nodeTomadorEndereco = Extensions.CriarNo(doc, nodeTomador, "Endereco", "", prefixo);

            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "TipoLogradouro", "", prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Logradouro", nota.Documento.TDFe.TTomador.TEndereco.FEndereco, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Complemento", nota.Documento.TDFe.TTomador.TEndereco.FComplemento, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF, prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorEndereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP, prefixo);

            #endregion FIM - Endereco

            #region Contato
            var nodeTomadorContato = Extensions.CriarNo(doc, nodeTomador, "Contato", "", prefixo);

            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FFone), prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Ddd", nota.Documento.TDFe.TTomador.TContato.FDDD.Length == 3 ? nota.Documento.TDFe.TTomador.TContato.FDDD : string.Concat("0", nota.Documento.TDFe.TTomador.TContato.FDDD), prefixo);
            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "TipoTelefone", "CO", prefixo); //FIXO TELEFONE COMERCIAL
            Extensions.CriarNoNotNull(doc, nodeTomadorContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail, prefixo);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            #endregion FIM - InfDeclaracaoPrestacaoServico

            #region Signature

            #endregion FIM - Signature

            #endregion FIM - Rps

            #endregion FIM - GerarNfseEnvio

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {

            var doc = new XmlDocument();

            string[] prefixo = { "p", "http://www.abrasf.org.br/nfse.xsd" };

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.AppendChild(docNode);

            #region ConsultarNfseRpsEnvio
            var gerarConsultarNfseRpsEnvio = doc.CreateElement("ConsultarNfseRpsEnvio", "");

            var dsAttribute = doc.CreateAttribute("xmlns", "ds", "http://localhost:8080/WsNFe2/lote");
            dsAttribute.Value = "http://www.w3.org/2000/09/xmldsig#";
            gerarConsultarNfseRpsEnvio.Attributes.Append(dsAttribute);

            var pAttribute = doc.CreateAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
            pAttribute.Value = "http://www.abrasf.org.br/nfse.xsd";
            gerarConsultarNfseRpsEnvio.Attributes.Append(pAttribute);

            var xsiAttributeTipos = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            xsiAttributeTipos.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarConsultarNfseRpsEnvio.Attributes.Append(xsiAttributeTipos);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://www.abrasf.org.br/nfse.xsd nfse-v2.xsd ";
            gerarConsultarNfseRpsEnvio.SetAttributeNode(schemaLocation);

            #region credenciais
            var nodeCredenciais = Extensions.CriarNo(doc, gerarConsultarNfseRpsEnvio, "credenciais", "", prefixo);

            Extensions.CriarNo(doc, nodeCredenciais, "usuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario, prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha, prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "chavePrivada", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim(), prefixo);

            #endregion FIM - credenciais

            #region IdentificacaoRps
            var nodeIdentificacaoRps = Extensions.CriarNo(doc, gerarConsultarNfseRpsEnvio, "IdentificacaoRps", "", prefixo);

            Extensions.CriarNo(doc, nodeIdentificacaoRps, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, prefixo);

            /* Suporta os valores R1, R2 e R3  
             * R1 = Recibo Provisório de Serviço  
             * R2 = Nota Fiscal Conjugada 
             * R3 = Cupom 
             * Para geração deve ser enviado sempre o valor R1. */
            Extensions.CriarNo(doc, nodeIdentificacaoRps, "Tipo", "R1", prefixo);

            #endregion FIM - IdentificacaoRps

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, gerarConsultarNfseRpsEnvio, "Prestador", "", prefixo);

            var CPFCNPJPrestador = Extensions.CriarNo(doc, nodePrestador, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);

            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);

            #endregion FIM - Prestador
            
            #endregion FIM - ConsultarNfseRpsEnvio

            return doc;            

        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            var doc = new XmlDocument();

            string[] prefixo = { "p", "http://www.abrasf.org.br/nfse.xsd" };

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.AppendChild(docNode);

            #region CancelarNfseEnvio
            var gerarCancelarNfseEnvio = doc.CreateElement("CancelarNfseEnvio", "");

            var dsAttribute = doc.CreateAttribute("xmlns", "ds", "http://localhost:8080/WsNFe2/lote");
            dsAttribute.Value = "http://www.w3.org/2000/09/xmldsig#";
            gerarCancelarNfseEnvio.Attributes.Append(dsAttribute);

            var pAttribute = doc.CreateAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
            pAttribute.Value = "http://www.abrasf.org.br/nfse.xsd";
            gerarCancelarNfseEnvio.Attributes.Append(pAttribute);

            var xsiAttributeTipos = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            xsiAttributeTipos.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarCancelarNfseEnvio.Attributes.Append(xsiAttributeTipos);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://www.abrasf.org.br/nfse.xsd nfse-v2.xsd ";
            gerarCancelarNfseEnvio.SetAttributeNode(schemaLocation);

            #region credenciais
            var nodeCredenciais = Extensions.CriarNo(doc, gerarCancelarNfseEnvio, "credenciais", "", prefixo);

            Extensions.CriarNo(doc, nodeCredenciais, "usuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario, prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha, prefixo);
            Extensions.CriarNo(doc, nodeCredenciais, "chavePrivada", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim(), prefixo);

            #endregion FIM - credenciais

            #region Pedido
            var nodePedido = Extensions.CriarNo(doc, gerarCancelarNfseEnvio, "Pedido", "", prefixo);

            #region InfPedidoCancelamento
            var nodeInfPedidoCancelamento = Extensions.CriarNo(doc, gerarCancelarNfseEnvio, "InfPedidoCancelamento", "", prefixo);

            #region IdentificacaoNfse
            var identificacaoNfseNode = Extensions.CriarNo(doc, nodeInfPedidoCancelamento, "IdentificacaoNfse", "", prefixo);

            Extensions.CriarNo(doc, identificacaoNfseNode, "Numero", numeroNFSe, prefixo);

            var CPFCNPJPrestador = Extensions.CriarNo(doc, identificacaoNfseNode, "CpfCnpj", "", prefixo);
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj, prefixo);

            Extensions.CriarNo(doc, identificacaoNfseNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, prefixo);
            Extensions.CriarNo(doc, identificacaoNfseNode, "CodigoVerificacao", codigoVerificacao, prefixo);

            #endregion FIM - IdentificacaoNfse

            /* Suporta os valores EE, ED, OU e SB
             * EE = Erro de Emissão   
             * ED = Erro de Digitação 
             * OU = Outros 
             * SB = Substituição */

            string motivoAux = "OU";

            if (motivo.ToLower().Trim() == "erro na emissão")
                motivoAux = "EE";
            
            Extensions.CriarNo(doc, nodeInfPedidoCancelamento, "CodigoCancelamento", motivoAux, prefixo);
            Extensions.CriarNo(doc, nodeInfPedidoCancelamento, "DescricaoCancelamento", motivo.ToLower().Trim(), prefixo);

            #endregion FIM - InfPedidoCancelamento

            #region Signature

            #endregion FIM - Signature

            #endregion FIM - Pedido
            
            #endregion FIM - CancelarNfseEnvio

            return doc;
        }
        
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
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

            return string.Concat("0", retorno);

        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc, string vlAtributo)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement("p", strNomeMetodo, "http://www.abrasf.org.br/nfse.xsd");
            
            //var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            //nsAttributeTipos.Value = vlAtributo;
            //gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

    }
}
