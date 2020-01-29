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
    internal class Provedor_SMARAPD : AbstractProvedor, IProvedor
    {
        internal Provedor_SMARAPD()
        {
            this.Nome = EnumProvedor.SMARAPD;
        }

        private static string TipoTributacao(NFSeNota nota)
        {
            //True – Tributado no Municipio
            //False – Tributado Fora 
            string trib = "True";

            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                trib = "False";
            }
            return trib;
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
                retorno = valor.ToString();
            }
            else
            {
                retorno = decimal.Floor(valor).ToString();
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
            if (nota.Provedor.Nome != EnumProvedor.SMARAPD)
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
            var linkImpressao = "";
            long numeroLote = 0;
            var descricaoProcesso = "";
            var descricaoErro = "";
            var area = EnumArea.Nenhum;
            var codigoErroOuAlerta = "";
            var _EnumResposta = EnumResposta.Nenhum;
            var isValidXML = true;
            string LinkImpressaoAux = string.Empty;

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("UTF-8"));
                using (XmlReader x = XmlReader.Create(stream))
                {
                    try { x.Read(); } catch { isValidXML = false; }
                    if (isValidXML)
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
                                                //case "cancelarnfseresposta": //CancelarRPS
                                                //  _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                                //case "consultarloterpsresposta":
                                                //    _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                                //case "consultarnfseresposta":
                                                //    _EnumResposta = EnumResposta.ConsultarNfseResposta; break;
                                                case "tbnfd": // Consultar RPS
                                                    _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                                case "nfd": //Resposta do envio da RPS
                                                    _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            }
                                            break;

                                        }
                                    #endregion   "EnumResposta"
                                    case EnumResposta.EnviarLoteRpsResposta:
                                        {
                                            switch (x.Name.ToString().ToLower())
                                            {
                                                case "codrecibo":
                                                    protocolo = x.ReadString();
                                                    codigoVerificacao = x.ReadString();
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
                                                case "chavevalidacao":
                                                    protocolo = x.ReadString();
                                                    break;
                                                case "numeronota":
                                                    numeroNF = x.ReadString();
                                                    sucesso = true;
                                                    break;
                                                case "numerorps":
                                                    numeroRPS = x.ReadString();
                                                    //long.TryParse(numeroRPS.Substring(4), out numeroLote);                                                    
                                                    break;
                                                case "dataemissao":
                                                    DateTime emissao;
                                                    DateTime.TryParse(x.ReadString(), out emissao);
                                                    dataEmissaoRPS = emissao;
                                                    break;
                                                case "situacaonf":
                                                    if (!x.ReadString().ToString().Trim().ToLower().Equals("normal")) {
                                                        situacaoRPS = "C";
                                                    }                                                        
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
                    else
                    {
                        x.Close();
                        stream.Dispose();
                        using (stream = new StreamReader(arquivo))
                        {
                            descricaoErro = stream.ReadToEnd();
                        }
                        if (descricaoErro.Length == 0)
                        {
                            descricaoErro = "ERRO DESCONHECIDO";
                        } else {
                            if((descricaoErro?.ToLower().ToString().Trim()??"").Contains("nota fiscal cancelada com sucesso")){
                                situacaoRPS = "C";
                                descricaoErro = "";
                                sucesso = true;
                            }
                        }
                    }
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

            if (protocolo != "" && numeroNF.ToString().Trim() != "")
            {
                switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
                {
                    case "3506508": /*Birigui-SP*/
                        LinkImpressaoAux = @"http://201.49.71.187:8085/tbw/loginWeb.jsp?execobj=NFENotaFiscalBuscarDireto&" +
                                    "cnpj=" + (nota?.Documento?.TDFe?.TPrestador?.FCnpj?.ToString().Trim() ?? "") +
                                    "&numero=" + (numeroNF?.ToString().Trim() ?? "") +
                                    "&chave=" + (protocolo?.ToString().Trim() ?? "");
                        break;

                    case "3205002": /*Serra-ES*/
                        LinkImpressaoAux = @"http://apps.serra.es.gov.br:8080/tbw/loginWeb.jsp?execobj=NFENotaFiscalBuscarDireto&" +
                                    "cnpj=" + (nota?.Documento?.TDFe?.TPrestador?.FCnpj?.ToString().Trim() ?? "") +
                                    "&numero=" + (numeroNF?.ToString().Trim() ?? "") +
                                    "&chave=" + (protocolo?.ToString().Trim() ?? "");
                        break;

                    default:
                        LinkImpressaoAux = string.Empty;
                        break;

                }

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

            nota = verificar_homolocagaco(nota);

            #region "tbnfd"
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("tbnfd", ref doc);

            #region "nfd"                  

            var nfdNode = Extensions.CriarNo(doc, gerarNotaNode, "nfd");

            Extensions.CriarNoNotNull(doc, nfdNode, "numeronfd", "0"); //Para gerar o numero automaticamente
            Extensions.CriarNoNotNull(doc, nfdNode, "codseriedocumento", "7");  //FIXO
            Extensions.CriarNoNotNull(doc, nfdNode, "codnaturezaoperacao", nota.Documento.TDFe.Tide.FNaturezaOperacao.ToString());
            Extensions.CriarNoNotNull(doc, nfdNode, "codigocidade", "3");  //FIXO
            Extensions.CriarNo(doc, nfdNode, "inscricaomunicipalemissor", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nfdNode, "dataemissao", DateTime.Now.ToString("dd/MM/yyyy"));
            Extensions.CriarNoNotNull(doc, nfdNode, "razaotomador", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNoNotNull(doc, nfdNode, "nomefantasiatomador", Generico.TratarString(nota.Documento.TDFe.TTomador.FNomeFantasia));
            Extensions.CriarNoNotNull(doc, nfdNode, "enderecotomador", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, nfdNode, "numeroendereco", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nfdNode, "cidadetomador", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio));
            Extensions.CriarNoNotNull(doc, nfdNode, "estadotomador", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNoNotNull(doc, nfdNode, "paistomador", "Brasil");
            Extensions.CriarNoNotNull(doc, nfdNode, "fonetomador", nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone);
            //Extensions.CriarNoNotNull(doc, nfdNode, "faxtomador", "");  
            Extensions.CriarNoNotNull(doc, nfdNode, "ceptomador", nota.Documento.TDFe.TTomador.TEndereco.FCEP);
            Extensions.CriarNoNotNull(doc, nfdNode, "bairrotomador", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, nfdNode, "emailtomador", nota.Documento.TDFe.TTomador.TContato.FEmail);
            Extensions.CriarNoNotNull(doc, nfdNode, "tppessoa", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa);
            Extensions.CriarNoNotNull(doc, nfdNode, "cpfcnpjtomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNoNotNull(doc, nfdNode, "inscricaoestadualtomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual);
            Extensions.CriarNoNotNull(doc, nfdNode, "inscricaomunicipaltomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nfdNode, "observacao", Generico.TratarString(nota.Documento.TDFe.Tide.FOutrasInformacoes));

            #region "tbfatura"       

            var tbfaturaNode = Extensions.CriarNo(doc, nfdNode, "tbfatura");

            #region "fatura"
            foreach (var oFatura in nota.Documento.TDFe.TCondicaoPagamento.FParcelas)
            {
                var faturaNode = Extensions.CriarNo(doc, tbfaturaNode, "fatura");
                Extensions.CriarNoNotNull(doc, faturaNode, "numfatura", oFatura.FParcela.ToString());
                Extensions.CriarNoNotNull(doc, faturaNode, "vencimentofatura", oFatura.DataVencimento.ToString("dd/MM/yyyy"));
                Extensions.CriarNoNotNull(doc, faturaNode, "valorfatura", FormataValor(oFatura.FValor));
            }
            #endregion "fatura"

            #endregion "tbfatura"                  

            #region "tbservico" 
            var tbservicoNode = Extensions.CriarNo(doc, nfdNode, "tbservico");
            #region "servico" 
            foreach (var oServico in nota.Documento.TDFe.TServico.TItemServico)
            {
                var servicoNode = Extensions.CriarNo(doc, tbservicoNode, "servico");

                Extensions.CriarNoNotNull(doc, servicoNode, "quantidade", FormataValor(oServico.FQuantidade));
                Extensions.CriarNoNotNull(doc, servicoNode, "descricao", Generico.TratarString(oServico.FDescricao));
                Extensions.CriarNoNotNull(doc, servicoNode, "codatividade", oServico.FCodServ);
                Extensions.CriarNoNotNull(doc, servicoNode, "valorunitario", FormataValor(oServico.FValorUnitario));
                Extensions.CriarNoNotNull(doc, servicoNode, "aliquota", FormataValor(oServico.FAliquota));
                //Extensions.CriarNoNotNull(doc, servicoNode, "impostoretido", );
            }
            #endregion "servico" 
            #endregion "tbservico" 


            Extensions.CriarNoNotNull(doc, nfdNode, "VlrAproxImposto", FormataValor(nota.Documento.TDFe.TServico.FValorCargaTributaria / 100));
            Extensions.CriarNoNotNull(doc, nfdNode, "AliquotaImpostoAprox", FormataValor(nota.Documento.TDFe.TServico.FPercentualCargaTributaria / 100));
            Extensions.CriarNoNotNull(doc, nfdNode, "FonteImpostoAprox", nota.Documento.TDFe.TServico.FFonteCargaTributaria);
            //Extensions.CriarNoNotNull(doc, nfdNode, "razaotransportadora", "");
            //Extensions.CriarNoNotNull(doc, nfdNode, "cpfcnpjtransportadora", "");
            //Extensions.CriarNoNotNull(doc, nfdNode, "enderecotransportadora", "");
            //Extensions.CriarNoNotNull(doc, nfdNode, "tipofrete", "0");
            //Extensions.CriarNoNotNull(doc, nfdNode, "quantidade", "0");
            //Extensions.CriarNoNotNull(doc, nfdNode, "especie", "0");
            //Extensions.CriarNoNotNull(doc, nfdNode, "pesoliquido", "0");
            //Extensions.CriarNoNotNull(doc, nfdNode, "pesobruto", "0");
            Extensions.CriarNoNotNull(doc, nfdNode, "pis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            Extensions.CriarNoNotNull(doc, nfdNode, "cofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));
            Extensions.CriarNoNotNull(doc, nfdNode, "csll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll));
            Extensions.CriarNoNotNull(doc, nfdNode, "irrf", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));
            Extensions.CriarNoNotNull(doc, nfdNode, "inss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));
            //Extensions.CriarNoNotNull(doc, nfdNode, "descdeducoesconstrucao", "0");
            //Extensions.CriarNoNotNull(doc, nfdNode, "totaldeducoesconstrucao", "0");
            Extensions.CriarNoNotNull(doc, nfdNode, "tributadonomunicipio", TipoTributacao(nota));
            Extensions.CriarNoNotNull(doc, nfdNode, "numerort", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nfdNode, "codigoseriert", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nfdNode, "dataemissaort", nota.Documento.TDFe.Tide.FDataEmissaoRps);

            #endregion "nfd"   

            #region "Signature => dsig:Signature "
            #endregion

            #endregion tbnfd
            return doc;
        }

        private NFSeNota verificar_homolocagaco(NFSeNota nota)
        {
            if (nota.Documento.TDFe.Tide.FIdentificacaoRps.FAmbiente == EnumAmbiente.Homologacao)
            {
                nota.Documento.TDFe.TPrestador.FCnpj = "55555555555";
                nota.Documento.TDFe.TPrestador.FInscricaoMunicipal = "555555";
                nota.Documento.TDFe.TServico.FItemListaServico = "1.01";

                foreach (var item in nota.Documento.TDFe.TServico.TItemServico)
                {
                    item.FCodServ = "1.01";
                    item.FCodLCServ = "1.01";
                }

                nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha = "1234";
                nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario = "55555555555";


            }
            return nota;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao) {
            return GerarXmlConsulta(nota, numeroNFSe);
        }


        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe)
        {

            nota = verificar_homolocagaco(nota);

            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("nfdSaida", ref doc);

            var IdentificacaoRpsNode = Extensions.CriarNo(doc, gerarNotaNode, "recibo");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "codrecibo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");
            Extensions.CriarNoNotNull(doc, IdentificacaoRpsNode, "codmunicipio", "3");

            doc.AppendChild(gerarNotaNode);
            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            return GerarXmlCancelaNota(nota, numeroNFSe, "Erro na digitação.");
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            nota = verificar_homolocagaco(nota);
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("nfd", ref doc);

            Extensions.CriarNo(doc, gerarNotaNode, "inscricaomunicipalemissor", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, gerarNotaNode, "numeronf", numeroNFSe);
            Extensions.CriarNo(doc, gerarNotaNode, "motivocancelamento", Generico.TratarString(motivo));
            Extensions.CriarNo(doc, gerarNotaNode, "datacancelamento", DateTime.Now.ToString("dd/MM/yyyy"));

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

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

    }
}
