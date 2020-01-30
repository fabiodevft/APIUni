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
    internal class Provedor_Bauru_SP : AbstractProvedor, IProvedor
    {
        internal Provedor_Bauru_SP()
        {
            this.Nome = EnumProvedor.Bauru_SP;
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
        private XmlElement CriaHeaderXml(string strNomeMetodo, ref XmlDocument doc)
        {
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var gerarNotaNode = doc.CreateElement(strNomeMetodo);
            //var nsAttributeTipos = doc.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
            //nsAttributeTipos.Value = "http://www.betha.com.br/e-nota-contribuinte-ws";
            //gerarNotaNode.Attributes.Append(nsAttributeTipos);

            doc.AppendChild(gerarNotaNode);
            return gerarNotaNode;
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            var doc = new XmlDocument();

            #region GerarNota
            var nodeGerarNota = CriaHeaderXml("GerarNota", ref doc);

            #region DescricaoRps
            var nodeLoteRps = Extensions.CriarNo(doc, nodeGerarNota, "DescricaoRps", "");

            Extensions.CriarNoNotNull(doc, nodeLoteRps, "ccm", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "crc", );
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "crc_estado", );

            if(nota.Documento.TDFe.Tide.FOptanteSimplesNacional.ToString() == "1" || 
                (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio))
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "aliquota_simples", nota.Documento.TDFe.TServico.FValores.FAliquota > 0 ? FormataValor(nota.Documento.TDFe.TServico.FValores.FAliquota / 100, 4) : "0.00");
            }
                        
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "id_sis_legado", );
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "servico", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TServico.FItemListaServico));

            var situacao = "";
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

            Extensions.CriarNoNotNull(doc, nodeLoteRps, "situacao", situacao);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "valor", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorServicos, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "base", FormataValor(nota.Documento.TDFe.TServico.FValores.FBaseCalculo, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "descricaoNF", Generico.TratarString(nota.Documento.TDFe.TServico.FDiscriminacao).Replace("\r\n", "|"));

            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_tipo", "2");
            }
            else if(nota.Documento.TDFe.TServico.FMunicipioIncidencia == nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_tipo", "3");
            }
            else
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_tipo", "4");
            }

            
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_cnpj", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj);
            if(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa != "F")
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_email", nota.Documento.TDFe.TTomador.TContato.FEmail);
            }
            
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_ie", );
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_im", nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal);

            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_razao", nota.Documento.TDFe.TTomador.FRazaoSocial);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_fantasia", nota.Documento.TDFe.TTomador.FNomeFantasia);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_endereco", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_numero", nota.Documento.TDFe.TTomador.TEndereco.FNumero);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_complemento", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_bairro", Generico.TratarString(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_CEP", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TEndereco.FCEP));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_cod_cidade", nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_fone", Generico.RetornarApenasNumeros(nota.Documento.TDFe.TTomador.TContato.FDDD + nota.Documento.TDFe.TTomador.TContato.FFone));
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_ramal", );
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "tomador_fax", );
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "rps_num", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "rps_serie", nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie);
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "rps_dia", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd"));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "rps_mes", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("MM"));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "rps_ano", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy"));

            if (nota.Documento.TDFe.TServico.FMunicipioIncidencia != nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
            {
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "outro_municipio", "1");
                Extensions.CriarNoNotNull(doc, nodeLoteRps, "cod_outro_municipio", nota.Documento.TDFe.TServico.FMunicipioIncidencia);
            }
                        
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "retencao_iss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIss, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "pis", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorPis, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "cofins", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "inss", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorInss, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "irrf", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorIr, 2));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "csll", FormataValor(nota.Documento.TDFe.TServico.FValores.FValorCofins, 2));
            //Extensions.CriarNoNotNull(doc, nodeLoteRps, "tipo_obra", );
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "dia_emissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("dd"));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "mes_emissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("MM"));
            Extensions.CriarNoNotNull(doc, nodeLoteRps, "ano_emissao", nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy"));

            #endregion fim - DescricaoRps

            #endregion fim - GerarNota

            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            var doc = new XmlDocument();

            #region ConsultarNotaPrestador
            var nodeConsultarNotaPrestador = CriaHeaderXml("ConsultarNotaPrestador", ref doc);

            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "ccm", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "aliquota_simples", nota.Documento.TDFe.TServico.FValores.FAliquota.ToString());
            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "idSisLegado", nota.Documento.TDFe.Tide.FnProtocolo);
            Extensions.CriarNoNotNull(doc, nodeConsultarNotaPrestador, "Nota", nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero);
            
            #endregion fim - ConsultarNotaPrestador

            return doc;
        }
        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            var doc = new XmlDocument();

            #region CancelarNota
            var nodeCancelarNota = CriaHeaderXml("CancelarNota", ref doc);

            #region DadosCancelaNota
            var nodeDadosCancelaNota = Extensions.CriarNo(doc, nodeCancelarNota, "DadosCancelaNota", "");

            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "ccm", nota.Documento.TDFe.TPrestador.FInscricaoMunicipal);
            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "cnpj", nota.Documento.TDFe.TPrestador.FCnpj);
            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "senha", nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha);
            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "nota", numeroNFSe);
            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "motivo", motivo);
            Extensions.CriarNoNotNull(doc, nodeDadosCancelaNota, "email", nota.Documento.TDFe.TPrestador.TContato.FEmail);

            #endregion fim - DadosCancelaNota

            #endregion fim - CancelarNota

            return doc;
        }

    }
}
