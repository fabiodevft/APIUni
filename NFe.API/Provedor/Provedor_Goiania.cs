using NFe.API.Domain;
using NFe.API.Enum;
using NFe.API.Provedor;
using NFe.API.Util;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace FRGDocFiscal.Provedor
{
    internal class Provedor_Goiania : AbstractProvedor, IProvedor
    {
        internal Provedor_Goiania()
        {
            this.Nome = EnumProvedor.Goiania;
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
            if (nota.Provedor.Nome != EnumProvedor.Goiania)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var sucesso = false;
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
                                            //case "consultarloterpsresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                            //case "consultarnfseresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarNfseResposta; break;
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "gerarnfseresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;

                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
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
                                            case "datahoracancelamento":
                                                protocolo = x.ReadString();
                                                situacaoRPS = "C";
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
                                //case EnumResposta.ConsultarLoteRpsResposta:
                                //    {
                                //        break;
                                //    }
                                case EnumResposta.CancelarNfseResposta:
                                    {

                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "datahoracancelamento":
                                                situacaoRPS = "C";
                                                sucesso = true;
                                                break;
                                            case "tsCodigoCancelamentoNfse":
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
                LinkImpressao = string.Concat("http://www2.goiania.go.gov.br/sistemas/snfse/asp/snfse00200w0.asp?",
                                              "inscricao=",nota?.Documento?.TDFe?.TPrestador?.FInscricaoMunicipal?.ToString().Trim() ?? "",
                                              "&nota=",numeroNF?.ToString().Trim() ?? "",
                                              "&verificador=",codigoVerificacao?.ToString().Trim() ?? ""),
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
            #region "GerarNfseEnvio"

            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("GerarNfseEnvio", ref doc);

            #region "Rps"

            var RpsNode = Extensions.CriarNo(doc, gerarNotaNode, "Rps");

            #region "tcInfDeclaracaoPrestacaoServico"

            var InfDeclaracaoPrestacaoServicoNode = Extensions.CriarNo(doc, RpsNode, "InfDeclaracaoPrestacaoServico");

            #region"Rps - tcInfRps"
            var RpsLoteNode = Extensions.CriarNo(doc, InfDeclaracaoPrestacaoServicoNode, "Rps");
            var vsAttribute = doc.CreateAttribute("Id");
            vsAttribute.Value = "rps" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero;

            RpsLoteNode.Attributes.Append(vsAttribute);

            #region"tcInfRps"

            #region "TcIdentificacaoRps"
            var IdentificacaoRpsNode = Extensions.CriarNo(doc, RpsLoteNode, "IdentificacaoRps");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());
            #endregion"TcIdentificacaoRps"
            Extensions.CriarNoNotNull(doc, RpsLoteNode, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));
            Extensions.CriarNoNotNull(doc, RpsLoteNode, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());
            #endregion"tcInfRps"

            //Extensions.CriarNoNotNull(doc, InfDeclaracaoPrestacaoServicoNode, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));

            #region"Servico -> tcDadosServico"
            var ServicoNode = Extensions.CriarNo(doc, InfDeclaracaoPrestacaoServicoNode, "Servico");
            #region "Valores -> tcValoresDeclaracaoServico"
            var ServicoValoresNode = Extensions.CriarNo(doc, ServicoNode, "Valores");
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes));
            //Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota) : "0");
            //Extensions.CriarNoNotNull(doc, ServicoValoresNode, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado));
            //Extensions.CriarNoNotNull(doc, ServicoValoresNode, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado));
            #endregion
            //Extensions.CriarNoNotNull(doc, ServicoNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            //Extensions.CriarNoNotNull(doc, ServicoNode, "ResponsavelRetencao", "1"); //Tomador Fixo
            //Extensions.CriarNoNotNull(doc, ServicoNode, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            //Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);
            Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio);
            Extensions.CriarNoNotNull(doc, ServicoNode, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoMunicipio", nota.Documento.TDFe.TServico._FMunicipioIncidenciaSedetec.ToString("d07"));
            Extensions.CriarNoNotNull(doc, ServicoNode, "CodigoPais", nota.Documento.TDFe.TServico.FCodigoPais.ToString());
            //Extensions.CriarNoNotNull(doc, ServicoNode, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
            //Extensions.CriarNoNotNull(doc, ServicoNode, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            //NumeroProcesso tsNumeroProcesso
            #endregion"Servico -> tcDadosServico"

            #region"Prestador -> tcIdentificacaoPrestador"
            var PrestadorNode = Extensions.CriarNo(doc, InfDeclaracaoPrestacaoServicoNode, "Prestador");

            var CPFCNPJPrestador = Extensions.CriarNo(doc, PrestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            #endregion"Prestador -> tcIdentificacaoPrestador"

            #region "Tomador -> tcDadosTomador"
            var TomadorNode = Extensions.CriarNo(doc, InfDeclaracaoPrestacaoServicoNode, "Tomador");
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


            Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);
            #endregion "TcIdentificacaoTomador"
            Extensions.CriarNoNotNull(doc, TomadorNode, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region "TcEndereco"
            var TomadorEnderecoNode = Extensions.CriarNo(doc, TomadorNode, "Endereco");
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco._FCodigoMunicipioSedetec.ToString("d07"));
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            #endregion "TcEndereco"

            #region "TcContato"
            var TomadorContatoNode = Extensions.CriarNo(doc, TomadorNode, "Contato");
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            #endregion "TcContato"

            #endregion "Tomador -> tcDadosTomador"


            Extensions.CriarNoNotNull(doc, InfDeclaracaoPrestacaoServicoNode, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            //Extensions.CriarNoNotNull(doc, InfDeclaracaoPrestacaoServicoNode, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            //Extensions.CriarNoNotNull(doc, InfDeclaracaoPrestacaoServicoNode, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            #endregion"Rps"

            #endregion "tcInfDeclaracaoPrestacaoServico"

            #endregion "RPS"
            #endregion "GerarNfseEnvio"
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarNfseRpsEnvio", ref doc);

            var IdentificacaoRpsNode = Extensions.CriarNo(doc, gerarNotaNode, "IdentificacaoRps");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #region"Prestador -> tcIdentificacaoPrestador"
            var PrestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");

            var CPFCNPJPrestador = Extensions.CriarNo(doc, PrestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            #endregion"Prestador -> tcIdentificacaoPrestador"


            doc.AppendChild(gerarNotaNode);
            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido", "", "xmlns", "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd");

            #region "InfPedidoCancelamento"
            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento", "", "id", "C" + numeroNFSe);
            #region "tcIdentificacaoNfse"

            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            
            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco._FCodigoMunicipioSedetec.ToString("d07"));
            #endregion "tcIdentificacaoNfse"

            Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "CodigoCancelamento", "2"); // tsCodigoCancelamentoNfse

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
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "iso-8859-1", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://nfse.goiania.go.gov.br/xsd/nfse_gyn_v02.xsd";
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
