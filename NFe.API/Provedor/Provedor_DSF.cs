using NFe.API.Domain;
using NFe.API.Enum;
using NFe.API.Provedor;
using NFe.API.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static NFe.API.Domain.NFSeModel;
using static NFe.API.Util.Extensions;

namespace FRGDocFiscal.Provedor
{
    internal class Provedor_DSF : AbstractProvedor, IProvedor
    {
        internal Provedor_DSF()
        {
            this.Nome = EnumProvedor.DSF;
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

        private static string SituacaoRPS(EnumNFSeRPSStatus status)
        {
            var situacaoRPS = "N";
            if (status == EnumNFSeRPSStatus.srCancelado)
            {
                situacaoRPS = "C";
            }
            return situacaoRPS;
        }
        private static string TipoRecolhimento(EnumNFSeSituacaoTributaria situacao, int tipo = 0)
        {
            var tipoRecolhimento = "N";
            if (situacao == EnumNFSeSituacaoTributaria.stRetencao)
            {
                tipoRecolhimento = "S";
            }

            if (tipo == 1 && tipoRecolhimento == "N") tipoRecolhimento = "A";
            else if (tipo == 1 && tipoRecolhimento == "S") tipoRecolhimento = "R";

            return tipoRecolhimento;
        }

        private static string GerarAssinatura(NFSeNota nota)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio))
                throw new ArgumentException("Código Tributação do Município do emitente não pode ser nulo");

            decimal vrTotalServicos = 0;

            foreach (var item in nota.Documento.TDFe.TServico.TItemServico)
            {
                var valor = Math.Round((item.FValorTotal / item.FQuantidade), 4);
                vrTotalServicos += Math.Truncate((item.FQuantidade * valor) * 100) / 100;
            }

            var reg = new TRegistroEDI();
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 01, 11, 0, nota.Documento.TDFe.TPrestador.FInscricaoMunicipal, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 12, 05, 0, "NF", ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 17, 12, 0, nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataAAAAMMDD_________, 29, 08, 0, nota.Documento.TDFe.Tide.DataEmissaoRps, ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 37, 02, 0, ValidaTributacao(nota).ToString(), ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 39, 01, 0, SituacaoRPS(nota.Documento.TDFe.Tide.FStatus), ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 40, 01, 0, TipoRecolhimento((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido), ' '));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 41, 15, 2, vrTotalServicos, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 56, 15, 2, nota.Documento.TDFe.TServico.FValores.FValorDeducoes, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 71, 10, 0, nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio, '0'));
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 81, 14, 0, nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj, '0'));
            reg.CodificarLinha();

            return reg.LinhaRegistro;
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI))
                throw new ArgumentException(nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio + ": Código da cidade do prestador padrão SIAFI não pode ser nulo.");

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TTomador.TEndereco.FSIAFI))
                throw new ArgumentException(nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio + ": Código da cidade do tomador padrão SIAFI não pode ser nulo.");

            var doc = new XmlDocument();
            
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            var gerarNotaNode = doc.CreateElement("ns1", "ReqEnvioLoteRPS", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "tipos", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://localhost:8080/WsNFe2/tp";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://localhost:8080/WsNFe2/lote http://localhost:8080/WsNFe2/xsd/ReqEnvioLoteRPS.xsd";
            gerarNotaNode.SetAttributeNode(schemaLocation);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");

            Extensions.CriarNo(doc, cabecalhoNode, "CodCidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "RazaoSocialRemetente", nota.Documento.TDFe.TPrestador.FRazaoSocial);
            Extensions.CriarNo(doc, cabecalhoNode, "transacao", "true");
            Extensions.CriarNo(doc, cabecalhoNode, "dtInicio", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "dtFim", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "QtdRPS", "1");
            Extensions.CriarNo(doc, cabecalhoNode, "ValorTotalServicos", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos));
            Extensions.CriarNo(doc, cabecalhoNode, "ValorTotalDeducoes", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorDeducoes));
            Extensions.CriarNo(doc, cabecalhoNode, "Versao", "1");
            Extensions.CriarNo(doc, cabecalhoNode, "MetodoEnvio", "WS");

            #endregion Cabecalho

            #region Lote

            var loteNode = Extensions.CriarNo(doc, gerarNotaNode, "Lote", "", "Id", string.Concat("L", nota.Documento.TDFe.Tide.FNumeroLote.ToString()));

            #region RPS

            var rpsNode = Extensions.CriarNo(doc, loteNode, "RPS", "", "Id", string.Concat("R", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero));

            Extensions.CriarNo(doc, rpsNode, "Assinatura", GerarAssinatura(nota));
            Extensions.CriarNo(doc, rpsNode, "InscricaoMunicipalPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, rpsNode, "RazaoSocialPrestador",nota.Documento.TDFe.TPrestador.FRazaoSocial);
            Extensions.CriarNo(doc, rpsNode, "TipoRPS", "RPS");
            Extensions.CriarNo(doc, rpsNode, "SerieRPS", "NF");
            Extensions.CriarNo(doc, rpsNode, "NumeroRPS", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNo(doc, rpsNode, "DataEmissaoRPS", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-ddTHH:mm:ss"));
            Extensions.CriarNo(doc, rpsNode, "SituacaoRPS", SituacaoRPS(nota.Documento.TDFe.Tide.FStatus));
            Extensions.CriarNo(doc, rpsNode, "SeriePrestacao", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNo(doc, rpsNode, "InscricaoMunicipalTomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, rpsNode, "CPFCNPJTomador", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            Extensions.CriarNo(doc, rpsNode, "RazaoSocialTomador", Generico.TratarString(nota.Documento.TDFe.TTomador.FRazaoSocial));
            Extensions.CriarNo(doc, rpsNode, "TipoLogradouroTomador");
            Extensions.CriarNo(doc, rpsNode, "LogradouroTomador", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNo(doc, rpsNode, "NumeroEnderecoTomador", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNo(doc, rpsNode, "TipoBairroTomador");
            Extensions.CriarNo(doc, rpsNode, "BairroTomador", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNo(doc, rpsNode, "CidadeTomador", nota.Documento.TDFe.TTomador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, rpsNode, "CidadeTomadorDescricao", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio));
            Extensions.CriarNo(doc, rpsNode, "CEPTomador", nota.Documento.TDFe.TTomador.TEndereco.FCEP);

            var listEmail = nota.Documento.TDFe.TTomador.TContato.FEmail?.Split(';');
            if (listEmail != null && listEmail.Length > 0)
            {
                Extensions.CriarNo(doc, rpsNode, "EmailTomador", listEmail[0]);
            }
            else
            {
                Extensions.CriarNo(doc, rpsNode, "EmailTomador", "");
            }

            Extensions.CriarNo(doc, rpsNode, "CodigoAtividade", Generico.RetornarNumeroZerosEsquerda(nota.Documento.TDFe.TServico.FCodigoTributacaoMunicipio, 9));
            Extensions.CriarNo(doc, rpsNode, "AliquotaAtividade", GetAliquotaAtividade(nota));
            Extensions.CriarNo(doc, rpsNode, "TipoRecolhimento", TipoRecolhimento((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1));
            Extensions.CriarNo(doc, rpsNode, "MunicipioPrestacao", BuscarMunicipioPrestacao(nota).FSIAFI );
            Extensions.CriarNo(doc, rpsNode, "MunicipioPrestacaoDescricao", BuscarMunicipioPrestacao(nota).FxMunicipio );
            Extensions.CriarNo(doc, rpsNode, "Operacao", nota.Documento.TDFe.TServico.FOperacao);
            Extensions.CriarNo(doc, rpsNode, "Tributacao", ValidaTributacao(nota));
            Extensions.CriarNo(doc, rpsNode, "ValorPIS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis));
            Extensions.CriarNo(doc, rpsNode, "ValorCOFINS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins));
            Extensions.CriarNo(doc, rpsNode, "ValorINSS", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss));
            Extensions.CriarNo(doc, rpsNode, "ValorIR", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr));
            Extensions.CriarNo(doc, rpsNode, "ValorCSLL", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCsll));
            Extensions.CriarNo(doc, rpsNode, "AliquotaPIS", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaPis));
            Extensions.CriarNo(doc, rpsNode, "AliquotaCOFINS", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaCofins));
            Extensions.CriarNo(doc, rpsNode, "AliquotaINSS", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaInss));
            Extensions.CriarNo(doc, rpsNode, "AliquotaIR", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaIr));
            Extensions.CriarNo(doc, rpsNode, "AliquotaCSLL", FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquotaCsll));

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == "3303500") //Nova Iguaçu
            {
                Extensions.CriarNo(doc, rpsNode, "DescricaoRPS", Generico.TrocarCaractercomAcentos(nota.Documento.TDFe.Tide._FMsgComplementares));
            }
            else
            {
                Extensions.CriarNo(doc, rpsNode, "DescricaoRPS");
            }

            var fonePrestador = nota.Documento.TDFe.TPrestador.TContato.FFone != null ? nota.Documento.TDFe.TPrestador.TContato.FFone.Replace("-", "") : "";
            var foneTomador = nota.Documento.TDFe.TTomador.TContato.FFone != null ? nota.Documento.TDFe.TTomador.TContato.FFone.Replace("-", "") : "";


            if (fonePrestador.Length >= 9)
            {
                fonePrestador = fonePrestador.Substring(1, 8);
            }

            if (foneTomador.Length >= 9)
            {
                foneTomador = foneTomador.Substring(1, 8);
            }

            Extensions.CriarNo(doc, rpsNode, "DDDPrestador", nota.Documento.TDFe.TPrestador.TContato.FDDD ?? "");
            Extensions.CriarNo(doc, rpsNode, "TelefonePrestador", fonePrestador);
            Extensions.CriarNo(doc, rpsNode, "DDDTomador", nota.Documento.TDFe.TTomador.TContato.FDDD ?? "");
            Extensions.CriarNo(doc, rpsNode, "TelefoneTomador", foneTomador);

            #region Itens

            var itensNode = Extensions.CriarNo(doc, rpsNode, "Itens");

            #region Item

            foreach (var x in nota.Documento.TDFe.TServico.TItemServico)
            {
                var itemNode = Extensions.CriarNo(doc, itensNode, "Item");
                Extensions.CriarNo(doc, itemNode, "DiscriminacaoServico", Generico.TratarString(x.FDescricao));
                Extensions.CriarNo(doc, itemNode, "Quantidade", FormataValor(x.FQuantidade));
                var valor = Math.Round((x.FValorTotal / x.FQuantidade), 4);
                Extensions.CriarNo(doc, itemNode, "ValorUnitario", valor.ToString().Replace(",", "."));
                Extensions.CriarNo(doc, itemNode, "ValorTotal", FormataValor(Math.Truncate((valor * x.FQuantidade) * 100) / 100));
                Extensions.CriarNo(doc, itemNode, "Tributavel", x.FAliquota == 0 ? "N" : "S");
            }

            #endregion Item

            #endregion Itens

            #endregion RPS

            #endregion Lote

            return doc;
        }

        private TEndereco BuscarMunicipioPrestacao(NFSeNota nota)
        {
            TEndereco endereco = new TEndereco();
            //Se for retido e o cliente for de cidade diferente - E - Não Incidência no Município - Municipio de tributação é Tomador
            if (((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido) == EnumNFSeSituacaoTributaria.stRetencao &&
                    nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio != nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio
                  )
            {
                endereco = nota.Documento.TDFe.TTomador.TEndereco;
            }
            else {
                endereco = nota.Documento.TDFe.TPrestador.TEndereco;
            }
            return endereco;
        }

        private static string ValidaTributacao(NFSeNota nota)
        {
            
            //    public enum EnumNFSeRegTributario GESTOR
            //{
            //    retNenhum = 0,
            //    retMicroempresaMunicipal = 1,
            //    retEstimativa = 2,
            //    retSociedadeProfissionais = 3,
            //    retCooperativa = 4,
            //    retMicroempresarioIndividual = 5,
            //    retMicroempresarioEmpresaPP = 6,
            //    retLucroReal = 7,
            //    retLucroPresumido = 8,
            //    retSimplesNacional = 9
            //}

            // Layout Prefeitura
            //C - Isenta de ISS
            //E - Não Incidência no Município
            //F – Imune
            //K – Exigibilidade Sus.Dec.J / Proc.A
            //N - Não tributável
            //T - Tributável
            //G - Tributável Fixo
            //H – Tributável S.N.
            //M – Micro Empreendedor Individual(MEI) 


            if (nota.Documento.TDFe.Tide.FOptanteSimplesNacional == 1) {
                //Se for retido e o cliente for de cidade diferente - E - Não Incidência no Município
                if (((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido) == EnumNFSeSituacaoTributaria.stRetencao 
                        && nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio != nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio)
                {
                    return "E";
                }

                return "H";
            }
            return nota.Documento.TDFe.TServico.FTributacao;
        }

        private string GetAliquotaAtividade(NFSeNota nota)
        {
            switch (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                case "3552205": /*Sorocaba*/
                    if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "J" 
                            && (((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido) != EnumNFSeSituacaoTributaria.stRetencao)
                            && nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio)
                    {
                        return "0";
                    }
                    else if(ValidaTributacao(nota) == "H" && TipoRecolhimento((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido, 1) == "A")
                    {
                        return "0";
                    }
                    else
                    {
                        return FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota);
                    }
                    
                case "3170206":
                    if ((((EnumNFSeSituacaoTributaria)nota.Documento.TDFe.TServico.FValores.FIssRetido) != EnumNFSeSituacaoTributaria.stRetencao)
                            && nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio)
                    {
                        return "0";
                    }
                    else
                    {
                        return FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota);
                    }

                default:
                    return FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota);
            }
 
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI))
                throw new ArgumentException(nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio + ": Código da cidade do prestador padrão SIAFI não pode ser nulo.");

            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            var gerarNotaNode = doc.CreateElement("ns1", "ReqConsultaLote", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "tipos", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://localhost:8080/WsNFe2/tp";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://localhost:8080/WsNFe2/lote http://localhost:8080/WsNFe2/xsd/ReqConsultaLote.xsd";
            gerarNotaNode.SetAttributeNode(schemaLocation);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");

            Extensions.CriarNo(doc, cabecalhoNode, "CodCidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "Versao", "1");
            Extensions.CriarNo(doc, cabecalhoNode, "NumeroLote", numeroLote.ToString());

            #endregion Cabecalho

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI))
                throw new ArgumentException(nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio + ": Código da cidade do prestador padrão SIAFI não pode ser nulo.");

            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            var gerarNotaNode = doc.CreateElement("ns1", "ReqConsultaNotas", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "tipos", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://localhost:8080/WsNFe2/tp";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://localhost:8080/WsNFe2/lote http://localhost:8080/WsNFe2/xsd/ReqConsultaNotas.xsd";
            gerarNotaNode.SetAttributeNode(schemaLocation);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");

            Extensions.CriarNo(doc, cabecalhoNode, "CodCidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "InscricaoMunicipalPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, cabecalhoNode, "dtInicio", emissao.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "dtFim", emissao.ToString("yyyy-MM-dd"));
            Extensions.CriarNo(doc, cabecalhoNode, "NotaInicial", numeroNFSe);
            Extensions.CriarNo(doc, cabecalhoNode, "Versao", "1");           
            
            #endregion Cabecalho

            return doc;
        }

        private bool MesmaNota(string numeroNF, string numNF)
        {
            long numero1 = 0;
            long numero2 = 0;
            long.TryParse(numeroNF, out numero1);
            long.TryParse(numNF, out numero2);

            return (numero1 == numero2 && numero1 != 0);
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            if (nota.Provedor.Nome != EnumProvedor.DSF)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var sucesso = false;
            var numeroNF = "";
            var numeroRPS = "";
            DateTime? dataEmissaoRPS = null;
            var situacaoRPS = "";
            var motCancelamento = "";
            var codigoVerificacao = "";
            var linkImpressao = "";
            long numeroLote = 0;
            var descricaoProcesso = "";
            var descricaoErro = "";
            var area = EnumArea.Nenhum;
            var codigoPrimeiroErroOuAlerta = "";
            var codigoErroOuAlerta = "";
            var qtdNotasProcessadas = 0;

            bool bErro = false;

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo);

                Regex badAmpersand = new Regex("&(?![a-zA-Z]{2,6};|#[0-9]{2,4};)");
                const string goodAmpersand = "&amp;";
                string data = File.ReadAllText(arquivo);
                data = badAmpersand.Replace(data, goodAmpersand);

                byte[] bytes = Encoding.ASCII.GetBytes(data);
                var teste = new MemoryStream(bytes);

                using (XmlReader x = XmlReader.Create(teste))
                {
                    while (x.Read())
                    {
                        if (x.NodeType == XmlNodeType.Element && x.Name == "Cabecalho")
                        {
                            area = EnumArea.Cabecalho;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Alerta")
                        {
                            area = EnumArea.Alerta;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Erro")
                        {
                            area = EnumArea.Erro;
                            bErro = true;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "ConsultaNFSe")
                        {
                            area = EnumArea.NFSe;
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Nota")
                        {
                            area = EnumArea.Nota;
                        }

                        #region Cabecalho
                        if (area == EnumArea.Cabecalho)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Sucesso")
                            {
                                sucesso = x.ReadString() == "true";
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroLote")
                            {
                                long.TryParse(x.ReadString(), out numeroLote);
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "QtdNotasProcessadas")
                            {
                                int.TryParse(x.ReadString(), out qtdNotasProcessadas);
                            }
                        }
                        #endregion Cabecalho
                        #region Alerta
                        else if (area == EnumArea.Alerta)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Codigo")
                            {
                                if (codigoPrimeiroErroOuAlerta == "")
                                {
                                    codigoPrimeiroErroOuAlerta = x.ReadString();
                                }
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Descricao")
                            {
                                if (string.IsNullOrEmpty(descricaoProcesso))
                                {
                                    descricaoProcesso = string.Concat("[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                                else
                                {
                                    descricaoProcesso = string.Concat(descricaoProcesso, "\n", "[", codigoErroOuAlerta, "] ", x.ReadString());
                                    codigoErroOuAlerta = "";
                                }
                            }
                        }
                        #endregion Alerta
                        #region Erro
                        else if (area == EnumArea.Erro)
                        {
                            if (x.NodeType == XmlNodeType.Element && x.Name == "Codigo")
                            {
                                if (codigoPrimeiroErroOuAlerta == "")
                                {
                                    codigoPrimeiroErroOuAlerta = x.ReadString();
                                }
                                codigoErroOuAlerta = x.ReadString();
                            }
                            else if (x.NodeType == XmlNodeType.Element && x.Name == "Descricao")
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
                        #region ConsultaNFSe
                        else if (area == EnumArea.NFSe)
                        {
                            if (!bErro) //SE NÃO OCORRER ERRO ENTÃO CONTINUA A LEITURA DA CONSULTA
                            {
                                if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroNFe")
                                {
                                    numeroNF = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "CodigoVerificacao")
                                {
                                    codigoVerificacao = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "DataEmissaoRPS")
                                {
                                    DateTime emissao;
                                    DateTime.TryParse(x.ReadString(), out emissao);
                                    dataEmissaoRPS = emissao;
                                }
                            }                            
                        }
                        #endregion ConsultaNFSe
                        #region Nota
                        else if (area == EnumArea.Nota)
                        {
                            if (!bErro) //SE NÃO OCORRER ERRO ENTÃO CONTINUA A LEITURA DA CONSULTA
                            {
                                if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroNota")
                                {
                                    if (string.IsNullOrEmpty(numeroNF) || numeroNF == "0")
                                    {
                                        numeroNF = x.ReadString();
                                    }
                                    else
                                    {
                                        break;
                                    }

                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroLote" && MesmaNota(numeroNF, numNF))
                                {
                                    long.TryParse(x.ReadString(), out numeroLote);
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "CodigoVerificacao" && MesmaNota(numeroNF, numNF))
                                {
                                    codigoVerificacao = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "URLNotaFiscal" && MesmaNota(numeroNF, numNF))
                                {
                                    linkImpressao = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "NumeroRPS" && MesmaNota(numeroNF, numNF))
                                {
                                    numeroRPS = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "DataEmissaoRPS" && MesmaNota(numeroNF, numNF))
                                {
                                    DateTime emissao;
                                    DateTime.TryParse(x.ReadString(), out emissao);
                                    dataEmissaoRPS = emissao;
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "SituacaoRPS" && MesmaNota(numeroNF, numNF))
                                {
                                    situacaoRPS = x.ReadString();
                                }
                                else if (x.NodeType == XmlNodeType.Element && x.Name == "MotCancelamento" && MesmaNota(numeroNF, numNF))
                                {
                                    motCancelamento = x.ReadString();
                                }
                            }                            
                        }
                        #endregion Nota
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
            if ((sucesso && qtdNotasProcessadas != 0) ||
                (!string.IsNullOrEmpty(numNF) && MesmaNota(numeroNF, numNF) && situacaoRPS != ""))
            {
                sucesso = true;
                success = "Sucesso";
            }
            else
            {
                error = xMotivo;
                if (string.IsNullOrEmpty(xMotivo))
                {
                    error = "Não foi possível finalizar a transmissão. Tente novamente mais tarde ou execute uma consulta.";
                }
            }

            var cStat = "";
            var xml = "";
            if (sucesso && (situacaoRPS == "N" || situacaoRPS == ""))
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
                var xmlRetorno = nota.MontarXmlRetorno(nota, numeroNF, codigoVerificacao);
                xml = System.Text.Encoding.GetEncoding("utf-8").GetString(xmlRetorno);
            }

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.ToString().Trim() == "3303500" 
                    && String.IsNullOrEmpty(linkImpressao))
            {
                if (codigoVerificacao != "" && numeroNF.ToString().Trim() != "")
                {
                    linkImpressao = "http://nfse.novaiguacu.rj.gov.br/nfse/QRCode.php" + "?cnpj=" + nota.Documento.TDFe.TPrestador.FCnpj + "&nota=" + Generico.RetornarNumeroZerosEsquerda(numeroNF.ToString().Trim(), 8) + "&codigo=" + codigoVerificacao.Trim(); 
                }
            }

            

            return new RetornoTransmitir(error, success)
            {
                chave = numeroNF != "" && numeroNF != "0" ?
                            GerarChaveNFSe(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FEmitIBGEUF, nota.Documento.TDFe.Tide.DataEmissaoRps, nota.Documento.TDFe.TPrestador.FCnpj, numeroNF, 56) : "",
                cStat = cStat,
                xMotivo = xMotivo,
                numero = numeroNF,
                LinkImpressao = linkImpressao,
                xml = xml,
                digVal = codigoVerificacao,
                NumeroLote = numeroLote,
                NumeroRPS = numeroRPS,
                DataEmissaoRPS = dataEmissaoRPS,
                dhRecbto = dhRecbto,
                CodigoRetornoPref = codigoPrimeiroErroOuAlerta
            };
        }

        public override XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            throw new ArgumentException("Executar metodo GerarXmlConsulta");
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI))
                throw new ArgumentException(nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio + ": Código da cidade do prestador padrão SIAFI não pode ser nulo.");

            var doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            var gerarNotaNode = doc.CreateElement("ns1", "ReqCancelamentoNFSe", "http://localhost:8080/WsNFe2/lote");

            var nsAttributeTipos = doc.CreateAttribute("xmlns", "tipos", "http://www.w3.org/2000/xmlns/");
            nsAttributeTipos.Value = "http://localhost:8080/WsNFe2/tp";
            gerarNotaNode.Attributes.Append(nsAttributeTipos);

            var nsAttribute = doc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            gerarNotaNode.Attributes.Append(nsAttribute);

            var schemaLocation = doc.CreateAttribute("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
            schemaLocation.Value = "http://localhost:8080/WsNFe2/lote http://localhost:8080/WsNFe2/xsd/ReqCancelamentoNFSe.xsd";
            gerarNotaNode.SetAttributeNode(schemaLocation);

            doc.AppendChild(gerarNotaNode);

            #region Cabecalho

            var cabecalhoNode = Extensions.CriarNo(doc, gerarNotaNode, "Cabecalho");

            Extensions.CriarNo(doc, cabecalhoNode, "CodCidade", nota.Documento.TDFe.TPrestador.TEndereco.FSIAFI);
            Extensions.CriarNo(doc, cabecalhoNode, "CPFCNPJRemetente", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNo(doc, cabecalhoNode, "transacao", "true");
            Extensions.CriarNo(doc, cabecalhoNode, "Versao", "1");

            #endregion Cabecalho

            #region Lote

            var loteNode = Extensions.CriarNo(doc, gerarNotaNode, "Lote", "", "Id", string.Concat("L", nota.Documento.TDFe.Tide.FNumeroLote.ToString()));

            #region Nota

            var notaNode = Extensions.CriarNo(doc, loteNode, "Nota", "", "Id", string.Concat("N", numeroNFSe));

            Extensions.CriarNo(doc, notaNode, "InscricaoMunicipalPrestador", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNo(doc, notaNode, "NumeroNota", numeroNFSe);
            Extensions.CriarNo(doc, notaNode, "CodigoVerificacao", codigoVerificacao);
            Extensions.CriarNo(doc, notaNode, "MotivoCancelamento", motivo);

            #endregion Nota

            #endregion Lote

            return doc;
        }

        public override RetornoTransmitir ValidarNFSe(NFSeNota nota)
        {
            if (nota.Provedor.Nome != EnumProvedor.DSF)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            var errors = new List<string>();
            var success = "sucesso";

            //Validacao Generica
            var error = Validacao.ValidaNFSe(nota);
            if (error.Count > 0)
            {
                errors.AddRange(error);
            }

            #region Validações do Provedor
            if (nota.Documento.TDFe.Tide.DataEmissaoRps < new DateTime(2006, 06, 01))
            {
                errors.Add(@"Data Inicial no cabeçalho do lote deve ser superior a 01/06/2006");
            }
            if (nota.Documento.TDFe.Tide.DataEmissaoRps > DateTime.Now)
            {
                errors.Add(@"");
            }
            #endregion

            //Transforma lista de erros em string para retornar
            var retornoErro = "";
            if (errors.Count > 0)
            {
                retornoErro = string.Join(";", errors.ToArray());
                success = "";
            }

            return new RetornoTransmitir(retornoErro, success)
            {
            };
        }

    }
}
