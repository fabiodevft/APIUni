using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using static UniAPI.Domain.NFSeNota;

namespace UniAPI
{
    public class AplicacaoNFSe
    {
        private XmlDocument _documento { get; }
        private readonly string _metodo;
        private readonly ComandoTransmitir _comandoTransmitir;
        
        private X509Certificate2 _certificado;
        private Empresa empresa;

        #region CONSTRUTOR       

        public AplicacaoNFSe(XmlDocument documento, string metodo, ComandoTransmitir comandoTransmitir)
        {
            _documento = documento;
            _metodo = metodo;
            _comandoTransmitir = comandoTransmitir;
        }

        #endregion

        #region MÉTODOS

        public void CarregarDados()
        {
            empresa = new Empresa();
            empresa.CNPJ = _comandoTransmitir.TDFe.TPrestador.FCnpj;
            empresa.Nome = _comandoTransmitir.TDFe.TPrestador.FNomeFantasia;
            empresa.UnidadeFederativaCodigo = Convert.ToInt32(_comandoTransmitir.TDFe.TPrestador.TEndereco.FCodigoMunicipio);

            if (_comandoTransmitir.TDFe.TCertificado.Arquivo != null)
            {
                empresa.UsaCertificado = true;
                empresa.CertificadoSenha = _comandoTransmitir.TDFe.TCertificado.SenhaCert;
                _certificado = new X509Certificate2(_comandoTransmitir.TDFe.TCertificado.Arquivo, _comandoTransmitir.TDFe.TCertificado.SenhaCert);

                empresa.X509Certificado = _certificado;
            }
            else
            {
                empresa.UsaCertificado = false;
            }

            empresa.Servico = TipoAplicativo.Nfse;
            empresa.UsuarioWS = _comandoTransmitir.TDFe.TPrestador.FIdentificacaoPrestador._FUsuario;
            empresa.SenhaWS = _comandoTransmitir.TDFe.TPrestador.FIdentificacaoPrestador.FSenha;            
                                   
        }        

        public XmlDocument ExecutaMetodo()
        {
            // get cnpj do xml
            XmlDocument resposta = null;
            //int emp = Empresas.FindConfEmpresaIndex(cnpj, Components.TipoAplicativo.Nfse); // 2 = nfse

            switch (_metodo.ToUpper())
            {
                //case "CANCELARNFSE":
                //    Processar.ExecutaTarefaAPI(_documento, new Service.NFSe.TaskNFSeCancelar(), "Execute");
                //    break;
                //case "CONSULTARLOTERPS":
                //    Processar.ExecutaTarefaAPI(_documento, new Service.NFSe.TaskNFSeConsultarLoteRps(), "Execute");
                //    break;
                //case "CONSULTARNFSE":
                //    Processar.ExecutaTarefaAPI(_documento, new Service.NFSe.TaskNFSeConsultar(), "Execute");
                //    break;

                //case "CONSULTARNFSEPORRPS":
                //    Processar.ExecutaTarefaAPI(_documento, new Service.NFSe.TaskNFSeConsultarPorRps(), "Execute");
                //    break;

                //case "CONSULTARNFSERECEBIDAS":
                //    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarNfseRecebidas(), "Execute");
                //    break;

                //case "CONSULTARNFSETOMADOS":
                //    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarNfseTomados(), "Execute");
                //    break;

                //case "CONSULTARSTATUSNFSE":
                //    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarStatusNFse(), "Execute");
                //    break;

                //case "CONSULTASITUACAOLOTERPS":
                //    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeConsultaSituacaoLoteRps(), "Execute");
                //    break;

                case "RECEPCIONARLOTERPS":
                    var servico = new Service.NFSe.TaskNFSeRecepcionarLoteRps(_documento, _certificado, empresa);
                    resposta = servico.ExecuteAPI();
                    break;


                default:
                    break;
            }

            return resposta;
        }


        #endregion

    }
}
