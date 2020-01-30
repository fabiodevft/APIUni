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
    internal class Provedor_Conam : AbstractProvedor, IProvedor
    {
        internal Provedor_Conam()
        {
            this.Nome = EnumProvedor.Conam;
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
            var retorno = string.Format("{0:0.00}", valor);

            return retorno;
        }

        private static string ImpostoRetido(EnumNFSeSituacaoTributaria situacao, int tipo = 0)
        {
            var tipoRecolhimento = "NAO";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "SIM";
            }

            return tipoRecolhimento;
        }

        private string DeParaTributacao(string fTributacao)
        {

            switch (fTributacao.ToLower().ToString())
            {
                case "1":
                case "2":
                    return "1";
                case "3":
                case "4":
                    return "2";
                case "5":
                    return "3";
                case "59":
                    return "4";
                default:
                    return "";
            }
        }

        private static bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            return (long.TryParse(numeroNF, out numero1) == long.TryParse(numeroNF, out numero2));
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.Conam)
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
                                            case "sdt_retornocancelanfe": //CancelarRPS
                                                _EnumResposta = EnumResposta.CancelarNfseResposta; break;
                                            //case "consultarloterpsresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarLoteRpsResposta; break;
                                            //case "consultarnfseresposta":
                                            //    _EnumResposta = EnumResposta.ConsultarNfseResposta; break;
                                            case "sdt_processarpsout": // Consultar RPS
                                                _EnumResposta = EnumResposta.EnviarLoteRpsResposta; break;
                                            case "sdt_consultanotasprotocoloout": //Resposta do envio da RPS
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
                                                break;
                                            case "retorno":
                                                bool retorno = false;

                                                bool.TryParse(x.ReadString(), out retorno);

                                                if (!retorno)
                                                {
                                                    area = EnumArea.Erro;
                                                }
                                                
                                                break;
                                        }
                                        break;
                                    }
                                case EnumResposta.ConsultarNfseRpsResposta:
                                    {
                                        switch (x.Name.ToString().ToLower())
                                        {

                                            case "codvernf":
                                                codigoVerificacao = x.ReadString();
                                                sucesso = true;
                                                break;
                                            case "numnf":
                                                numeroNF = x.ReadString();
                                                break;
                                            case "dteminf":
                                                DateTime emissao;
                                                DateTime.TryParse(x.ReadString(), out emissao);
                                                dataEmissaoRPS = emissao;
                                                break;


                                            case "sitnf":
                                                if (x.ReadString() == "2")
                                                {
                                                    situacaoRPS = "C";
                                                }
                                                break;


                                            case "datacncnf":
                                                break;
                                            case "numrps":
                                                numeroNF = x.ReadString();
                                                break;
                                            case "retorno":
                                                bool retorno = false;

                                                bool.TryParse(x.ReadString(), out retorno);

                                                if (!retorno)
                                                {
                                                    area = EnumArea.Erro;
                                                }
                                                break;
                                        }
                                        break;
                                    }
                                case EnumResposta.CancelarNfseResposta:
                                    {

                                        switch (x.Name.ToString().ToLower())
                                        {
                                            case "retorno":
                                                bool retorno = false;

                                                bool.TryParse(x.ReadString(), out retorno);

                                                if (!retorno)
                                                {
                                                    area = EnumArea.Erro;
                                                }
                                                else
                                                {
                                                    sucesso = true;
                                                    situacaoRPS = "C";
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
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Id")
                            {
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Description")
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
            #region "Sdt_ProcessarpsIn"
            var doc = new XmlDocument();
            var gerarNotaNode = CriaHeaderXml("Sdt_ProcessarpsIn", ref doc);

            #region "login"
            var LoginNode = Extensions.CriarNo(doc, gerarNotaNode, "Login");

            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoUsuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);// "c711f909-69c7-43ab-9fbf-40ea2252444645et17se6005t---000eta10bu8a"
            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoContribuinte", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha); //"c5503d21-3b99-4170-86b7-c097105f1aec80et10se0050t---067eta15bu4a"

            #endregion "login"

            #region "SDTRPS"
            var SDTRPSNode = Extensions.CriarNo(doc, gerarNotaNode, "SDTRPS");
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "Ano", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy"));
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "Mes", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("MM"));
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "CPFCNPJ", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "DTIni", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd/MM/yyyy"));
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "DTFin", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd/MM/yyyy"));
            //Extensions.CriarNoNotNull(doc, SDTRPSNode, "DtAdeSN", "????");   
            string tributacao_convertida = DeParaTributacao(nota.Documento?.TDFe?.Tide?.FNaturezaOperacao.ToString()?.Trim()) ?? "";
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "TipoTrib", tributacao_convertida);
            if (tributacao_convertida == "4" || tributacao_convertida == "6") {
                Extensions.CriarNoNotNull(doc, SDTRPSNode, "AlqIssSN_IP", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota));
            }            
            Extensions.CriarNoNotNull(doc, SDTRPSNode, "Versao", "2.00");

            #region "Reg20"
            var Reg20Node = Extensions.CriarNo(doc, SDTRPSNode, "Reg20");

            #region "Reg20Item"
            var Reg20ItemNode = Extensions.CriarNo(doc, Reg20Node, "Reg20Item");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "TipoNFS", "RPS");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "NumRps", Generico.RetornarNumeroZerosEsquerda(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, 9));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "SerRps", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "DtEmi", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd/MM/yyyy"));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "RetFonte", ImpostoRetido((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "CodSrv", nota.Documento.TDFe.TServico.FItemListaServico);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "DiscrSrv", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "VlNFS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "VlDed", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "DiscrDed", nota.Documento.TDFe.TServico.FValores.FJustificativaDeducao);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "VlBasCalc", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "AlqIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "VlIss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "VlIssRet", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "CpfCnpTom", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "RazSocTom", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "TipoLogtom", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "LogTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "NumEndTom", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "ComplEndTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "BairroTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "MunTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "SiglaUFTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FUF));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "CepTom", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FCEP));
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "Telefone", nota.Documento.TDFe.TTomador.TContato.FFone);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "InscricaoMunicipal", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "TipoLogLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "LogLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "NumEndLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "ComplEndLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "BairroLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "MunLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "SiglaUFLocpre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "CepLocPre", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "Email1", nota.Documento.TDFe.TTomador.TContato.FEmail);
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "Email2", "");
            Extensions.CriarNoNotNull(doc, Reg20ItemNode, "Email3", "");

            #region "Reg30"
            var Reg30Node = Extensions.CriarNo(doc, Reg20ItemNode, "Reg30");

            #region "Reg30Item"
            var Reg30ItemNode = Extensions.CriarNo(doc, Reg30Node, "Reg30Item");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoSigla", "COFINS");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoAliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaCofins));
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));

            Reg30ItemNode = Extensions.CriarNo(doc, Reg30Node, "Reg30Item");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoSigla", "CSLL");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoAliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaCsll));
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaCsll));

            Reg30ItemNode = Extensions.CriarNo(doc, Reg30Node, "Reg30Item");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoSigla", "INSS");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoAliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaInss));
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));

            Reg30ItemNode = Extensions.CriarNo(doc, Reg30Node, "Reg30Item");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoSigla", "IR");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoAliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaIr));
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));

            Reg30ItemNode = Extensions.CriarNo(doc, Reg30Node, "Reg30Item");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoSigla", "PIS");
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoAliquota", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaPis));
            Extensions.CriarNoNotNull(doc, Reg30ItemNode, "TributoValor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            #endregion "Reg30Item"

            #endregion "Reg30"


            #endregion "Reg20Item"

            #endregion "Reg20"

            #region "Reg90"
            var Reg90Node = Extensions.CriarNo(doc, SDTRPSNode, "Reg90");
            Extensions.CriarNoNotNull(doc, Reg90Node, "QtdRegNormal", "1");
            Extensions.CriarNoNotNull(doc, Reg90Node, "ValorNFS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNoNotNull(doc, Reg90Node, "ValorISS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss));
            Extensions.CriarNoNotNull(doc, Reg90Node, "ValorDed", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNoNotNull(doc, Reg90Node, "ValorIssRetTom", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIssRetido));
            Extensions.CriarNoNotNull(doc, Reg90Node, "QtdReg30", "5");
            Extensions.CriarNoNotNull(doc, Reg90Node, "ValorTributos", FormataValor(
                                                                                    nota.Documento.TDFe.TServico.FValores.FValorCofins +
                                                                                    nota.Documento.TDFe.TServico.FValores.FAliquotaCsll +
                                                                                    nota.Documento.TDFe.TServico.FValores.FValorInss +
                                                                                    nota.Documento.TDFe.TServico.FValores.FValorIr +
                                                                                    nota.Documento.TDFe.TServico.FValores.FValorPis
                                                                                    ));
            #endregion "Reg90"

            #endregion "SDTRPS"

            #endregion "Sdt_ProcessarpsIn"
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();
            var CONSULTAPROTOCOLONode = CriaHeaderXml("nfe:ws_nfe.CONSULTAPROTOCOLO", ref doc);
            var gerarNotaNode = Extensions.CriarNo(doc, CONSULTAPROTOCOLONode, "nfe:SDT_ConsultaProtocoloIn");

            Extensions.CriarNoNotNull(doc, gerarNotaNode, "nfe:Protocolo", nota.Documento.TDFe.Tide.FnProtocolo?.ToString() ?? "");

            var LoginNode = Extensions.CriarNo(doc, gerarNotaNode, "nfe:Login");
            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoUsuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);// "c711f909-69c7-43ab-9fbf-40ea2252444645et17se6005t---000eta10bu8a"
            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoContribuinte", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha); //"c5503d21-3b99-4170-86b7-c097105f1aec80et10se0050t---067eta15bu4a"

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            return GerarXmlCancelaNota(nota, numeroNFSe, "Exclusão via software");
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();
            var CONSULTAPROTOCOLONode = CriaHeaderXml("nfe:ws_nfe.CANCELANOTAELETRONICA", ref doc);
            var gerarNotaNode = Extensions.CriarNo(doc, CONSULTAPROTOCOLONode, "nfe:Sdt_CancelaNFE");

            var LoginNode = Extensions.CriarNo(doc, gerarNotaNode, "nfe:Login");
            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoUsuario", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario);// "c711f909-69c7-43ab-9fbf-40ea2252444645et17se6005t---000eta10bu8a"
            Extensions.CriarNoNotNull(doc, LoginNode, "CodigoContribuinte", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha); //"c5503d21-3b99-4170-86b7-c097105f1aec80et10se0050t---067eta15bu4a"

            var NotaNode = Extensions.CriarNo(doc, gerarNotaNode, "nfe:Nota");

            //Extensions.CriarNoNotNull(doc, NotaNode, "nfe:SerieNota", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            //Extensions.CriarNoNotNull(doc, NotaNode, "nfe:NumeroNota", numeroNFSe);
            Extensions.CriarNoNotNull(doc, NotaNode, "nfe:SerieRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, NotaNode, "nfe:NumeroRps", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, NotaNode, "nfe:ValorNota", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNoNotNull(doc, NotaNode, "nfe:MotivoCancelamento", motivo);
            Extensions.CriarNoNotNull(doc, NotaNode, "nfe:PodeCancelarGuia", "N");

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
            nsAttributeTipos.Value = "NFe";
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
