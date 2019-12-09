using NFe.API.Domain;
using NFe.API.Enum;
using NFe.API.Provedor;
using NFe.API.Util;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using static NFe.API.Util.Extensions;

namespace FRGDocFiscal.Provedor
{
    internal class Provedor_SigCorpSigISS : AbstractProvedor, IProvedor
    {
        internal Provedor_SigCorpSigISS()
        {
            this.Nome = EnumProvedor.SigCorpSigISS;
        }

        public virtual string Concatenar(string texto1, string texto2)
        {
            var msg = (texto1 ?? "").Trim();
            if (!string.IsNullOrEmpty(texto2))
            {
                if (msg != "")
                {
                    msg = string.Concat(msg, "\n");
                }
                msg = string.Concat(msg, texto2 ?? "");
            }

            return msg;
        }

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode gerarNotaNode = doc.CreateElement("GerarNota");
            //XmlAttribute gerarNotaAttribute = doc.CreateAttribute("versao");
            //gerarNotaAttribute.Value = "1.0";
            //gerarNotaNode.Attributes.Append(gerarNotaAttribute);
            doc.AppendChild(gerarNotaNode);

            XmlNode descricaoRpsNode = doc.CreateElement("DescricaoRps");
            gerarNotaNode.AppendChild(descricaoRpsNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FCCM))
                throw new ArgumentException("Usuário para acesso ao sistema da prefeitura não encontrada.");

            XmlNode ccmNode = doc.CreateElement("ccm");
            ccmNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCCM));
            descricaoRpsNode.AppendChild(ccmNode);

            XmlNode cnpjNode = doc.CreateElement("cnpj");
            cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCnpj));
            descricaoRpsNode.AppendChild(cnpjNode);

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina CPF obrigatorio*/
            {
                XmlNode cpfNode = doc.CreateElement("cpf");
                cpfNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario));
                descricaoRpsNode.AppendChild(cpfNode);
            }

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha))
                throw new ArgumentException("Senha para acesso ao sistema da prefeitura não encontrada.");

            XmlNode senhaNode = doc.CreateElement("senha");
            senhaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha));
            descricaoRpsNode.AppendChild(senhaNode);

            //XmlNode crcNode = doc.CreateElement("crc");
            //crcNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(crcNode);

            //XmlNode crc_estadoNode = doc.CreateElement("crc_estado");
            //crc_estadoNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(crc_estadoNode);

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina Versão 1.01*/
            {
                /*Obrigatório.
                    aliquota : Serviços isentos, imunes ou com tributação fixa:
                                informar “0,00”. (Adicionado na versão 1.01)
                    
                    cod_municipio_prestacao_servico: Código do município em que o serviço foi
                                                     prestado

                    cod_municipio_incidencia: Código do município onde ocorre a incidência do
                                                ISSQN.
                 */
                XmlNode aliquotaNode = doc.CreateElement("aliquota");
                aliquotaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FAliquota.ToString()));
                descricaoRpsNode.AppendChild(aliquotaNode);

                XmlNode cod_municipio_prestacao_servicoNode = doc.CreateElement("cod_municipio_prestacao_servico");
                cod_municipio_prestacao_servicoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FCodigoMunicipio.ToString()));
                descricaoRpsNode.AppendChild(cod_municipio_prestacao_servicoNode);

                XmlNode cod_municipio_incidenciaNode = doc.CreateElement("cod_municipio_incidencia");
                cod_municipio_incidenciaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FMunicipioIncidencia.ToString()));
                descricaoRpsNode.AppendChild(cod_municipio_incidenciaNode);
            }
            else
            {
                XmlNode aliquota_simplesNode = doc.CreateElement("aliquota_simples");
                aliquota_simplesNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FAliquota.ToString()));
                descricaoRpsNode.AppendChild(aliquota_simplesNode);
            }

            XmlNode id_sis_legadoNode = doc.CreateElement("id_sis_legado");
            id_sis_legadoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString()));
            descricaoRpsNode.AppendChild(id_sis_legadoNode);

            XmlNode servicoNode = doc.CreateElement("servico");
            servicoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FItemListaServico));
            descricaoRpsNode.AppendChild(servicoNode);

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

            XmlNode situacaoNode = doc.CreateElement("situacao");
            situacaoNode.AppendChild(doc.CreateTextNode(situacao));
            descricaoRpsNode.AppendChild(situacaoNode);

            XmlNode valorNode = doc.CreateElement("valor");
            valorNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FValorServicos.ToString()));
            descricaoRpsNode.AppendChild(valorNode);

            XmlNode baseNode = doc.CreateElement("base");
            baseNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FBaseCalculo.ToString()));
            descricaoRpsNode.AppendChild(baseNode);

            var discriminacao = nota.Documento.TDFe.TServico.FDiscriminacao;

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == "4113700")             // Londrina
            {
                discriminacao = Concatenar(discriminacao, nota.Documento.TDFe.Tide.FOutrasInformacoes);
            }
            else if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == "3529005")        // Marilia
            {
                discriminacao = Concatenar(discriminacao, nota.Documento.TDFe.Tide._FMsgComplementares);
            }

            XmlNode descricaoNFNode = doc.CreateElement("descricaoNF");
            descricaoNFNode.AppendChild(doc.CreateTextNode(discriminacao));
            descricaoRpsNode.AppendChild(descricaoNFNode);

            var tipoTomador = "";

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa))
                throw new ArgumentException("Tipo de pessoa do tomador não identificado");

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TTomador.TEndereco.FUF))
                throw new ArgumentException("UF do tomador não pode ser nulo");

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.TEndereco.FUF))
                throw new ArgumentException("UF do prestador não pode ser nulo");

            if (nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FPessoa == "F")
            {
                tipoTomador = "2";              // Pessoa Física
            }
            else
            {
                if (nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio == nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio)
                {
                    tipoTomador = "3";          // Jurídica do Município
                }
                else if (nota.Documento.TDFe.TTomador.TEndereco.FUF == "EX")
                {
                    tipoTomador = "5";          // Jurídica de Fora do País
                }
                else
                {
                    tipoTomador = "4";          // Jurídica de Fora
                }
            }

            XmlNode tomador_tipoNode = doc.CreateElement("tomador_tipo");
            tomador_tipoNode.AppendChild(doc.CreateTextNode(tipoTomador));
            descricaoRpsNode.AppendChild(tomador_tipoNode);

            XmlNode tomador_cnpjNode = doc.CreateElement("tomador_cnpj");
            tomador_cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj));
            descricaoRpsNode.AppendChild(tomador_cnpjNode);

            XmlNode tomador_emailNode = doc.CreateElement("tomador_email");
            tomador_emailNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TContato.FEmail));
            descricaoRpsNode.AppendChild(tomador_emailNode);

            XmlNode tomador_ieNode = doc.CreateElement("tomador_ie");
            tomador_ieNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoEstadual));
            descricaoRpsNode.AppendChild(tomador_ieNode);

            XmlNode tomador_imNode = doc.CreateElement("tomador_im");
            tomador_imNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FInscricaoMunicipal));
            descricaoRpsNode.AppendChild(tomador_imNode);

            XmlNode tomador_razaoNode = doc.CreateElement("tomador_razao");
            tomador_razaoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.FRazaoSocial));
            descricaoRpsNode.AppendChild(tomador_razaoNode);

            var fantasia = nota.Documento.TDFe.TTomador.FNomeFantasia ?? "";
            if (string.IsNullOrEmpty(fantasia))
                fantasia = nota.Documento.TDFe.TTomador.FRazaoSocial;
            XmlNode tomador_fantasiaNode = doc.CreateElement("tomador_fantasia");
            tomador_fantasiaNode.AppendChild(doc.CreateTextNode(fantasia));
            descricaoRpsNode.AppendChild(tomador_fantasiaNode);

            XmlNode tomador_enderecoNode = doc.CreateElement("tomador_endereco");
            tomador_enderecoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FEndereco));
            descricaoRpsNode.AppendChild(tomador_enderecoNode);

            XmlNode tomador_numeroNode = doc.CreateElement("tomador_numero");
            tomador_numeroNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FNumero));
            descricaoRpsNode.AppendChild(tomador_numeroNode);

            XmlNode tomador_complementoNode = doc.CreateElement("tomador_complemento");
            tomador_complementoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FComplemento));
            descricaoRpsNode.AppendChild(tomador_complementoNode);

            XmlNode tomador_bairroNode = doc.CreateElement("tomador_bairro");
            tomador_bairroNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FBairro));
            descricaoRpsNode.AppendChild(tomador_bairroNode);

            XmlNode tomador_CEPNode = doc.CreateElement("tomador_CEP");
            tomador_CEPNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FCEP));
            descricaoRpsNode.AppendChild(tomador_CEPNode);

            XmlNode tomador_cod_cidadeNode = doc.CreateElement("tomador_cod_cidade");
            tomador_cod_cidadeNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TEndereco.FCodigoMunicipio));
            descricaoRpsNode.AppendChild(tomador_cod_cidadeNode);

            XmlNode tomador_foneNode = doc.CreateElement("tomador_fone");
            tomador_foneNode.AppendChild(doc.CreateTextNode(Strings.FoneComDDD(nota.Documento.TDFe.TTomador.TContato.FDDD, nota.Documento.TDFe.TTomador.TContato.FFone)));
            descricaoRpsNode.AppendChild(tomador_foneNode);

            //XmlNode tomador_ramalNode = doc.CreateElement("tomador_ramal");
            //tomador_ramalNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(tomador_ramalNode);

            //XmlNode tomador_faxNode = doc.CreateElement("tomador_fax");
            //tomador_faxNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(tomador_faxNode);

            XmlNode rps_numNode = doc.CreateElement("rps_num");
            rps_numNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero.ToString()));
            descricaoRpsNode.AppendChild(rps_numNode);
            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina Versão 1.01*/
            {
                XmlNode rps_tipoNode = doc.CreateElement("rps_tipo");
                rps_tipoNode.AppendChild(doc.CreateTextNode("1"));
                descricaoRpsNode.AppendChild(rps_tipoNode);
            }
            XmlNode rps_serieNode = doc.CreateElement("rps_serie");
            rps_serieNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie));
            descricaoRpsNode.AppendChild(rps_serieNode);

            XmlNode rps_diaNode = doc.CreateElement("rps_dia");
            rps_diaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Day.ToString()));
            descricaoRpsNode.AppendChild(rps_diaNode);

            XmlNode rps_mesNode = doc.CreateElement("rps_mes");
            rps_mesNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Month.ToString()));
            descricaoRpsNode.AppendChild(rps_mesNode);

            XmlNode rps_anoNode = doc.CreateElement("rps_ano");
            rps_anoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Year.ToString()));
            descricaoRpsNode.AppendChild(rps_anoNode);

            //XmlNode outro_municipioNode = doc.CreateElement("outro_municipio");
            //outro_municipioNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(outro_municipioNode);

            //XmlNode cod_outro_municipioNode = doc.CreateElement("cod_outro_municipio");
            //cod_outro_municipioNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(cod_outro_municipioNode);


            //XmlNode retencao_issNode = doc.CreateElement("retencao_iss");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);


            //XmlNode pisNode = doc.CreateElement("pis");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);


            //XmlNode cofinsNode = doc.CreateElement("cofins");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);

            //XmlNode inssNode = doc.CreateElement("inss");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);

            //XmlNode irrfNode = doc.CreateElement("irrf");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);

            //XmlNode csllNode = doc.CreateElement("csll");
            //senhaNode.AppendChild(doc.CreateTextNode("?"));
            //descricaoRpsNode.AppendChild(senhaNode);
            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio == "4105508"/*CiaNorte*/)
            {
                //nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FEmitIBGEUF
                XmlNode prest_diaNode = doc.CreateElement("dia_prest_servico");
                prest_diaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Day.ToString()));
                descricaoRpsNode.AppendChild(prest_diaNode);

                XmlNode prest_mesNode = doc.CreateElement("mes_prest_servico");
                prest_mesNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Month.ToString()));
                descricaoRpsNode.AppendChild(prest_mesNode);

                XmlNode prest_anoNode = doc.CreateElement("ano_prest_servico");
                prest_anoNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissaoRps.Year.ToString()));
                descricaoRpsNode.AppendChild(prest_anoNode);
            }
            else if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina Versão 1.01*/
            {
                XmlNode rps_numSpecifiedNode = doc.CreateElement("rps_numSpecified");
                rps_numSpecifiedNode.AppendChild(doc.CreateTextNode("True"));
                descricaoRpsNode.AppendChild(rps_numSpecifiedNode);

                XmlNode rps_tipoSpecifiedNode = doc.CreateElement("rps_tipoSpecified");
                rps_tipoSpecifiedNode.AppendChild(doc.CreateTextNode("True"));
                descricaoRpsNode.AppendChild(rps_tipoSpecifiedNode);

                XmlNode rps_diaSpecifiedNode = doc.CreateElement("rps_diaSpecified");
                rps_diaSpecifiedNode.AppendChild(doc.CreateTextNode("True"));
                descricaoRpsNode.AppendChild(rps_diaSpecifiedNode);

                XmlNode rps_mesSpecifiedNode = doc.CreateElement("rps_mesSpecified");
                rps_mesSpecifiedNode.AppendChild(doc.CreateTextNode("True"));
                descricaoRpsNode.AppendChild(rps_mesSpecifiedNode);

                XmlNode rps_anoSpecifiedNode = doc.CreateElement("rps_anoSpecified");
                rps_anoSpecifiedNode.AppendChild(doc.CreateTextNode("True"));
                descricaoRpsNode.AppendChild(rps_anoSpecifiedNode);
            }
            return doc;
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode consultarNotaPrestadorNode = doc.CreateElement("urn", "ConsultarNotaPrestador", "urn:sigiss_ws");
            doc.AppendChild(consultarNotaPrestadorNode);

            XmlNode dadosPrestadorNode = doc.CreateElement("DadosPrestador");
            consultarNotaPrestadorNode.AppendChild(dadosPrestadorNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FCCM))
                throw new ArgumentException("Usuário para acesso ao sistema da prefeitura não encontrada.");

            XmlNode ccmNode = doc.CreateElement("ccm");
            ccmNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCCM));
            dadosPrestadorNode.AppendChild(ccmNode);

            XmlNode cnpjNode = doc.CreateElement("cnpj");
            cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCnpj));
            dadosPrestadorNode.AppendChild(cnpjNode);

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina CPF obrigatorio*/
            {
                XmlNode cpfNode = doc.CreateElement("cpf");
                cpfNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario));
                dadosPrestadorNode.AppendChild(cpfNode);
            }

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha))
                throw new ArgumentException("Senha para acesso ao sistema da prefeitura não encontrada.");

            XmlNode senhaNode = doc.CreateElement("senha");
            senhaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha));
            dadosPrestadorNode.AppendChild(senhaNode);

            XmlNode aliquota_simplesNode = doc.CreateElement("aliquota_simples");
            aliquota_simplesNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FAliquota.ToString()));
            dadosPrestadorNode.AppendChild(aliquota_simplesNode);

            XmlNode notaNode = doc.CreateElement("Nota");
            notaNode.AppendChild(doc.CreateTextNode(numeroNFSe));
            consultarNotaPrestadorNode.AppendChild(notaNode);

            return doc;
        }

        private string GetHash(string linkImpressao)
        {
            string hash = "";
            int inicio = linkImpressao.ToLower().IndexOf("hash=");
            int fim = linkImpressao.ToLower().IndexOf("nfe=");
            if (inicio != -1 && fim > inicio)
                hash = linkImpressao.Substring(inicio + 5, (fim - inicio - 6));

            return hash;
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo)
        {
            if (nota.Provedor.Nome != EnumProvedor.SigCorpSigISS)
            {
                throw new ArgumentException("Provedor inválido, neste caso é o provedor " + nota.Provedor.Nome.ToString());
            }

            bool sucesso = false;
            string numeroNF = "";
            string linkImpressao = "";
            long id = 0;
            string descricaoProcesso = "";
            string descricaoErro = "";

            if (File.Exists(arquivo))
            {
                var stream = new StreamReader(arquivo, Encoding.GetEncoding("ISO-8859-1"));
                //using (XmlReader x = XmlReader.Create(new StreamReader(arquivo, Encoding.GetEncoding("ISO-8859-1"))))
                //using (XmlReader x = XmlReader.Create(new StreamReader(arquivo)))
                using (XmlReader x = XmlReader.Create(stream))
                {
                   while (x.Read())
                    {
                        if (x.NodeType == XmlNodeType.Element && x.Name == "Resultado")
                            sucesso = x.ReadString() == "1";
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "StatusNfse")
                        {
                            sucesso = true;
                            descricaoErro = x.ReadString() == "1" ? "nota emitida com sucesso" : "cancelada";
                        }
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "Nota" || x.NodeType == XmlNodeType.Element && x.Name == "Numero")
                            numeroNF = x.ReadString();
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "LinkImpressao")
                            linkImpressao = x.ReadString();
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "id")
                            long.TryParse(x.ReadString(), out id);
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "DescricaoProcesso")
                            descricaoProcesso = x.ReadString();
                        else if (x.NodeType == XmlNodeType.Element && x.Name == "DescricaoErro")
                            descricaoErro = x.ReadString();
                    }
                    x.Close();
                }
                stream.Dispose();
            }

           var error = "";
            var success = "";
            var cStat = "";
            var xml = "";
            var hash = "";
            var xMotivo = descricaoErro != "" ? string.Concat(descricaoProcesso, "[", descricaoErro, "]") : descricaoProcesso;
            if (sucesso && (
                                (
                                xMotivo.ToLower().IndexOf("documento: válido") != -1 ||
                                xMotivo.ToLower().IndexOf("nota emitida com sucesso") != -1 ||
                                xMotivo.ToLower().IndexOf("cancelada") != -1 || xMotivo.ToLower().IndexOf("aviso de cancelamento") != -1 ||
                                xMotivo.ToLower().IndexOf("nfs-e enviada por e-mail para o endereço") != -1 ||
                                xMotivo.ToLower().IndexOf("envio de nfs-e") != -1
                                ) 
                                ||
                                (
                                xMotivo.Equals("") && !numeroNF.Equals("") && !linkImpressao.Equals("")
                                )
                            )
                )
            {
                if (xMotivo.ToLower().IndexOf("cancelada") != -1 || xMotivo.ToLower().IndexOf("aviso de cancelamento") != -1)
                {
                    cStat = "101";
                    nota.Documento.TDFe.Tide.FStatus = EnumNFSeRPSStatus.srCancelado;
                }
                else
                {
                    cStat = "100";
                    nota.Documento.TDFe.Tide.FStatus = EnumNFSeRPSStatus.srNormal;
                    xMotivo = "documento: válido";
                    descricaoProcesso = (string.IsNullOrEmpty(descricaoProcesso) ? "documento: válido" : descricaoProcesso);
                }
            }
            else if (xMotivo.ToLower().IndexOf("se encontra cancelada") != -1)
            {
                cStat = "101";
                nota.Documento.TDFe.Tide.FStatus = EnumNFSeRPSStatus.srCancelado;
            }

            if (cStat == "100" || cStat == "101")
            {
                success = descricaoErro != "" ? descricaoErro : descricaoProcesso;
                hash = GetHash(linkImpressao);
                var xmlRetorno = nota.MontarXmlRetorno(nota, numeroNF, hash);
                xml = System.Text.Encoding.GetEncoding("utf-8").GetString(xmlRetorno);
            }
            else
            {
                error = descricaoErro;
            }

            if (xMotivo == "")
                throw new ArgumentException("Nenhuma resposta do monitor UniNFe, tente mais tarde.");

            return new RetornoTransmitir(error, success)
            {
                chave = numeroNF != "" && numeroNF != "0" ?
                            GerarChaveNFSe(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FEmitIBGEUF, nota.Documento.TDFe.Tide.DataEmissaoRps, nota.Documento.TDFe.TPrestador.FCnpj, numeroNF, 56) : "",
                cStat = cStat,
                xMotivo = xMotivo,
                numero = numeroNF,
                LinkImpressao = linkImpressao,
                xml = xml,
                digVal = hash
            };
        }

        //private byte[] MontarXmlRetorno(NFSeNota nota, string numeroNFSe, string codigoVerificacao)
        //{
        //    var po = new NFe
        //    {

        //        Prefeitura = nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio,
        //        InscricaoPrestador = nota.Documento.TDFe.TPrestador.FInscricaoMunicipal,
        //        IEPrestador = nota.Documento.TDFe.TPrestador.FInscricaoEstadual
        //    };

        //    CPFCNPJPrestador cpfCNPJPrestador = new CPFCNPJPrestador();
        //    cpfCNPJPrestador.CNPJ = nota.Documento.TDFe.TPrestador.FCnpj;
        //    po.CPFCNPJPrestador = cpfCNPJPrestador;

        //    ChaveNFe chaveNFe = new ChaveNFe();
        //    chaveNFe.NumeroNFe = numeroNFSe;
        //    chaveNFe.SerieNFe = nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie;
        //    chaveNFe.CodigoVerificacao = codigoVerificacao;
        //    chaveNFe.DataEmissaoNFe = nota.Documento.TDFe.Tide.DataEmissaoRps.ToString("yyyy-MM-dd");
        //    po.ChaveNFe = chaveNFe;

        //    po.RazaoSocialPrestador = nota.Documento.TDFe.TPrestador.FRazaoSocial;

        //    EnderecoPrestador enderecoPrestador = new EnderecoPrestador();
        //    enderecoPrestador.Logradouro = nota.Documento.TDFe.TPrestador.TEndereco.FEndereco;
        //    enderecoPrestador.NumeroEndereco = nota.Documento.TDFe.TPrestador.TEndereco.FNumero;
        //    enderecoPrestador.ComplementoEndereco = nota.Documento.TDFe.TPrestador.TEndereco.FComplemento;
        //    enderecoPrestador.Bairro = nota.Documento.TDFe.TPrestador.TEndereco.FBairro;
        //    enderecoPrestador.Cidade = nota.Documento.TDFe.TPrestador.TEndereco.FxMunicipio;
        //    enderecoPrestador.UF = nota.Documento.TDFe.TPrestador.TEndereco.FUF;
        //    enderecoPrestador.CEP = nota.Documento.TDFe.TPrestador.TEndereco.FCEP;
        //    po.EnderecoPrestador = enderecoPrestador;

        //    po.TelefonePrestador = Strings.FoneComDDD(nota.Documento.TDFe.TPrestador.TContato.FDDD, nota.Documento.TDFe.TPrestador.TContato.FFone);
        //    po.EmailPrestador = nota.Documento.TDFe.TPrestador.TContato.FEmail;
        //    po.StatusNFe = nota.Documento.TDFe.Tide.FStatus == EnumNFSeRPSStatus.srNormal ? "Ativa" : "Cancelada";

        //    var situacao = "";
        //    switch (nota.Documento.TDFe.Tide.FNaturezaOperacao)
        //    {
        //        case 1: { situacao = "Tributado no Prestador"; break; }
        //        case 2: { situacao = "Tributado no Tomador"; break; }
        //        case 3: { situacao = "Isenta"; break; }
        //        case 4: { situacao = "imune"; break; }
        //        default: { situacao = "Não tributada"; break; }
        //    }
        //    po.TributacaoNFe = situacao;

        //    po.OpcaoSimples = (nota.Documento.TDFe.Tide.FOptanteSimplesNacional == 1 ? "SIM" : "NÃO");
        //    po.ValorServicos = nota.Documento.TDFe.TServico.FValores.FValorServicos.ToString();
        //    po.ValorBase = nota.Documento.TDFe.TServico.FValores.FBaseCalculo.ToString();
        //    po.CodigoServico = nota.Documento.TDFe.TServico.FItemListaServico;
        //    po.AliquotaServicos = nota.Documento.TDFe.TServico.FValores.FAliquota.ToString();
        //    po.ValorINSS = nota.Documento.TDFe.TServico.FValores.FValorInss.ToString();
        //    po.ValorIR = nota.Documento.TDFe.TServico.FValores.FValorIr.ToString();
        //    po.ValorPIS = nota.Documento.TDFe.TServico.FValores.FValorPis.ToString();
        //    po.ValorCOFINS = nota.Documento.TDFe.TServico.FValores.FValorCofins.ToString();
        //    po.ValorCSLL = nota.Documento.TDFe.TServico.FValores.FValorCsll.ToString();
        //    po.ValorISS = nota.Documento.TDFe.TServico.FValores.FValorIss.ToString();
        //    po.ISSRetido = nota.Documento.TDFe.TServico.FValores.FValorIssRetido > 0 ? "SIM" : "NÃO";
        //    po.InscricaoTomador = "";                                                                               // ?!

        //    CPFCNPJTomador cpfCNPJTomador = new CPFCNPJTomador();
        //    cpfCNPJTomador.CNPJ = nota.Documento.TDFe.TTomador.TIdentificacaoTomador.FCpfCnpj;
        //    po.CPFCNPJTomador = cpfCNPJTomador;

        //    po.IETomador = "";                                                                                      // ?!
        //    po.RazaoSocialTomador = nota.Documento.TDFe.TTomador.FRazaoSocial;

        //    EnderecoTomador enderecoTomador = new EnderecoTomador();
        //    enderecoTomador.Logradouro = nota.Documento.TDFe.TTomador.TEndereco.FEndereco;
        //    enderecoTomador.NumeroEndereco = nota.Documento.TDFe.TTomador.TEndereco.FNumero;
        //    enderecoTomador.ComplementoEndereco = nota.Documento.TDFe.TTomador.TEndereco.FComplemento;
        //    enderecoTomador.Bairro = nota.Documento.TDFe.TTomador.TEndereco.FBairro;
        //    enderecoTomador.Cidade = nota.Documento.TDFe.TTomador.TEndereco.FxMunicipio;
        //    enderecoTomador.UF = nota.Documento.TDFe.TTomador.TEndereco.FUF;
        //    enderecoTomador.CEP = nota.Documento.TDFe.TTomador.TEndereco.FCEP;
        //    po.EnderecoTomador = enderecoTomador;

        //    po.TelefoneTomador = Strings.FoneComDDD(nota.Documento.TDFe.TTomador.TContato.FDDD, nota.Documento.TDFe.TTomador.TContato.FFone);
        //    po.EmailTomador = nota.Documento.TDFe.TTomador.TContato.FEmail;
        //    po.Discriminacao = nota.Documento.TDFe.TServico.FDiscriminacao;

        //    //using (MemoryStream msEncrypt = new MemoryStream())
        //    //{
        //    //        using (StreamWriter swEncrypt = new StreamWriter(msEncrypt, Encoding.GetEncoding(1252)))
        //    //        {
        //    //            swEncrypt.Write(po);
        //    //        }
        //    //    return Encoding.GetEncoding("ISO-8859-1").GetString(msEncrypt.ToArray());
        //    //}


        //    XmlSerializer serializer = new XmlSerializer(typeof(NFe));
        //    using (MemoryStream memoryStream = new MemoryStream())
        //    {
        //        using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream))
        //        {
        //            serializer.Serialize(xmlWriter, po);
        //            return memoryStream.ToArray();
        //        }
        //    }
        //}

        public override XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina - Consulta Diferente*/
            {
                return GerarXmlConsulta_ConsultarRpsServicoPrestado(nota, numeroNFSe, hash);
            }
            else
            {
                return GerarXmlConsulta_ConsultarNotaValida(nota, numeroNFSe, hash);
            }

        }

        private XmlDocument GerarXmlConsulta_ConsultarRpsServicoPrestado(NFSeNota nota, string numeroNFSe, string hash)
        {
            /*Londrina - Consulta Diferente*/
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode dadosConsultaNotaNode = doc.CreateElement("ConsultarRpsServicoPrestadoEnvio", "urn:sigiss_ws");
            doc.AppendChild(dadosConsultaNotaNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FCCM))
                throw new ArgumentException("Usuário para acesso ao sistema da prefeitura não encontrada.");

            XmlNode ccmNode = doc.CreateElement("ccm");
            ccmNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCCM));
            dadosConsultaNotaNode.AppendChild(ccmNode);

            XmlNode cnpjNode = doc.CreateElement("cnpj");
            cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCnpj));
            dadosConsultaNotaNode.AppendChild(cnpjNode);

            XmlNode cpfNode = doc.CreateElement("cpf");
            cpfNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario));
            dadosConsultaNotaNode.AppendChild(cpfNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha))
                throw new ArgumentException("Senha para acesso ao sistema da prefeitura não encontrada.");

            XmlNode senhaNode = doc.CreateElement("senha");
            senhaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha));
            dadosConsultaNotaNode.AppendChild(senhaNode);

            XmlNode notaNode = doc.CreateElement("numero_rps");
            notaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.FIdentificacaoRps.FNumero));
            dadosConsultaNotaNode.AppendChild(notaNode);


            XmlNode dia_rpsNode = doc.CreateElement("dia_rps");
            dia_rpsNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissao.Day.ToString()));
            dadosConsultaNotaNode.AppendChild(dia_rpsNode);


            XmlNode mes_rpsNode = doc.CreateElement("mes_rps");
            mes_rpsNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissao.Month.ToString()));
            dadosConsultaNotaNode.AppendChild(mes_rpsNode);


            XmlNode ano_rpsNode = doc.CreateElement("ano_rps");
            ano_rpsNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.DataEmissao.Year.ToString()));
            dadosConsultaNotaNode.AppendChild(ano_rpsNode);


            return doc;
        }

        private XmlDocument GerarXmlConsulta_ConsultarNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode consultarNotaValidaNode = doc.CreateElement("urn", "ConsultarNotaValida", "urn:sigiss_ws");
            doc.AppendChild(consultarNotaValidaNode);

            XmlNode dadosConsultaNotaNode = doc.CreateElement("DadosConsultaNota");
            consultarNotaValidaNode.AppendChild(dadosConsultaNotaNode);

            XmlNode notaNode = doc.CreateElement("nota");
            notaNode.AppendChild(doc.CreateTextNode(numeroNFSe));
            dadosConsultaNotaNode.AppendChild(notaNode);

            XmlNode serieNode = doc.CreateElement("serie");
            serieNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.Tide.FIdentificacaoRps.FSerie));
            dadosConsultaNotaNode.AppendChild(serieNode);

            XmlNode valorNode = doc.CreateElement("valor");
            valorNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TServico.FValores.FValorLiquidoNfse.ToString()));
            dadosConsultaNotaNode.AppendChild(valorNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FCCM))
                throw new ArgumentException("Usuário para acesso ao sistema da prefeitura não encontrada.");

            XmlNode ccmNode = doc.CreateElement("prestador_ccm");
            ccmNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCCM));
            dadosConsultaNotaNode.AppendChild(ccmNode);

            XmlNode cnpjNode = doc.CreateElement("prestador_cnpj");
            cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCnpj));
            dadosConsultaNotaNode.AppendChild(cnpjNode);

            XmlNode autenticidadeNode = doc.CreateElement("autenticidade");
            autenticidadeNode.AppendChild(doc.CreateTextNode(hash));
            dadosConsultaNotaNode.AppendChild(autenticidadeNode);

            return doc;
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode CancelarNotaNode = doc.CreateElement("urn", "CancelarNota", "urn:sigiss_ws");
            doc.AppendChild(CancelarNotaNode);

            XmlNode DadosCancelaNotaNode = null;
            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina Versão 1.01*/
            {
                DadosCancelaNotaNode = doc.CreateElement("DescricaoCancelaNota");
            }
            else
            {
                DadosCancelaNotaNode = doc.CreateElement("DadosCancelaNota");
            }

            CancelarNotaNode.AppendChild(DadosCancelaNotaNode);

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FCCM))
                throw new ArgumentException("Usuário para acesso ao sistema da prefeitura não encontrada.");

            XmlNode ccmNode = doc.CreateElement("ccm");
            ccmNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCCM));
            DadosCancelaNotaNode.AppendChild(ccmNode);

            XmlNode cnpjNode = doc.CreateElement("cnpj");
            cnpjNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FCnpj));
            DadosCancelaNotaNode.AppendChild(cnpjNode);

            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina CPF obrigatorio*/
            {
                XmlNode cpfNode = doc.CreateElement("cpf");
                cpfNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario));
                DadosCancelaNotaNode.AppendChild(cpfNode);
            }

            if (string.IsNullOrEmpty(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha))
                throw new ArgumentException("Senha para acesso ao sistema da prefeitura não encontrada.");

            XmlNode senhaNode = doc.CreateElement("senha");
            senhaNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TPrestador.FIdentificacaoPrestador.FSenha));
            DadosCancelaNotaNode.AppendChild(senhaNode);

            XmlNode notaNode = doc.CreateElement("nota");
            notaNode.AppendChild(doc.CreateTextNode(numeroNFSe));
            DadosCancelaNotaNode.AppendChild(notaNode);
            if (nota.Documento.TDFe.TPrestador.TEndereco.FCodigoMunicipio.Equals("4113700"))/*Londrina Versão 1.01*/
            {
                XmlNode motivoNode = doc.CreateElement("cod_cancelamento");
                motivoNode.AppendChild(doc.CreateTextNode("2"));
                DadosCancelaNotaNode.AppendChild(motivoNode);
            }
            else
            {
                XmlNode motivoNode = doc.CreateElement("motivo");
                motivoNode.AppendChild(doc.CreateTextNode(motivo));
                DadosCancelaNotaNode.AppendChild(motivoNode);
            }

            XmlNode tomador_emailNode = doc.CreateElement("email");
            tomador_emailNode.AppendChild(doc.CreateTextNode(nota.Documento.TDFe.TTomador.TContato.FEmail));
            DadosCancelaNotaNode.AppendChild(tomador_emailNode);

            return doc;
        }
    }
}
