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
    public class Provedor_CECAM : AbstractProvedor, IProvedor
    {
        internal Provedor_CECAM()
        {
            this.Nome = EnumProvedor.CECAM;
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
            var tipoRecolhimento = "N";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "S";
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

            if (nota.Provedor.Nome != EnumProvedor.CECAM)
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
            int notaAux = 0;


            string msgRetorno = string.Empty;
            string strDtEmissao = string.Empty;
            string strCdVerificacao = string.Empty;
            string strSituacaoNota = string.Empty;
            string strDtCancelamento = string.Empty;

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
                                            case "issecancelanferetorno": //Cancelar NFse
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;

                                            case "isseconsultanotaretorno":
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;

                                            case "retorno":
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                        }
                                        break;
                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "numeronf":
                                                if (numeroNF.Equals(""))
                                                {                                                    
                                                    int.TryParse(x.ReadString(), out notaAux);

                                                    if (notaAux != 0)
                                                    {
                                                        numeroNF = notaAux.ToString();

                                                        if (Convert.ToInt64(numeroNF) > 0)
                                                        {
                                                            dataEmissaoRPS = DateTime.Now;
                                                        }
                                                    }
                                                }
                                                break;

                                            case "sretorno":
                                                msgRetorno = x.ReadString().Trim();
                                                break;

                                            case "ddataemissao":
                                                strDtEmissao = x.ReadString().Trim();
                                                break;

                                            case "chavevalidacao":
                                                codigoVerificacao = x.ReadString().Trim();
                                                sucesso = true;
                                                break;
                                           
                                            case "ssituacao":
                                                strSituacaoNota = x.ReadString().Trim();
                                                break;

                                            case "erro":
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
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "numeronf":
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

                                            case "situacaonf":
                                                string situacao = x.ReadString();
                                                if (situacao.ToUpper().Equals("C"))
                                                {
                                                    cancelamento = true;
                                                }
                                                break;

                                            case "emissao":
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
                                            case "erro":
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
                                            case "codigoresultado":
                                                var codigoResult = x.ReadString().Trim();

                                                if (codigoResult.Equals("1"))
                                                {
                                                    cancelamento = true;

                                                    strDtCancelamento = DateTime.Now.ToString();
                                                }

                                                break;
                                            case "ddatacancelamento":
                                                strDtCancelamento = x.ReadString().Trim();

                                                break;
                                            case "numeronf":
                                                numeroNF = x.ReadString().Trim();
                                                break;
                                            case "erro":
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
                            if (x.NodeType == XmlNodeType.Element && x.Name == "ID")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Erro")
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
                    if ((notaAux == 0 && string.IsNullOrEmpty(numeroNF)) && string.IsNullOrEmpty(descricaoErro))
                    {
                        descricaoErro = "A transmissão não retornou numero de nota. Tente executar uma nova transmissão mais tarde.";
                    }

                }
                stream.Dispose();
            }

            var dhRecbto = "";
            var error = "";
            var success = "";

            if (strSituacaoNota.ToUpper().Trim() == "C" || !string.IsNullOrEmpty(strDtCancelamento))
            {
                situacaoRPS = "C";
                sucesso = true;
            }

            if (numeroNF != "0" && strDtEmissao != "" && codigoVerificacao != "")
                sucesso = true;

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
                    error = "Não foi possível finalizar a transmissão. Tente novamente mais tarde ou execute uma consulta.";
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
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {

            string value = string.Empty;

            switch (strNomeMetodo.ToUpper())
            {
                case "NFEELETRONICA":
                    value = "XSDISSEletronico.xsd";
                    break;

                case "ISSECONSULTANOTA":
                    value = "XSDISSEConsultaNota.xsd";
                    break;

                case "ISSECANCELANFE":
                    value = "XSDISSECancelaNFe.xsd";
                    break;
            }
            
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);


            var nsAttributeXsd = doc.CreateAttribute("xmlns:xsi", "http://www.w3.org/2000/xmlns/");
            nsAttributeXsd.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttributeXsd);


            var nsTeste = doc.CreateAttribute("noNamespaceSchemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            nsTeste.InnerText = value;
            gerarNotaNode.Attributes.Append(nsTeste);
                    

            doc.AppendChild(gerarNotaNode);

            return gerarNotaNode;
        }

        #region XML

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region NFEEletronica
            var nodeNFEEletronica = CriaHeaderXml("NFEEletronica", ref doc);

            #region Header
            var nodeHeader = Extensions.CriarNo(doc, nodeNFEEletronica, "Header");

            Extensions.CriarNo(doc, nodeHeader, "Versao", "002"); //FIXO
            Extensions.CriarNo(doc, nodeHeader, "CNPJCPFPrestador", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeHeader, "Chave", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());

            #endregion Header - FIM

            #region DadosNotaFiscal
            var nodeDadosNotaFiscal = Extensions.CriarNo(doc, nodeNFEEletronica, "DadosNotaFiscal");

            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ID", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NumeroNF", "0000000000"); //FIXO
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "SituacaoNF", "N"); //FIXO
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "TipoNF", "P"); //FIXO
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "Emissao", "1900-01-01"); //FIXO
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CNPJCPFTomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NomeTomador", nota.Documento.TDFe.TTomador.FRazaoSocial);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "UFTomador", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CidadeTomador", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "EnderecoTomador", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NumeroTomador", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ComplementoTomador", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "BairroTomador", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CEPTomador", Convert.ToUInt64(nota.Documento.TDFe.TTomador.TEndereco.FCEP).ToString(@"00000\-000"));
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "EmailTomador", nota.Documento.TDFe.TTomador.TContato.FEmail ?? "");
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "Observacao", "");
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NFSubstituta", "0000000000"); //FIXO

            //if (nota.Documento.TDFe.Tide.FOptanteSimplesNacional == 1)
            //{
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ValorAliquotaSimplesNacional", "");
            //}
            string localServico = string.Empty;
            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "LocalPrestacao", "4");
                localServico = "4";
            }
            else
            {
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "LocalPrestacao", "1");
                localServico = "1";
            }

            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DescricaoLocalPrestacao", "");
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DescricaoLocalPrestacaoComplementar", "");
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "InscricaoImovel", "0");

            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "UFLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            }
            else
            {
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "UFLocalPrestacao", nota.Documento.TDFe.TPrestador.TEndereco.FUF);
            }
                        

            if (localServico == "4" || localServico == "3")
            {               
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CidadeLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ValorAliquotaLocalPrestacao", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 2));
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "EnderecoLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NumeroLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ComplementoLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FComplemento);
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "BairroLocalPrestacao", nota.Documento.TDFe.TTomador.TEndereco.FBairro);
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CEPLocalPrestacao", Convert.ToUInt64(nota.Documento.TDFe.TTomador.TEndereco.FCEP).ToString(@"00000\-000"));
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "MotivoCancelamento", "");
            }
            else
            {
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CidadeLocalPrestacao", "0000000");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ValorAliquotaLocalPrestacao", "0");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "EnderecoLocalPrestacao", "");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "NumeroLocalPrestacao", "");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "ComplementoLocalPrestacao", "");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "BairroLocalPrestacao", "");
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "CEPLocalPrestacao", Convert.ToUInt64(nota.Documento.TDFe.TPrestador.TEndereco.FCEP).ToString(@"00000\-000"));
                Extensions.CriarNo(doc, nodeDadosNotaFiscal, "MotivoCancelamento", "");
            }

            
            Extensions.CriarNo(doc, nodeDadosNotaFiscal, "TipoDocumento", "009"); //FIXO

            #region DadosItensNotaFiscal
            var nodeDadosItensNotaFiscal = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosItensNotaFiscal");

            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "ItemAtividade", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico.ToString()));
            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "TextoItem", nota.Documento.TDFe.TServico.FDiscriminacao);
            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "ValorItem", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "ValorDeducao", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes, 2));
            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "Retido", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNo(doc, nodeDadosItensNotaFiscal, "Pais", "Brasil"); //FIXO

            #endregion DadosItensNotaFiscal - FIM

            #region DadosImpostosNotaFiscal            
            if (nota.Documento.TDFe.TServico.FValores.FValorIssRetido > 0)
            {
                var nodeDadosImpostosNotaFiscalISS = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalISS, "Imposto", "ISS");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalISS, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido, 2));
            }

            if (nota.Documento.TDFe.TServico.FValores.FValorPis > 0)
            {
                var nodeDadosImpostosNotaFiscalPIS = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalPIS, "Imposto", "PIS");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalPIS, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            }

            if (nota.Documento.TDFe.TServico.FValores.FValorInss > 0)
            {
                var nodeDadosImpostosNotaFiscalInss = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalInss, "Imposto", "INSS");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalInss, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            }

            if (nota.Documento.TDFe.TServico.FValores.FValorCsll > 0)
            {
                var nodeDadosImpostosNotaFiscalCsll = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalCsll, "Imposto", "CSLL");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalCsll, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));
            }

            if (nota.Documento.TDFe.TServico.FValores.FValorIr > 0)
            {
                var nodeDadosImpostosNotaFiscalIssRetido = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalIssRetido, "Imposto", "IRRF");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalIssRetido, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            }

            if (nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes > 0)
            {
                var nodeDadosImpostosNotaFiscalOutrasRetencoes = Extensions.CriarNo(doc, nodeDadosNotaFiscal, "DadosImpostosNotaFiscal");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalOutrasRetencoes, "Imposto", "OUTRO");
                Extensions.CriarNoNotNull(doc, nodeDadosImpostosNotaFiscalOutrasRetencoes, "ValorImposto", FormataValor(nota.Documento.TDFe.TServico.FValores.FvalorOutrasRetencoes, 2));
            }

            #endregion

            #endregion DadosNotaFiscal - FIM

            #endregion NFEEletronica - FIM 

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            var doc = new XmlDocument();
            var nodeISSEConsultaNota = CriaHeaderXml("ISSEConsultaNota", ref doc);
                        
            #region Header
            var nodeHeader = Extensions.CriarNo(doc, nodeISSEConsultaNota, "Header");

            Extensions.CriarNo(doc, nodeHeader, "Versao", "003"); //FIXO
            Extensions.CriarNo(doc, nodeHeader, "CNPJCPFPrestador", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeHeader, "Chave", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());

            #endregion Header - FIM

            #region Filtro
            var nodeFiltro = Extensions.CriarNo(doc, nodeISSEConsultaNota, "Filtro");

            Extensions.CriarNo(doc, nodeFiltro, "NumeroNFInicial", numeroNFSe);
            Extensions.CriarNo(doc, nodeFiltro, "NumeroNFFinal", numeroNFSe);
            Extensions.CriarNo(doc, nodeFiltro, "Referencia", emissao.ToString("yyyyMM"));
            Extensions.CriarNo(doc, nodeFiltro, "Lote", nota.Documento.TDFe.Tide.FNumeroLote.ToString());
            Extensions.CriarNo(doc, nodeFiltro, "TipoDocumento", "000");

            #endregion Filtro - FIM
            
            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            var doc = new XmlDocument();
            var nodeISSECancelaNFe = CriaHeaderXml("ISSECancelaNFe", ref doc);


            #region Header
            var nodeHeader = Extensions.CriarNo(doc, nodeISSECancelaNFe, "Header");

            Extensions.CriarNo(doc, nodeHeader, "Versao", "004"); //FIXO
            Extensions.CriarNo(doc, nodeHeader, "CNPJCPFPrestador", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeHeader, "Chave", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());

            #endregion Header - FIM

            #region Filtro
            var nodeFiltro = Extensions.CriarNo(doc, nodeISSECancelaNFe, "Filtro");

            Extensions.CriarNo(doc, nodeFiltro, "NumeroNF", numeroNFSe);

            Extensions.CriarNo(doc, nodeFiltro, "ChaveValidacao", codigoVerificacao);
                       
            Extensions.CriarNo(doc, nodeFiltro, "MotivoCancelamento", motivo);

            #endregion Filtro - FIM


            return doc;
        }

        #endregion XML - FIM


    }
}
