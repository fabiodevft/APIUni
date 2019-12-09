using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using NFe.API.Domain;
using NFe.API.Enum;
using static NFe.API.Domain.NFSeNota;
using static NFe.API.Domain.Notas;

namespace NFe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NFSeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CancelarNfse([FromBody] ComandoTransmitir comando, string numeroNota, string justificativa, string codigoVerificacao, EnumProvedor provedor)
        {
            //montar doc xml
            try
            {
                if (ModelState.IsValid)
                {
                    var nota = new NFSeNota(provedor)
                    {
                        Documento = comando
                    };

                    XmlDocument xml = null;
                    switch (provedor)
                    {
                        case EnumProvedor.DSF:
                        case EnumProvedor.CECAM:
                            xml = nota.GerarXmlCancelaNota(numeroNota, justificativa, nota.Documento.TDFe.Tide.FNumeroLote, codigoVerificacao);
                            break;
                        case EnumProvedor.Paulistana:
                        case EnumProvedor.Metropolis:
                        case EnumProvedor.Thema:
                        case EnumProvedor.BHISS:
                        case EnumProvedor.IssOnline:
                        case EnumProvedor.Natalense:
                        case EnumProvedor.CARIOCA:
                        case EnumProvedor.PRONIM:
                            xml = nota.GerarXmlCancelaNota(numeroNota);
                            break;
                        //case EnumProvedor.Goiania:
                        //    return "Não é disponibilizado o cancelamento ou a substituição de NFS - e via web service.Substituição e Cancelamento são realizáveis através do site da NFS - e, nos termos lá descritos, ou via Processo Administrativo junto à Secretaria de Finanças.";
                        default:
                            xml = nota.GerarXmlCancelaNota(numeroNota, justificativa);
                            break;
                    }

                    //chamarservico
                    AplicacaoNFSe aplicacao = new AplicacaoNFSe(xml, "CancelarNfse", comando.TDFe.TPrestador.FCnpj);
                    var ret = aplicacao.ExecutaMetodo();

                    //retornar resposta
                    var resultadoOperacao = nota.LerRetorno(ret, EnumOperacao.Cancela);

                    return Ok(resultadoOperacao);
                }

                return BadRequest();               
                
            }
            catch (Exception)
            {
                return BadRequest();
            }            
        }

        [HttpPost]
        public void ConsultarLoteRps()
        {

        }

        [HttpPost]
        public void ConsultarNfse()
        {

        }
        [HttpPost]
        public void ConsultarNfsePorRps()
        {

        }
        [HttpPost]
        public void ConsultarNfseRecebidas()
        {

        }
        [HttpPost]
        public void ConsultarNfseTomados()
        {

        }
        [HttpPost]
        public void ConsultarStatusNFse()
        {

        }

        [HttpPost]
        public void ConsultaSituacaoLoteRps()
        {

        }
        [HttpPost]
        public void RecepcionarLoteRps()
        {

        }

    }
}