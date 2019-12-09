using FRGDocFiscal.Provedor;
using NFe.API.Domain;
using NFe.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using static NFe.API.Domain.Notas;

namespace NFe.API.Provedor
{
    public class Provedor : AbstractProvedor, IProvedor
    {

        #region Variaveis

        private IProvedor _IProvedor;

        #endregion Variaveis

        #region Construtores

        internal Provedor()
        {
        }

        public Provedor(EnumProvedor nomeProvedor)
        {
            try
            {
                InstanciaProvedor(nomeProvedor);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao instanciar objeto.", ex);
            }
        }

        #endregion

        #region Propriedades da Interface

        public override EnumProvedor Nome
        {
            get { return _IProvedor.Nome; }
            set { _IProvedor.Nome = value; }
        }

        #endregion

        #region Métodos Privados

        private void InstanciaProvedor(EnumProvedor nomeProvedor)
        {
            try
            {
                switch (nomeProvedor)
                {
                    //1 - SigCorp/SigISS
                    case EnumProvedor.SigCorpSigISS:
                        _IProvedor = new Provedor_SigCorpSigISS();
                        break;
                    //3 - DSF
                    case EnumProvedor.DSF:
                        _IProvedor = new Provedor_DSF();
                        break;
                    case EnumProvedor.Paulistana:
                        _IProvedor = new Provedor_Paulistana();
                        break;
                    case EnumProvedor.Thema:
                        _IProvedor = new Provedor_Thema();
                        break;
                    case EnumProvedor.Metropolis:
                        _IProvedor = new Provedor_Metropolis();
                        break;
                    case EnumProvedor.BHISS:
                        _IProvedor = new Provedor_BHISS();
                        break;
                    case EnumProvedor.EeL:
                        _IProvedor = new Provedor_EeL();
                        break;
                    case EnumProvedor.Goiania:
                        _IProvedor = new Provedor_Goiania();
                        break;

                    case EnumProvedor.Conam:
                        _IProvedor = new Provedor_Conam();
                        break;
                    case EnumProvedor.SMARAPD:
                        _IProvedor = new Provedor_SMARAPD();
                        break;

                    case EnumProvedor.IssOnline:
                        _IProvedor = new Provedor_IssOnline();
                        break;
                    case EnumProvedor.Natalense:
                        _IProvedor = new Provedor_Natalense();
                        break;

                    case EnumProvedor.Fiorilli:
                        _IProvedor = new Provedor_Fiorilli();
                        break;
                    case EnumProvedor.SuperNova:
                        _IProvedor = new Provedor_SuperNova();
                        break;
                    case EnumProvedor.Tinus:
                        _IProvedor = new Provedor_Tinus();
                        break;
                    case EnumProvedor.Simple:
                        _IProvedor = new Provedor_Simple();
                        break;
                    case EnumProvedor.GovDigital:
                        _IProvedor = new Provedor_GovDigital();
                        break;

                    case EnumProvedor.Ginfes:
                        _IProvedor = new Provedor_Ginfes();
                        break;

                    case EnumProvedor.EGoverne:
                        _IProvedor = new Provedor_EGoverne();
                        break;

                    case EnumProvedor.SISPMJP:
                        _IProvedor = new Provedor_SISPMJP();
                        break;

                    case EnumProvedor.SALVADOR_BA:
                        _IProvedor = new Provedor_SALVADOR_BA();
                        break;
                    /*
                    case EnumProvedor.BSITBR:
                        _IProvedor = new Provedor_BSITBR();
                        break;
                    case EnumProvedor.Elotech:
                        _IProvedor = new Provedor_Elotech();
                        break;
                    case EnumProvedor.PRODATA:
                        _IProvedor = new Provedor_Prodata();
                        break;
                    */
                    case EnumProvedor.CARIOCA:
                        _IProvedor = new Provedor_CARIOCA();
                        break;

                    case EnumProvedor.Tiplan:
                        _IProvedor = new Provedor_Tiplan();
                        break;

                    case EnumProvedor.Lexsom:
                        _IProvedor = new Provedor_Lexsom();
                        break;

                    case EnumProvedor.ABACO:
                        _IProvedor = new Provedor_ABACO();
                        break;

                    case EnumProvedor.Coplan:
                        _IProvedor = new Provedor_Coplan();
                        break;
                    case EnumProvedor.PRONIM:
                        _IProvedor = new Provedor_PRONIM();
                        break;

                    case EnumProvedor.VersaTecnologia:
                        _IProvedor = new Provedor_VersaTecnologia();
                        break;

                    case EnumProvedor.Betha:
                        _IProvedor = new Provedor_Betha();
                        break;

                    case EnumProvedor.Bauru_SP:
                        _IProvedor = new Provedor_Bauru_SP();
                        break;

                    case EnumProvedor.DBSeller:
                        _IProvedor = new Provedor_DBSeller();
                        break;

                    case EnumProvedor.IPM:
                        _IProvedor = new Provedor_IPM();
                        break;

                    case EnumProvedor.Simpliss:
                        _IProvedor = new Provedor_SimpliSS();
                        break;

                    case EnumProvedor.DSF_V3:
                        _IProvedor = new Provedor_DSF_V3();
                        break;

                    case EnumProvedor.CECAM:
                        _IProvedor = new Provedor_CECAM();
                        break;

                    case EnumProvedor.Tiplan_203:
                        _IProvedor = new Provedor_Tiplan_203();
                        break;

                    case EnumProvedor.MARINGA_PR:
                        _IProvedor = new Provedor_MARINGA_PR();
                        break;

                    default:
                        throw new Exception("Provedor não implementando: " + nomeProvedor.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a execução da transação.", ex);
            }
        }

        #endregion

        #region Métodos de Interface

        public override XmlDocument GeraXmlNota(NFSeNota nota)
        {
            return _IProvedor.GeraXmlNota(nota);
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe)
        {
            return _IProvedor.GerarXmlConsulta(nota, numeroNFSe);
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao)
        {
            return _IProvedor.GerarXmlConsulta(nota, numeroNFSe, emissao);
        }

        public override XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote)
        {
            return _IProvedor.GerarXmlConsulta(nota, numeroLote);
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo)
        {
            return _IProvedor.LerRetorno(nota, arquivo);
        }

        public override RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF)
        {
            return _IProvedor.LerRetorno(nota, arquivo, numNF);
        }

        public override XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash)
        {
            return _IProvedor.GerarXmlConsultaNotaValida(nota, numeroNFSe, hash);
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo)
        {
            return _IProvedor.GerarXmlCancelaNota(nota, numeroNFSe, motivo);
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe)
        {
            return _IProvedor.GerarXmlCancelaNota(nota, numeroNFSe);
        }

        public override XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao)
        {
            return _IProvedor.GerarXmlCancelaNota(nota, numeroNFSe, motivo, numeroLote, codigoVerificacao);
        }

        public override RetornoTransmitir ValidarNFSe(NFSeNota nota)
        {
            return _IProvedor.ValidarNFSe(nota);
        }
        #endregion
    }


}
