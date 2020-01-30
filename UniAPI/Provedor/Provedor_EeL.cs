using UniAPI.Domain;
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
    internal class Provedor_EeL : AbstractProvedor, IProvedor
    {
        internal Provedor_EeL()
        {
            this.Nome = EnumProvedor.EeL;
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
            if (nota.Provedor.Nome != EnumProvedor.EeL)
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
                                            case "nfserpsresposta":
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                            case "loterpsresposta":
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;

                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "numeroprotocolo":
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
                                            case "mensagens":
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
                                                    long.TryParse(numeroRPS.Substring(4), out numeroLote);
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
                                            case "mensagens":
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
                                            case "mensagens":
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
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "mensagens")
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
            var CabLoteRps = CriaHeaderXml("LoteRps", ref doc);

            #region "tcLoteRps"                   
            Extensions.CriarNoNotNull(doc, CabLoteRps, "Id", CompletaTextoLeft("0",
                                        GerarHashFNV(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString()).ToString(), 15));
            Extensions.CriarNoNotNull(doc, CabLoteRps, "NumeroLote", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, CabLoteRps, "QuantidadeRps", "1");

            #region "IdentificacaoPrestador -> TcIdentificacaoPrestador"
            long _FInscricaoMunicipal;
            var IdentificacaoPrestadorNode = Extensions.CriarNo(doc, CabLoteRps, "IdentificacaoPrestador");
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "CpfCnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "IndicacaoCpfCnpj", "2");//???????????
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));
            #endregion IdentificacaoPrestador

            var Listarps = Extensions.CriarNo(doc, CabLoteRps, "ListaRps");
            #region "TcRps=>TcInfRps"
            var ListarpsNode = Extensions.CriarNo(doc, Listarps, "Rps");

            Extensions.CriarNoNotNull(doc, ListarpsNode, "Id", CompletaTextoLeft("0",
                                        GerarHashFNV(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString()).ToString(), 15));

            //Código para identificação do local de prestação do serviço
            //1 - Fora do município
            //2 - No município 
            //Identificação de retenção
            //1 - Normal
            //2 - Recolhido na Fonte

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio != nota.Documento.TDFe.TServico.FMunicipioIncidencia)
            {
                Extensions.CriarNoNotNull(doc, ListarpsNode, "LocalPrestacao", "2");
                Extensions.CriarNoNotNull(doc, ListarpsNode, "IssRetido", "2");
            }
            else
            {
                Extensions.CriarNoNotNull(doc, ListarpsNode, "LocalPrestacao", "1");
                Extensions.CriarNoNotNull(doc, ListarpsNode, "IssRetido", "1");
            }

            Extensions.CriarNoNotNull(doc, ListarpsNode, "DataEmissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("s"));

            #region "TcIdentificacaoRps"
            var IdentificacaoRpsNode = Extensions.CriarNo(doc, ListarpsNode, "IdentificacaoRps");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy") +
                                                        Generico.RetornarNumeroZerosEsquerda(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, 7));
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());
            #endregion

            #region "Prestador -> TcIdentificacaoPrestador"
            var PrestadorNode = Extensions.CriarNo(doc, ListarpsNode, "DadosPrestador");

            #region "IdentificacaoPrestador -> TcIdentificacaoPrestador"
            IdentificacaoPrestadorNode = Extensions.CriarNo(doc, PrestadorNode, "IdentificacaoPrestador");
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "CpfCnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "IndicacaoCpfCnpj", "2");//???????????
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IdentificacaoPrestadorNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));
            #endregion IdentificacaoPrestador

            Extensions.CriarNoNotNull(doc, PrestadorNode, "RazaoSocial", nota.Documento.TDFe.TPrestador.FRazaoSocial);
            Extensions.CriarNoNotNull(doc, PrestadorNode, "NomeFantasia", nota.Documento.TDFe.TPrestador.FNomeFantasia);
            Extensions.CriarNoNotNull(doc, PrestadorNode, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNoNotNull(doc, PrestadorNode, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, PrestadorNode, "NaturezaOperacao", tsNaturezaOperacao(nota));
            Extensions.CriarNoNotNull(doc, PrestadorNode, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());

            #region "Prestador -> Endereco"
            var PrestadorEnderecoNode = Extensions.CriarNo(doc, PrestadorNode, "Endereco");
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "LogradouroTipo", "");
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "Logradouro", nota.Documento.TDFe.TPrestador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "LogradouroNumero", nota.Documento.TDFe.TPrestador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "LogradouroComplemento", nota.Documento.TDFe.TPrestador.TEndereco.FComplemento);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "Bairro", nota.Documento.TDFe.TPrestador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "Municipio", nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "Uf", nota.Documento.TDFe.TPrestador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, PrestadorEnderecoNode, "Cep", nota.Documento.TDFe.TPrestador.TEndereco.FCEP);
            #endregion "Prestador -> Endereco"

            #region "Prestador -> contato"
            var PrestadorContatoNode = Extensions.CriarNo(doc, PrestadorNode, "Contato");
            Extensions.CriarNoNotNull(doc, PrestadorContatoNode, "Telefone", nota.Documento.TDFe.TPrestador.TContato.FDDD + nota.Documento.TDFe.TPrestador.TContato.FFone);
            Extensions.CriarNoNotNull(doc, PrestadorContatoNode, "Email", nota.Documento.TDFe.TPrestador.TContato.FEmail);
            #endregion "Prestador -> contato"

            #endregion "Prestador -> TcIdentificacaoPrestador"

            #region "Tomador -> TcIdentificacaoTomador"
            var TomadorNode = Extensions.CriarNo(doc, ListarpsNode, "DadosTomador");

            #region "IdentificacaoTomador -> TcIdentificacaoTomador"
            var IdentificacaoTomadorNode = Extensions.CriarNo(doc, TomadorNode, "IdentificacaoTomador");
            Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "CpfCnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "IndicacaoCpfCnpj", "2");//???????????
            long.TryParse(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));

            long _FInscricaoEstadual;
            long.TryParse(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual, out _FInscricaoEstadual);
            Extensions.CriarNoNotNull(doc, IdentificacaoTomadorNode, "InscricaoEstadual", _FInscricaoEstadual.ToString("d13"));
            #endregion IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, TomadorNode, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial);
            Extensions.CriarNoNotNull(doc, TomadorNode, "NomeFantasia", nota.Documento.TDFe.TTomador.FNomeFantasia);

            #region "Tomador -> Endereco"
            var TomadorEnderecoNode = Extensions.CriarNo(doc, TomadorNode, "Endereco");
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "LogradouroTipo", "");
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Logradouro", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "LogradouroNumero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "LogradouroComplemento", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Municipio", nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, TomadorEnderecoNode, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            #endregion "Prestador -> Endereco"

            #region "Tomador -> contato"
            var TomadorContatoNode = Extensions.CriarNo(doc, TomadorNode, "Contato");
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Telefone", nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone);
            Extensions.CriarNoNotNull(doc, TomadorContatoNode, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            #endregion "Tomador -> contato"

            #endregion "Tomador -> TcIdentificacaoTomador"

            #region "Servico -> TcDadosServico"

            var ServicosNode = Extensions.CriarNo(doc, ListarpsNode, "Servicos");

            foreach (var x in nota.Documento.TDFe.TServico.TItemServico)
            {
                var itemNode = Extensions.CriarNo(doc, ServicosNode, "Servico");
                Extensions.CriarNo(doc, itemNode, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);
                Extensions.CriarNo(doc, itemNode, "CodigoServico116", x.FCodServ);
                Extensions.CriarNo(doc, itemNode, "CodigoServicoMunicipal", x.FCodLCServ);
                Extensions.CriarNo(doc, itemNode, "Quantidade", FormataValor(x.FQuantidade));
                Extensions.CriarNo(doc, itemNode, "Unidade", x.FUnidade);
                Extensions.CriarNo(doc, itemNode, "Descricao", x.FDescricao);
                Extensions.CriarNo(doc, itemNode, "Aliquota", FormataValor(x.FAliquota, 2));
                
                Extensions.CriarNo(doc, itemNode, "ValorServico", FormataValor(x.FValorUnitario, 2));
                Extensions.CriarNo(doc, itemNode, "ValorIssqn", FormataValor(x.FValorIss, 2));
                Extensions.CriarNo(doc, itemNode, "ValorDesconto", FormataValor(x.FDescontoCondicionado, 2));
                //NumeroAlvara
            }

            #endregion Servico
            #region "tcValores"
            var ServicoValoresNode = Extensions.CriarNo(doc, ListarpsNode, "Valores");
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorOutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse, 2));
            Extensions.CriarNoNotNull(doc, ServicoValoresNode, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2));
            //Extensions.CriarNoNotNull(doc, ServicoValoresNode, "OutrosDescontos", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));//????
            #endregion tcValores           


            #endregion "TcRps=>TcInfRps"

            #endregion "tcLoteRps"   

            Extensions.CriarNoNotNull(doc, ListarpsNode, "Observacao", "");
            Extensions.CriarNoNotNull(doc, ListarpsNode, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarNfseRpsEnvio", ref doc);

            var IdentificacaoRpsNode = Extensions.CriarNo(doc, gerarNotaNode, "IdentificacaoRps");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Numero", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy") +
                                                        Generico.RetornarNumeroZerosEsquerda(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, 7));
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "Tipo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FTipo.ToString());

            var PrestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");
            Extensions.CriarNoNotNull(doc, PrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, PrestadorNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));
            doc.AppendChild(gerarNotaNode);
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {

            var doc = new XmlDocument();
            var nodeGerarConsulta = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeGerarConsulta, "Prestador");

            Extensions.CriarNoNotNull(doc, nodePrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion FIM - Prestador

            Extensions.CriarNoNotNull(doc, nodeGerarConsulta, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;

        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);

            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido", "", "xmlns", "http://www.abrasf.org.br/ABRASF/arquivos/nfse.xsd");

            #region "InfPedidoCancelamento"
            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento","", "id", "C" + numeroNFSe);
            
            #region "tcIdentificacaoNfse"
            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);
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

            var nsAttributeXmlns = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeXmlns.Value = "http://www.el.com.br/nfse/xsd/el-nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttributeXmlns);

            var nsAttributeXsi = doc.CreateAttribute("xmlns:xsi", "http://www.w3.org/2000/xmlns/");
            nsAttributeXsi.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttributeXsi);

            var nsAttributeXsd = doc.CreateAttribute("xmlns:xsd", "http://www.w3.org/2000/xmlns/");
            nsAttributeXsd.Value = "http://www.w3.org/2001/XMLSchema";
            gerarNotaNode.Attributes.Append(nsAttributeXsd);

            var nsAttributeXsiSchema = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            nsAttributeXsiSchema.Value = "http://www.el.com.br/nfse/xsd/el-nfse.xsd el-nfse.xsd";
            gerarNotaNode.Attributes.Append(nsAttributeXsiSchema);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;

        }


        private static uint GerarHashFNV(string valor)
        {

            const uint fnv_prime = 0x01000193;
            const uint FnvOffsetBasis = 0x811C9DC5;
            int HashSizeValue = 32;

            uint hash = FnvOffsetBasis; //base

            for (int i = 0; i < valor.Length; i++)
            {
                hash ^= ((byte)valor[(int)i]);
                hash *= fnv_prime;
            }

            return hash;
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

            if (retorno.Equals("1")) {
                if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
                {
                    retorno = "2";
                }
            }

            return retorno;

        }

        public static string CompletaTextoLeft(string valor, string texto, int tamanho)
        {
            string textoAux = string.Empty;
            string textoRet = string.Empty;

            int round = tamanho - texto.Length;

            for (int i = 0; i <= round; i++)
            {
                textoAux += valor;
            }

            if (!string.IsNullOrEmpty(textoAux))
            {
                textoRet = string.Concat(textoAux, texto);
                return textoRet.Substring(textoRet.Length - tamanho, tamanho);
            }

            return texto;
        }
    }
}
