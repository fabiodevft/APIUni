using UniAPI.Domain;
using UniAPI.Enum;
using UniAPI.Interface;
using UniAPI.Provedor;
using UniAPI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;


namespace UniAPI.Provedor
{
    public class Provedor_IPM : AbstractProvedor, IProvedor
    {
        internal Provedor_IPM()
        {
            this.Nome = EnumProvedor.IPM;
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
                retorno = String.Format("{0:0.00}", valor).ToString().Replace(",", ".");
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

        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        private static string GetMunicipioIncidencia(NFSeNota nota)
        {
            string resposta = string.Empty;

            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia.Equals(nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio))
                resposta = nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI;
            else
                resposta = nota.Documento.TDFe.TTomador.TEndereco.FSIAFI;

            return resposta;
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

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.CARIOCA)
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
                                            //case "enviarloterpsresposta": //Resposta do envio da RPS
                                            //    _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            //case "cancelarnfseresposta": //CancelarRPS
                                            //    _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "retorno": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;

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
                                            case "codigo":
                                                if (x.ReadString().ToLower().Contains("sucesso"))
                                                {                                                    
                                                    sucesso = true;
                                                }
                                                else
                                                {
                                                    area = EnumArea.Erro;
                                                }
                                                                                                
                                                break;
                                            case "numero_nfse":
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

                                            case "data_nfse":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                            case "situacao_codigo_nfse":
                                                if (x.ReadString().Equals("2"))
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                           
                                            case "datahoracancelamento":
                                                if (cancelamento)
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
                                                }
                                                break;
                                            case "cod_verificador_autenticidade":
                                                codigoVerificacao = x.ReadString();
                                                break;
                                            case "link_nfse":
                                                linkImpressaoAux = x.ReadString();
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
                                            case "cancelamento":
                                                cancelamento = true;
                                                break;
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
                            if (x.NodeType == XmlNodeType.Element && x.Name == "codigo")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "mensagem")
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
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "correcao")
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

            #region nfse
            var nodeNfse = CriaHeaderXml("nfse", ref doc);

            #region nf
            var nodeNf = Extensions.CriarNo(doc, nodeNfse, "nf");

            Extensions.CriarNo(doc, nodeNf, "valor_total", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_desconto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_ir", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_inss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_contribuicao_social", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_rps", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_pis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_cofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "observacao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));

            #endregion fim - nf

            #region prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeNfse, "prestador");

            Extensions.CriarNo(doc, nodePrestador, "cpfcnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodePrestador, "cidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);

            #endregion fim - prestador

            #region tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeNfse, "tomador");

            Extensions.CriarNo(doc, nodeTomador, "tipo", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa);
            Extensions.CriarNo(doc, nodeTomador, "cpfcnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, nodeTomador, "ie", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual);
            Extensions.CriarNo(doc, nodeTomador, "nome_razao_social", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNo(doc, nodeTomador, "sobrenome_nome_fantasia", Generico.TratarString(nota.Documento.TDFe.TTomador.FNomeFantasia));
            Extensions.CriarNo(doc, nodeTomador, "logradouro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNo(doc, nodeTomador, "email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            Extensions.CriarNo(doc, nodeTomador, "complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNo(doc, nodeTomador, "ponto_referencia", "");
            Extensions.CriarNo(doc, nodeTomador, "bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNo(doc, nodeTomador, "cidade", nota.Documento.TDFe.TTomador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, nodeTomador, "cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            Extensions.CriarNo(doc, nodeTomador, "ddd_fone_comercial", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD));
            Extensions.CriarNo(doc, nodeTomador, "fone_comercial", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNo(doc, nodeTomador, "ddd_fone_residencial", "");
            Extensions.CriarNo(doc, nodeTomador, "fone_residencial", "");
            Extensions.CriarNo(doc, nodeTomador, "ddd_fax", "");
            Extensions.CriarNo(doc, nodeTomador, "fone_fax", "");

            #endregion fim - tomador

            #region itens
            var nodeItens = Extensions.CriarNo(doc, nodeNfse, "itens");

            #region lista
            var nodeLista = Extensions.CriarNo(doc, nodeItens, "lista");

            Extensions.CriarNo(doc, nodeLista, "codigo_local_prestacao_servico", GetMunicipioIncidencia(nota));
            Extensions.CriarNo(doc, nodeLista, "codigo_item_lista_servico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNo(doc, nodeLista, "descritivo", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNo(doc, nodeLista, "aliquota_item_lista_servico", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 2));
            var situacao = "";
            switch (nota.Documento.TDFe.Tide.FNaturezaOperacao)
            {
                case 1: { situacao = "Tributado no Prestador"; break; }
                case 2: { situacao = "Tributado no Tomador"; break; }
                case 3: { situacao = "Isenta"; break; }
                case 4: { situacao = "imune"; break; }
                default: { situacao = "Não tributada"; break; }
            }

            Extensions.CriarNo(doc, nodeLista, "situacao_tributaria", situacao);
            Extensions.CriarNo(doc, nodeLista, "valor_tributavel", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeLista, "valor_deducao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNo(doc, nodeLista, "valor_issrf", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNo(doc, nodeLista, "tributa_municipio_prestador", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido));
            Extensions.CriarNo(doc, nodeLista, "unidade_codigo", "");
            Extensions.CriarNo(doc, nodeLista, "unidade_quantidade", "");
            Extensions.CriarNo(doc, nodeLista, "unidade_valor_unitario", "");

            #endregion fim - lista

            #endregion fim - itens

            #region produtos

            #endregion fim - produtos
            var nodeProdutos = Extensions.CriarNo(doc, nodeNfse, "produtos");

            #endregion fim - nfse

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            var doc = new XmlDocument();

            #region nfse
            var nodeNfse = CriaHeaderXml("nfse", ref doc);

            #region pesquisa
            var nodePesquisa = Extensions.CriarNo(doc, nodeNfse, "pesquisa");

            Extensions.CriarNo(doc, nodePesquisa, "codigo_autenticidade", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");
            Extensions.CriarNo(doc, nodePesquisa, "numero", numeroNFSe);
            Extensions.CriarNo(doc, nodePesquisa, "serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, nodePesquisa, "cadastro", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);

            #endregion fim - pesquisa

            #endregion fim - nfse

            return doc;
        }
               
        public override XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            throw new ArgumentException("Executar metodo GerarXmlConsulta");
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, 
            string codigoVerificacao)
        {
            var doc = new XmlDocument();

            #region nfse
            var nodeNfse = CriaHeaderXml("nfse", ref doc);

            #region nf
            var nodeNf = Extensions.CriarNo(doc, nodeNfse, "nf");

            Extensions.CriarNo(doc, nodeNf, "numero", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "situacao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_total", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_desconto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_ir", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_inss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_pis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "valor_contribuicao_social", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeNf, "observacao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));

            #endregion fim - nf

            #region prestador
            var nodePrestador = Extensions.CriarNo(doc, nodeNfse, "prestador");

            Extensions.CriarNo(doc, nodePrestador, "cpfcnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodePrestador, "cidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);

            #endregion fim - prestador

            #region tomador
            var nodeTomador = Extensions.CriarNo(doc, nodeNfse, "tomador");

            Extensions.CriarNo(doc, nodeTomador, "tipo", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa);
            Extensions.CriarNo(doc, nodeTomador, "identificador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, nodeTomador, "nome_razao_social", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNo(doc, nodeTomador, "cidade", nota.Documento.TDFe.TTomador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, nodeTomador, "estado", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNo(doc, nodeTomador, "pais", "");
            Extensions.CriarNo(doc, nodeTomador, "cpfcnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, nodeTomador, "ie", "");
            Extensions.CriarNo(doc, nodeTomador, "logradouro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNo(doc, nodeTomador, "email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            Extensions.CriarNo(doc, nodeTomador, "numero_residencia", Generico.TratarString(nota.Documento.TDFe.TTomador.FNomeFantasia));
            Extensions.CriarNo(doc, nodeTomador, "complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNo(doc, nodeTomador, "ponto_referencia", "");
            Extensions.CriarNo(doc, nodeTomador, "bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNo(doc, nodeTomador, "cep", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            Extensions.CriarNo(doc, nodeTomador, "ddd_fone_comercial", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD));
            Extensions.CriarNo(doc, nodeTomador, "fone_comercial", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FFone));
            Extensions.CriarNo(doc, nodeTomador, "ddd_fone_residencial", "");
            Extensions.CriarNo(doc, nodeTomador, "fone_residencial", "");
            Extensions.CriarNo(doc, nodeTomador, "ddd_fax", "");
            Extensions.CriarNo(doc, nodeTomador, "fone_fax", "");

            #endregion fim - tomador

            #region itens
            var nodeItens = Extensions.CriarNo(doc, nodeNfse, "itens");

            #region lista
            var nodeLista = Extensions.CriarNo(doc, nodeItens, "lista");

            Extensions.CriarNo(doc, nodeLista, "codigo_local_prestacao_servico", GetMunicipioIncidencia(nota));
            Extensions.CriarNo(doc, nodeLista, "codigo_item_lista_servico", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNo(doc, nodeLista, "descritivo", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNo(doc, nodeLista, "aliquota_item_lista_servico", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 2));
            var situacao = "";
            switch (nota.Documento.TDFe.Tide.FNaturezaOperacao)
            {
                case 1: { situacao = "Tributado no Prestador"; break; }
                case 2: { situacao = "Tributado no Tomador"; break; }
                case 3: { situacao = "Isenta"; break; }
                case 4: { situacao = "imune"; break; }
                default: { situacao = "Não tributada"; break; }
            }

            Extensions.CriarNo(doc, nodeLista, "situacao_tributaria", situacao);
            Extensions.CriarNo(doc, nodeLista, "valor_tributavel", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, nodeLista, "valor_deducao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNo(doc, nodeLista, "valor_issrf", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNo(doc, nodeLista, "tributa_municipio_prestador", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido));
            Extensions.CriarNo(doc, nodeLista, "unidade_codigo", "");
            Extensions.CriarNo(doc, nodeLista, "unidade_quantidade", "");
            Extensions.CriarNo(doc, nodeLista, "unidade_valor_unitario", "");

            #endregion fim - lista

            #endregion fim - itens

            #region produtos

            #endregion fim - produtos
            var nodeProdutos = Extensions.CriarNo(doc, nodeNfse, "produtos");

            #endregion fim - nfse

            return doc;
        }

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);          

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

    }
}
