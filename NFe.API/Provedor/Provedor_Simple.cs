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
    internal class Provedor_Simple : AbstractProvedor, IProvedor
    {

        internal Provedor_Simple()
        {
            this.Nome = EnumProvedor.Simple;
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

            if (nota.Provedor.Nome != EnumProvedor.Simple)
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
                                            case "cancelarnfseresposta": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            case "consultarloterpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "consultarnfserpsresposta": // Consultar RPS
                                                _EnumResposta = EnumResposta.ConsultarNfseRpsResposta; break;
                                            case "enviarloterpsresposta": //Resposta do envio da RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "gerarnfseresposta":
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;


                                            case "nota": //RESPOSTA DO ENVIO DE RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "cancelamentonota": //Cancelar NFse
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                        }
                                        break;
                                    }
                                #endregion   "EnumResposta"
                                case EnumResposta.EnviarLoteRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {                                                                                           
                                            case "inota":

                                                if (numeroNF.Equals("")) {
                                                    int notaAux;
                                                    int.TryParse(x.ReadString(), out notaAux);

                                                    if (notaAux != 0)
                                                    {
                                                        numeroNF = notaAux.ToString();
                                                    }                                                   
                                                }                                               

                                                break;
                                                
                                            case "sretorno":
                                                msgRetorno = x.ReadString().Trim();
                                                break;

                                            case "ddataemissao":
                                                strDtEmissao = x.ReadString().Trim();
                                                break;

                                            case "scodigoverificador":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;

                                            case "ddatarecibo":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;
                                            case "ssituacao":
                                                strSituacaoNota = x.ReadString().Trim();
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

                                case EnumResposta.CancelarNfseResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "sretornocanc":
                                                msgRetorno = x.ReadString().Trim();
                                                break;
                                            case "ddatacancelamento":
                                                strDtCancelamento = x.ReadString().Trim();

                                                break;
                                            case "inotacanc":
                                                numeroNF = x.ReadString().Trim();
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

            if (strSituacaoNota.ToUpper().Trim() == "C" || !string.IsNullOrEmpty(strDtCancelamento))
            {
                situacaoRPS = "C";
                sucesso = true;
            }

            if ( numeroNF != "0" && strDtEmissao != "" && codigoVerificacao != "")
                sucesso = true;
                       
            if (dataEmissaoRPS != null && dataEmissaoRPS.Value != null)
            {
                nota.Documento.TDFe.Tide.DataEmissaoRps = dataEmissaoRPS.Value;
                nota.Documento.TDFe.Tide.DataEmissao = dataEmissaoRPS.Value;
                dhRecbto = dataEmissaoRPS.Value.ToString();
            }

            var xMotivo = msgRetorno;
            if ((sucesso && !string.IsNullOrEmpty(numeroNF)) || (!string.IsNullOrEmpty(numNF) && MesmaNota(numeroNF, numNF) && situacaoRPS != ""))
            {
                sucesso = true;
                success = "Sucesso";
            }
            else
            {
                error = xMotivo;
                if(string.IsNullOrEmpty(xMotivo))
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

        #region XML

        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {

            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");

            nsAttributeTipos.Value = "http://tempuri.org";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region LeRPSeGravaNota 
            var leRPSeGravaNota = CriaHeaderXml("LeRPSeGravaNota", ref doc);

            #region tNota
            var nodeTNota = Extensions.CriarNo(doc, leRPSeGravaNota, "tNota");

            #region Nota
            var nodeNota = Extensions.CriarNo(doc, nodeTNota, "Nota");

            Extensions.CriarNo(doc, nodeNota, "sRetorno", "");

            //CNPJ PRESTADOR
            Extensions.CriarNo(doc, nodeNota, "sContribuinte", nota.Documento.TDFe.TPrestador.FCnpj);
            //NUM. RPS
            Extensions.CriarNo(doc, nodeNota, "iRecibo", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, nodeNota, "dDataRecibo", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            Extensions.CriarNo(doc, nodeNota, "iNota", "0");
            Extensions.CriarNo(doc, nodeNota, "sSerie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, nodeNota, "dDataEmissao", "");
            Extensions.CriarNo(doc, nodeNota, "sCodigoVerificador", "");

            //N – Normal C – Cancelada E – extraviada
            Extensions.CriarNo(doc, nodeNota, "sSituacao", "N");

            //Nota de Substituição Tributaria S – Sim N – Não
            EnumNFSeSituacaoTributaria sSt = (EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido;

            Extensions.CriarNo(doc, nodeNota, "sSt", sSt != EnumNFSeSituacaoTributaria.stSubstituicao ? "N" : "S");

            //N - Normal  
            //S - Simples Nacional 
            //I -Isento 
            //R - Iss Retido 
            //P -Pago em Outro Município 
            //T -Substituição Tributaria 

            string situacao = "";
            switch (nota.Documento.TDFe.Tide.FNaturezaOperacao)
            {
                case 1:
                    {
                        situacao = "tp";                                                    // tp - Tributado no Prestador                
                        if (nota.Documento.TDFe.TServico.FValores.FIssRetido == 1)
                        {
                            situacao = "tt";                                                // tt - Tributado no Tomador                
                        }
                        break;
                    }
                case 2: { situacao = "tt"; break; }                                         // tt - Tributado no Tomador                
                case 3: { situacao = "is"; break; }                                         // is - Isenta
                case 4: { situacao = "im"; break; }                                         // im - Imune
                default: { situacao = "nt"; break; }                                        // nt – Não tributada
            }

            string sTributacao = "N"; //default

            if (nota.Documento.TDFe.Tide.FRegimeEspecialTributacao.ToString() == "9")
                sTributacao = "S";
            else
            {
                if (ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1).Equals("1"))
                    sTributacao = "R";
                else if (situacao == "is")
                    sTributacao = "I";
                else if (situacao == "tt")
                    sTributacao = "P";
                else if ((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido == EnumNFSeSituacaoTributaria.stSubstituicao)
                    sTributacao = "T";
            }
            Extensions.CriarNo(doc, nodeNota, "sTributacao", sTributacao);

            Extensions.CriarNo(doc, nodeNota, "nValorTotal", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNo(doc, nodeNota, "nValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNo(doc, nodeNota, "nValorBaseCalculo", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));


            #region tTomador
            var nodeTTomador = Extensions.CriarNo(doc, nodeNota, "tTomador");

            Extensions.CriarNo(doc, nodeTTomador, "sCPFTomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, nodeTTomador, "sNomeTomador", nota.Documento.TDFe.TTomador.FRazaoSocial);
            Extensions.CriarNo(doc, nodeTTomador, "sCidadeTomador", nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio);
            Extensions.CriarNo(doc, nodeTTomador, "sEnderecoTomador", nota.Documento.TDFe.TTomador.TEndereco.FEndereco);
            Extensions.CriarNo(doc, nodeTTomador, "sEmailTomador", nota.Documento.TDFe.TTomador.TContato.FEmail);
            Extensions.CriarNo(doc, nodeTTomador, "sUfTomador", nota.Documento.TDFe.TTomador.TEndereco.FUF);
            Extensions.CriarNo(doc, nodeTTomador, "sTipoTomador", ""); //E – estrangeiro Ou vazio

            #endregion FIM - tTomador

            Extensions.CriarNo(doc, nodeNota, "iLinhas", "1"); //Numero de RPS por lote, por padrão é sempre 1.
            Extensions.CriarNo(doc, nodeNota, "nIrAliquota", "0.00");
            Extensions.CriarNo(doc, nodeNota, "nIrValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNo(doc, nodeNota, "nPisPasep", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNo(doc, nodeNota, "nCofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNo(doc, nodeNota, "nInss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNo(doc, nodeNota, "nCsll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll, 2));



            #region tItens
            var nodeTItens = Extensions.CriarNo(doc, nodeNota, "tItens");

            #region Itens
            var nodeItens = Extensions.CriarNo(doc, nodeTItens, "Itens");

            Extensions.CriarNo(doc, nodeItens, "iSequencia", "1");
            Extensions.CriarNo(doc, nodeItens, "iServico", nota.Documento.TDFe.TServico.FItemListaServico.ToString());
            Extensions.CriarNo(doc, nodeItens, "sCNAE", nota.Documento.TDFe.TServico.FCodigoCnae);
            Extensions.CriarNo(doc, nodeItens, "nValorServico", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNo(doc, nodeItens, "sDescricao", nota.Documento.TDFe.TServico.FDiscriminacao);
            Extensions.CriarNo(doc, nodeItens, "nAliquota", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota, 2) : "0.00");
            Extensions.CriarNo(doc, nodeItens, "nValorIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNo(doc, nodeItens, "nValorTotal", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));

            #endregion FIM - Itens

            #endregion FIM - tItens

            Extensions.CriarNo(doc, nodeNota, "sObservacao1", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao2", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao3", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao4", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao5", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao6", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao7", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao8", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao9", "");
            Extensions.CriarNo(doc, nodeNota, "sObservacao10", "");

            #endregion FIM - Nota

            #endregion FIM - tNota

            Extensions.CriarNo(doc, leRPSeGravaNota, "iCMC", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());
            Extensions.CriarNo(doc, leRPSeGravaNota, "sLogin", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);
            Extensions.CriarNo(doc, leRPSeGravaNota, "sSenha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);

            #endregion FIM - LeRPSeGravaNota 

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var nodeConsultaNotaporRPS = CriaHeaderXml("ConsultaNotaporRPS", ref doc);

            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "iCMC", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());
            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "sLogin", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);
            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "sSenha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "iRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "sCPFCNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeConsultaNotaporRPS, "dDataRecibo", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            var nodeCancelarNota = CriaHeaderXml("CancelarNota", ref doc);

            #region tCancelamentoNota
            var nodeTCancelamentoNota = Extensions.CriarNo(doc, nodeCancelarNota, "tCancelamentoNota");

            #region CancelamentoNota
            var nodeCancelamentoNota = Extensions.CriarNo(doc, nodeTCancelamentoNota, "CancelamentoNota");

            Extensions.CriarNo(doc, nodeCancelamentoNota, "sRetornoCanc", "");
            Extensions.CriarNo(doc, nodeCancelamentoNota, "sContribuinteCanc", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, nodeCancelamentoNota, "iNotaCanc", numeroNFSe);
            Extensions.CriarNo(doc, nodeCancelamentoNota, "sSerieCanc", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, nodeCancelamentoNota, "dDataCancelamento", System.DateTime.Now.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, nodeCancelamentoNota, "sMotivoCanc", motivo);

            #endregion FIM - CancelamentoNota      
                       
            #endregion FIM - tCancelamentoNota

            Extensions.CriarNo(doc, nodeCancelarNota, "iCMC", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FFraseSecreta.ToString().Trim());
            Extensions.CriarNo(doc, nodeCancelarNota, "sLogin", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);
            Extensions.CriarNo(doc, nodeCancelarNota, "sSenha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);

            return doc;
        }

        #endregion

    }
}
