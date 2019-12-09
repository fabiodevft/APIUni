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
    internal class Provedor_SuperNova: AbstractProvedor, IProvedor
    {

        internal Provedor_SuperNova()
        {
            this.Nome = EnumProvedor.SuperNova;
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

        #endregion

        private static string ImpostoRetido(EnumNFSeSituacaoTributaria situacao, int tipo = 0)
        {
            var tipoRecolhimento = "2";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "1";
            }

            return tipoRecolhimento;
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

        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.SuperNova)
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
                                                _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                           case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "erro": //Resposta do envio da RPS
                                                area = EnumArea.Erro;
                                                break;
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
                                                    numeroNF = Generico.RetornarApenasNumeros(numeroNF);
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    numeroRPS = x.ReadString();
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
                                            case "identificacaorps":
                                                bIdentificacaoRPS = true;
                                                break;
                                        }
                                        break;
                                    }

                                case EnumResposta.ConsultarLoteRpsResposta:
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
                                                    numeroNF = Generico.RetornarApenasNumeros(numeroNF);
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    if (bIdentificacaoRPS)
                                                    {
                                                        numeroRPS = x.ReadString();
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
                                                situacaoRPS = "C";
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
                                            case "identificacaorps":
                                                bIdentificacaoRPS = true;
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
                                            case "numeronfse":
                                                if (numeroNF.Equals(""))
                                                {
                                                    numeroNF = x.ReadString();
                                                    numeroNF = Generico.RetornarApenasNumeros(numeroNF);
                                                }
                                                else if (numeroRPS.Equals(""))
                                                {
                                                    if (bIdentificacaoRPS)
                                                    {
                                                        numeroRPS = x.ReadString();
                                                        bIdentificacaoRPS = false;
                                                    }
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
                CodigoRetornoPref = codigoErroOuAlerta

            };
        }


        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region EnviarLoteRpsEnvio
            var enviarLoteRpsEnvio = CriaHeaderXml("EnviarLoteRpsEnvio", ref doc);

            #region LoteRps

            var cabLoteRps = Extensions.CriarNo(doc, enviarLoteRpsEnvio, "LoteRps", "", "versao", "2.01");

            var vsAttribute = doc.CreateAttribute("Id");
            //vsAttribute.Value = string.Concat("ID", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            vsAttribute.Value = nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString().Trim();
            cabLoteRps.Attributes.Append(vsAttribute);

            Extensions.CriarNoNotNull(doc, cabLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            var nodeCpfCnpj = Extensions.CriarNo(doc, cabLoteRps, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, nodeCpfCnpj, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, cabLoteRps, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, cabLoteRps, "QuantidadeRps", "1");

            #region ListaRps
            var listarps = Extensions.CriarNo(doc, cabLoteRps, "ListaRps");

            #region Rps
            var rpsNode = Extensions.CriarNo(doc, listarps, "Rps");
            

            #region InfDeclaracaoPrestacaoServico
            var infDeclaracaoPrestacaoServico = Extensions.CriarNo(doc, rpsNode, "InfDeclaracaoPrestacaoServico");
            vsAttribute = doc.CreateAttribute("Id");
            //vsAttribute.Value = string.Concat("R", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            vsAttribute.Value = nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString().Trim();
            infDeclaracaoPrestacaoServico.Attributes.Append(vsAttribute);

            #region Rps Info
            var infoRps = Extensions.CriarNo(doc, infDeclaracaoPrestacaoServico, "Rps");
            vsAttribute = doc.CreateAttribute("Id");
            vsAttribute.Value = nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString().Trim();
            infoRps.Attributes.Append(vsAttribute);

            #region IdentificacaoRps
            var identificacaoRpsNode = Extensions.CriarNo(doc, infoRps, "IdentificacaoRps");

            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, identificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            #endregion FIM - IdentificacaoRps

            Extensions.CriarNoNotNull(doc, infoRps, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNoNotNull(doc, infoRps, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            #endregion FIM - Rps Info

            Extensions.CriarNoNotNull(doc, infDeclaracaoPrestacaoServico, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            #region Serviço
            var servicoNode = Extensions.CriarNo(doc, infDeclaracaoPrestacaoServico, "Servico");

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
            Extensions.CriarNoNotNull(doc, servicoValoresNode, "Aliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota) : "0.0000");

            #endregion FIM - Valores

            Extensions.CriarNoNotNull(doc, servicoNode, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, servicoNode, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico.ToString().Replace(".", ""));
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio?.ToString()??"");
            Extensions.CriarNoNotNull(doc, servicoNode, "Discriminacao", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, servicoNode, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, servicoNode, "ExigibilidadeISS", nota.Documento.TDFe.TServico.FExigibilidadeISS.ToString());
            Extensions.CriarNoNotNull(doc, servicoNode, "MunicipioIncidencia", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            #endregion FIM - Serviço

            #region Prestador

            var prestadorNode = Extensions.CriarNo(doc, infDeclaracaoPrestacaoServico, "Prestador");
            var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, prestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, prestadorNode, "Senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            Extensions.CriarNoNotNull(doc, prestadorNode, "FraseSecreta", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta);
            //Extensions.CriarNoNotNull(doc, prestadorNode, "FraseSecreta", "vinclercontabilidade");

            #endregion FIM - Prestador

            #region Tomador

            var tomadorNode = Extensions.CriarNo(doc, infDeclaracaoPrestacaoServico, "Tomador");

            #region "IdentificacaoTomador"
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

            Extensions.CriarNoNotNull(doc, identificacaoTomadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);

            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, tomadorNode, "RazaoSocial", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));

            #region Endereco
            var tomadorEnderecoNode = Extensions.CriarNo(doc, tomadorNode, "Endereco");

            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "CodigoPais", "1058"); //FIXO BRASIL
            Extensions.CriarNoNotNull(doc, tomadorEnderecoNode, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato
            var tomadorContatoNode = Extensions.CriarNo(doc, tomadorNode, "Contato");

            Extensions.CriarNoNotNull(doc, tomadorContatoNode, "Telefone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNoNotNull(doc, tomadorContatoNode, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion FIM - Tomador

            Extensions.CriarNoNotNull(doc, infDeclaracaoPrestacaoServico, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, infDeclaracaoPrestacaoServico, "IncentivoFiscal", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
                       
            //Producao: Sim/Não ->  1 – Sim  2 – Não
            Extensions.CriarNoNotNull(doc, infDeclaracaoPrestacaoServico, "Producao", nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao ? "2" : "1");

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
            var consultarLoteRpsEnvio = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            #region Prestador

            var prestadorNode = Extensions.CriarNo(doc, consultarLoteRpsEnvio, "Prestador");
            var CPFCNPJPrestadorNode = Extensions.CriarNo(doc, prestadorNode, "CpfCnpj");
            Extensions.CriarNoNotNull(doc, CPFCNPJPrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, prestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, prestadorNode, "Senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            Extensions.CriarNoNotNull(doc, prestadorNode, "FraseSecreta", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta);
            //Extensions.CriarNoNotNull(doc, prestadorNode, "FraseSecreta", "vinclercontabilidade");

            #endregion FIM - Prestador

            Extensions.CriarNoNotNull(doc, consultarLoteRpsEnvio, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");
            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            
            var doc = new XmlDocument();

            numeroNFSe = Convert.ToInt32(numeroNFSe).ToString();

            //return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));

            #region CancelarNfseEnvio
            var cancelarNfseEnvio = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            #region Pedido
            var pedidoNode = Extensions.CriarNo(doc, cancelarNfseEnvio, "Pedido");

            #region InfPedidoCancelamento
            var infPedidoCancelamentoNode = Extensions.CriarNo(doc, pedidoNode, "InfPedidoCancelamento", "", "Id", "ID" + nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);

            #region IdentificacaoNfse
            var identificacaoNfseNode = Extensions.CriarNo(doc, infPedidoCancelamentoNode, "IdentificacaoNfse");
            Extensions.CriarNo(doc, identificacaoNfseNode, "Numero", nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao ? "T"+ Generico.RetornarNumeroZerosEsquerda(numeroNFSe.Trim(), 7) : Generico.RetornarNumeroZerosEsquerda(numeroNFSe.Trim(), 8));

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

            Extensions.CriarNo(doc, infPedidoCancelamentoNode, "CodigoCancelamento", motivoAux); 

            #endregion FIM - InfPedidoCancelamento

            #region Signature

            #endregion FIM - Signature

            #endregion FIM - Pedido

            #endregion FIM - CancelarNfseEnvio

            return doc;
        }


        /// <summary>
        /// Cria o documento xml e retorna a TAG principal
        /// /summary>
        /// <param name="strNomeMetodo">Ex.: ConsultarNfseRpsEnvio</param>
        /// <param name="doc">Referencia do objeto que será o documento</param>
        /// <returns>retorna o node principal</returns>
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.abrasf.org.br/nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }


    }
}
