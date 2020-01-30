using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using UniAPI.Domain;
using UniAPI.Enum;
using static UniAPI.Domain.NFSeNota;
using static UniAPI.Domain.Notas;

namespace UniAPI.Controllers
{
    public class NFSeController : ApiController
    {

        public RetornoTransmitir PostRecepcionarLoteRps(ComandoTransmitir comando)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var provedor = EnumProvedor.Fiorilli;

                    var nota = new NFSeNota(provedor)
                    {
                        Documento = comando
                    };

                    XmlDocument xml = null;
                    switch (provedor)
                    {
                        case EnumProvedor.Fiorilli:
                            xml = nota.GeraXmlNota();
                            break;
                        //case EnumProvedor.Goiania:
                        //    return "Não é disponibilizado o cancelamento ou a substituição de NFS - e via web service.Substituição e Cancelamento são realizáveis através do site da NFS - e, nos termos lá descritos, ou via Processo Administrativo junto à Secretaria de Finanças.";
                        default:
                            //xml = nota.GerarXmlCancelaNota(numeroNota, justificativa);
                            break;
                    }

                    //chamarservico                    
                    AplicacaoNFSe aplicacao = new AplicacaoNFSe(xml, "RecepcionarLoteRPS", comando);
                    aplicacao.CarregarDados();
                    var ret = aplicacao.ExecutaMetodo();

                    //retornar resposta
                    var resultadoOperacao = nota.LerRetorno(ret, EnumOperacao.Cancela);

                    return resultadoOperacao; //  Request.CreateResponse<RetornoTransmitir>(HttpStatusCode.OK, resultadoOperacao);

                }

                //return Request.CreateResponse(HttpStatusCode.BadRequest);
                return null;
            }
            catch (Exception e)
            {
                //return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message); 
                return null;
            }
        }
                       
        public HttpResponseMessage Teste()
        {

            RetornoTransmitir ret = new RetornoTransmitir("", "Sucesso")
            {
                chave = "123456789",
                cStat = "101",
                xMotivo = "",
                numero = "123123",
                nProt = "1259878633",
                xml = "",
                digVal = "3265",
                NumeroLote = 1,
                NumeroRPS = "1",
                DataEmissaoRPS = DateTime.Now,
                dhRecbto = "",
                CodigoRetornoPref = ""
            };

            return Request.CreateResponse(HttpStatusCode.OK); ;
        }

    }
}
