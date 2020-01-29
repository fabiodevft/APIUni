using Microsoft.AspNetCore.Mvc;
using NFe.API.Domain;
using NFe.API.Enum;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using static NFe.API.Domain.NFSeModel;
using static NFe.API.Domain.NFSeNota;
using static NFe.API.Domain.Notas;

namespace NFe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    public class NFSeController : ControllerBase
    {

        //public IActionResult Index()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult CancelarNfse([FromBody] ComandoTransmitir comando, string numeroNota, string justificativa, string codigoVerificacao, EnumProvedor provedor)
        //{
        //    //montar doc xml
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            var nota = new NFSeNota(provedor)
        //            {
        //                Documento = comando
        //            };

        //            XmlDocument xml = null;
        //            switch (provedor)
        //            {
        //                case EnumProvedor.DSF:
        //                case EnumProvedor.CECAM:
        //                    xml = nota.GerarXmlCancelaNota(numeroNota, justificativa, nota.Documento.TDFe.Tide.FNumeroLote, codigoVerificacao);
        //                    break;
        //                case EnumProvedor.Paulistana:
        //                case EnumProvedor.Metropolis:
        //                case EnumProvedor.Thema:
        //                case EnumProvedor.BHISS:
        //                case EnumProvedor.IssOnline:
        //                case EnumProvedor.Natalense:
        //                case EnumProvedor.CARIOCA:
        //                case EnumProvedor.PRONIM:
        //                    xml = nota.GerarXmlCancelaNota(numeroNota);
        //                    break;
        //                //case EnumProvedor.Goiania:
        //                //    return "Não é disponibilizado o cancelamento ou a substituição de NFS - e via web service.Substituição e Cancelamento são realizáveis através do site da NFS - e, nos termos lá descritos, ou via Processo Administrativo junto à Secretaria de Finanças.";
        //                default:
        //                    xml = nota.GerarXmlCancelaNota(numeroNota, justificativa);
        //                    break;
        //            }

        //            //chamarservico
        //            AplicacaoNFSe aplicacao = new AplicacaoNFSe(xml, "CancelarNfse", comando.TDFe.TPrestador.FCnpj);
        //            var ret = aplicacao.ExecutaMetodo();

        //            //retornar resposta
        //            var resultadoOperacao = nota.LerRetorno(ret, EnumOperacao.Cancela);

        //            return Ok(resultadoOperacao);
        //        }

        //        return BadRequest();               
                
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest();
        //    }            
        //}

        //[HttpPost]
        //public void ConsultarLoteRps()
        //{

        //}

        //[HttpPost]
        //public void ConsultarNfse()
        //{

        //}
        //[HttpPost]
        //public void ConsultarNfsePorRps()
        //{

        //}
        //[HttpPost]
        //public void ConsultarNfseRecebidas()
        //{

        //}
        //[HttpPost]
        //public void ConsultarNfseTomados()
        //{

        //}
        //[HttpPost]
        //public void ConsultarStatusNFse()
        //{

        //}

        //[HttpPost]
        //public void ConsultaSituacaoLoteRps()
        //{

        //}
        [HttpPost("RecepcionarLoteRps")]
        public IActionResult RecepcionarLoteRps(ComandoTransmitir comando)
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

                    return Ok(resultadoOperacao);

                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost("teste")]
        public IActionResult Teste(ComandoTransmitir comando)
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
                        
            return Ok();
        }


        //public class RetornoTransmitir : RetornoBase
        //{
        //    public RetornoTransmitir();
        //    public RetornoTransmitir(string _error, string _success);

        //    public long CodigoEvento { get; set; }
        //    public string CodigoRetornoPref { get; set; }
        //    public DateTime? DataEmissaoRPS { get; set; }
        //    public string NumeroRPS { get; set; }
        //    public long NumeroLote { get; set; }
        //    public string LinkImpressao { get; set; }
        //    public string xmlEvento { get; set; }
        //    public string xml { get; set; }
        //    public string numero { get; set; }
        //    public string xMotivo { get; set; }
        //    public string cStat { get; set; }
        //    public string digVal { get; set; }
        //    public string nProt { get; set; }
        //    public string dhRecbto { get; set; }
        //    public string chave { get; set; }
        //    public string hash { get; set; }
        //    public string Danfe { get; set; }
        //}

    }
}