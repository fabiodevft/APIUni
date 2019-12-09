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
    internal class Provedor_VersaTecnologia : AbstractProvedor, IProvedor
    {

        internal Provedor_VersaTecnologia()
        {
            this.Nome = EnumProvedor.VersaTecnologia;
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
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc, EnumAmbiente ambiente)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");

            if (ambiente == EnumAmbiente.Homologacao)
            {
                nsAttributeTipos.Value = "http://homologacaouba.versatecnologia.com.br/schema/nfse_v201.xsd";
            }
            else
            {
                nsAttributeTipos.Value = "http://nfe.uba.mg.gov.br/schema/nfse_v201.xsd";
            }
                                  
            gerarNotaNode.Attributes.Append(nsAttributeTipos);


            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.VersaTecnologia)
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
            bool bRPS = false;

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
                                            case "gerarnfseresposta":
                                            case "consultarnfserpsresposta":
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
                                            case "status":
                                                var resultado = x.ReadString();
                                                if (resultado.Equals("2"))
                                                {
                                                    sucesso = true;
                                                    cancelamento = true;
                                                    situacaoRPS = "C";
                                                }

                                                break;
                                            case "numero":
                                                if (numeroNF.Equals(""))
                                                {
                                                    numeroNF = x.ReadString();
                                                }
                                                else if (numeroRPS.Equals("") && bRPS == true)
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
                                            case "identificacaorps":
                                                bRPS = true;
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
                                            case "datahora":
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

            #region GerarNfseEnvio
            var gerarNfseEnvio = CriaHeaderXml("GerarNfseEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);

            #region Rps
            var rps = Extensions.CriarNo(doc, gerarNfseEnvio, "Rps");

            #region InfDeclaracaoPrestacaoServico
            var nodeInfDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, rps, "InfDeclaracaoPrestacaoServico");

            var vsAttribute = doc.CreateAttribute("Id");
            vsAttribute.Value = "InfDec_Rps_" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero;
            nodeInfDeclaracaoPrestacaoServico.Attributes.Append(vsAttribute);


            #region Rps
            var nodeRps = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Rps");

            var vsAttributeRps = doc.CreateAttribute("Id");
            vsAttributeRps.Value = "Rps_" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero;
            nodeRps.Attributes.Append(vsAttributeRps);

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, nodeRps, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, nodeRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNoNotNull(doc, nodeRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #endregion FIM - Rps

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            #region Servico
            var servicoNode = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Servico");

            #region Valores
            var servicoValoresNode = Extensions.CriarNo(doc, servicoNode, "Valores");

            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorDeducoes", nota.Documento.TDFe.TServico.FValores.FValorDeducoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorPis", nota.Documento.TDFe.TServico.FValores.FValorPis > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCofins", nota.Documento.TDFe.TServico.FValores.FValorCofins > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorInss", nota.Documento.TDFe.TServico.FValores.FValorInss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIr", nota.Documento.TDFe.TServico.FValores.FValorIr > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorCsll", nota.Documento.TDFe.TServico.FValores.FValorCsll > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "OutrasRetencoes", nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "ValorIss", nota.Documento.TDFe.TServico.FValores.FValorIss > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 4) : "0.0000");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoIncondicionado", nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2) : "0.00");
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "DescontoCondicionado", nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2) : "0.00");


            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, servicoNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, servicoNode, "ResponsavelRetencao", nota.Documento.TDFe.TServico.FResponsavelRetencao.ToString());
            Extensions.CriarNoNotNull(doc, servicoNode, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoCnae", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio));
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoTributacaoMunicipio", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio));
            Extensions.CriarNoNotNull(doc, servicoNode, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            //Extensions.CriarNoNotNull(doc, servicoNode, "CodigoPais", nota.Documento.TDFe.TServico.FCodigoPais.ToString());
            Extensions.CriarNoNotNull(doc, servicoNode, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
            Extensions.CriarNoNotNull(doc, servicoNode, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FMunicipioIncidencia);

            #endregion FIM - Servico

            #region Prestador    

            var prestadorNode = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "Prestador");
            var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, prestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            #region Tomador

            var Tomador = Extensions.CriarNo(doc, nodeInfDeclaracaoPrestacaoServico, "TomadorServico");

            #region IdentificacaoTomador

            var identificacaoTomador = Extensions.CriarNo(doc, Tomador, "IdentificacaoTomador");

            var CPFCNPJTomadorNode = Extensions.CriarNo(doc, identificacaoTomador, "CpfCnpj");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomadorNode, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomadorNode, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }


            Extensions.CriarNoNotNull(doc, identificacaoTomador, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);

            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, Tomador, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region Endereco

            var Endereco = Extensions.CriarNo(doc, Tomador, "Endereco");

            Extensions.CriarNoNotNull(doc, Endereco, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, Endereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, Endereco, "Complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, Endereco, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, Endereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, Endereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            //Extensions.CriarNoNotNull(doc, servicoNode, "CodigoPais", nota.Documento.TDFe.TServico.FCodigoPais.ToString());
            Extensions.CriarNoNotNull(doc, Endereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato

            var Contato = Extensions.CriarNo(doc, Tomador, "Contato");

            Extensions.CriarNoNotNull(doc, Contato, "Telefone", nota.Documento.TDFe.TTomador.TContato.FFone);
            Extensions.CriarNoNotNull(doc, Contato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao != 0 ? nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString() : "");
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, nodeInfDeclaracaoPrestacaoServico, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());

            #endregion FIM - InfDeclaracaoPrestacaoServico

            #region Signature
            #endregion FIM - Signature

            #endregion FIM - Rps

            #endregion FIM - GerarNfseEnvio

            #region Username/Password

            #endregion

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var consultarNfseRpsEnvio = CriaHeaderXml("ConsultarNfseRpsEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, consultarNfseRpsEnvio, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            #region Prestador    

            var prestadorNode = Extensions.CriarNo(doc, consultarNfseRpsEnvio, "Prestador");

            var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, prestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc, nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente);

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

    }
}