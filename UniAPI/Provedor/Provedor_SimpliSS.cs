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
    internal class Provedor_SimpliSS : AbstractProvedor, IProvedor
    {

        internal Provedor_SimpliSS()
        {
            this.Nome = EnumProvedor.Simpliss;
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
        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.Simpliss)
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
                                            case "numerolote":
                                                long.TryParse(x.ReadString(), out numeroLote);
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

            #region GerarNovaNfseEnvio           
            var gerarNfse = CriaHeaderXml("GerarNovaNfseEnvio", ref doc);

            #region Prestador
            var nodePrestador = Extensions.CriarNo(doc, gerarNfse, "Prestador", "", "id", "ID" + nota.Documento.TDFe.Tide.FNumeroLote.ToString());

            Extensions.CriarNoNotNull(doc, nodePrestador, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodePrestador, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);

            #endregion

            #region InformacaoNfse
            var nodeInformacaoNfse = Extensions.CriarNo(doc, gerarNfse, "InformacaoNfse", "", "id", "ID" + nota.Documento.TDFe.Tide.FNumeroLote.ToString());

            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "NaturezaOperacao", tsNaturezaOperacao(nota));
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "RegimeEspecialTributacao", nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString());
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "OptanteSimplesNacional", nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString());
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "IncentivadorCultural", nota.Documento.TDFe.Tide.FIncentivadorCultural.ToString());
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "Status", ((int)nota.Documento.TDFe.Tide.FStatus).ToString());
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "Competencia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNoNotNull(doc, nodeInformacaoNfse, "OutrasInformacoes", "");

            #region Servico
            var nodeServico = Extensions.CriarNo(doc, nodeInformacaoNfse, "Servico");

            #region Valores
            var nodeValores = Extensions.CriarNo(doc, nodeServico, "Valores");

            Extensions.CriarNoNotNull(doc, nodeValores, "ValorServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorPis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIr", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "IssRetido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorIssRetido", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "OutrasRetencoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FOutrasRetencoes, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "BaseCalculo", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "Aliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "ValorLiquidoNfse", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoIncondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoIncondicionado, 2));
            Extensions.CriarNoNotNull(doc, nodeValores, "DescontoCondicionado", FormataValor(nota.Documento.TDFe.TServico.FValores.FDescontoCondicionado, 2));

            #endregion fim - Valores

            Extensions.CriarNoNotNull(doc, nodeServico, "ItemListaServico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoCnae", nota.Documento.TDFe.TServico.FCodigoCnae);
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoTributacaoMunicipio", nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeServico, "Discriminacao", nota.Documento.TDFe.TServico.FDiscriminacao);            
            Extensions.CriarNoNotNull(doc, nodeServico, "CodigoMunicipio", nota.Documento.TDFe.TServico.FCodigoMunicipio);

            XmlNode ItemServicoNode; 
            foreach (var oServico in nota.Documento.TDFe.TServico.TItemServico)
            {
                ItemServicoNode  = Extensions.CriarNo(doc, nodeServico, "ItensServico");
                Extensions.CriarNoNotNull(doc, ItemServicoNode, "Descricao", Generico.TratarString(oServico.FDescricao));
                Extensions.CriarNoNotNull(doc, ItemServicoNode, "Quantidade", FormataValor(oServico.FQuantidade));
                Extensions.CriarNoNotNull(doc, ItemServicoNode, "ValorUnitario", FormataValor(oServico.FValorUnitario, 2));
            }

            #endregion fim - Servico

            #region Tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeInformacaoNfse, "Tomador");

            #region IdentificacaoTomador

            var nodeIdentificacaoTomador = Extensions.CriarNo(doc, nodeTomador, "IdentificacaoTomador");

            var CPFCNPJTomadorNode = Extensions.CriarNo(doc, nodeIdentificacaoTomador, "CpfCnpj");
            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomadorNode, "Cpf", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }
            else
            {
                Extensions.CriarNoNotNull(doc, CPFCNPJTomadorNode, "Cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            }

            #endregion FIM - IdentificacaoTomador

            Extensions.CriarNoNotNull(doc, nodeTomador, "RazaoSocial", nota.Documento.TDFe.TTomador.FRazaoSocial);

            #region Endereco

            var Endereco = Extensions.CriarNo(doc, nodeTomador, "Endereco");

            Extensions.CriarNoNotNull(doc, Endereco, "Endereco", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNoNotNull(doc, Endereco, "Numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, Endereco, "Bairro", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
            Extensions.CriarNoNotNull(doc, Endereco, "CodigoMunicipio", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, Endereco, "Uf", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, Endereco, "Cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            #endregion FIM - Endereco

            #region Contato

            var nodeContato = Extensions.CriarNo(doc, nodeTomador, "Contato");

            Extensions.CriarNoNotNull(doc, nodeContato, "Telefone", nota.Documento.TDFe.TTomador.TContato.FFone);
            Extensions.CriarNoNotNull(doc, nodeContato, "Email", nota.Documento.TDFe.TTomador.TContato.FEmail);

            #endregion FIM - Contato

            #endregion fim - Tomador

            #endregion fim - InformacaoNfse

            #endregion fim - GerarNovaNfseEnvio

            return doc;
        }
        
        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("ConsultarLoteRpsEnvio", ref doc);
            
            var PrestadorNode = Extensions.CriarNo(doc, gerarNotaNode, "Prestador");
            Extensions.CriarNoNotNull(doc, PrestadorNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, PrestadorNode, "InscricaoMunicipal", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            
            Extensions.CriarNoNotNull(doc, gerarNotaNode, "Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("CancelarNfseEnvio", ref doc);
                        
            var PedidoNode = Extensions.CriarNo(doc, gerarNotaNode, "Pedido");

            #region InfPedidoCancelamento

            var InfPedidoCancelamentoNode = Extensions.CriarNo(doc, PedidoNode, "InfPedidoCancelamento", "", "id", "ID" + numeroNFSe);
           
            
            #region "tcIdentificacaoNfse"

            var IdentificacaoNfseNode = Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "IdentificacaoNfse");

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Numero", numeroNFSe);

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "Cnpj", nota.Documento.TDFe.TPrestador.FCnpj);

            long _FInscricaoMunicipal;
            long.TryParse(nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, out _FInscricaoMunicipal);
            Extensions.CriarNo(doc, IdentificacaoNfseNode, "InscricaoMunicipal", _FInscricaoMunicipal.ToString("d13"));

            Extensions.CriarNo(doc, IdentificacaoNfseNode, "CodigoMunicipio", nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio);

            #endregion FIM - InfPedidoCancelamento

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
            Extensions.CriarNo(doc, InfPedidoCancelamentoNode, "CodigoCancelamento", nota.Documento.FCodigoCancelamento.ToString());  

            #endregion "InfPedidoCancelamento"
            return doc;
        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://www.sistema.com.br/Nfse/arquivos/nfse_3.xsd";
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
