using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using NFe.Service;
using NFe.Settings;

namespace NFe.API
{
    public class AplicacaoNFSe
    {
        private XmlDocument documento { get; }
        private readonly string metodo;
        private readonly string cnpj;
        private readonly string userWS;
        private readonly string senhaWS;

        #region CONSTRUTOR
        public AplicacaoNFSe(XmlDocument documento, string metodo, string cnpj)
        {
            this.documento = documento;
            this.metodo = metodo;
            this.cnpj = cnpj;
        }

        public AplicacaoNFSe(XmlDocument documento, string metodo, string cnpj, string userWS, string senhaWS)
        {
            this.documento = documento;
            this.metodo = metodo;
            this.cnpj = cnpj;
            this.userWS = userWS;
            this.senhaWS = senhaWS;
        }
        #endregion


        public XmlDocument ExecutaMetodo()
        {
            // get cnpj do xml
            XmlDocument resposta = null;
            int emp = Empresas.FindConfEmpresaIndex(cnpj, Components.TipoAplicativo.Nfse); // 2 = nfse

            switch (metodo.ToUpper())
            {
                case "CANCELARNFSE":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeCancelar(), "Execute");                                        
                    break;
                case "CONSULTARLOTERPS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeConsultarLoteRps (), "Execute");
                    break;
                case "CONSULTARNFSE":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeConsultar(), "Execute");
                    break;
                                    
                case "CONSULTARNFSEPORRPS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeConsultarPorRps(), "Execute");
                    break;

                case "CONSULTARNFSERECEBIDAS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarNfseRecebidas(), "Execute");
                    break;

                case "CONSULTARNFSETOMADOS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarNfseTomados(), "Execute");
                    break;

                case "CONSULTARSTATUSNFSE":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskConsultarStatusNFse(), "Execute");
                    break;

                case "CONSULTASITUACAOLOTERPS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeConsultaSituacaoLoteRps(), "Execute");
                    break;

                case "RECEPCIONARLOTERPS":
                    Processar.ExecutaTarefaAPI(documento, new Service.NFSe.TaskNFSeRecepcionarLoteRps(emp, documento), "Execute");
                    break;


                default:
                    break;
            }

            return resposta;
        }



    }
}
