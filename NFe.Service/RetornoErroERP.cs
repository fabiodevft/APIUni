using NFe.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NFe.Service
{
    public static class RetornoErroERP
    {

        public static void GeraArquivoErroERP(string NomeArquivoXML, string strErro, string strMesagemErro, string nomeArqErro)
        {
            string strNumeroLote = string.Empty;
            string strNomeArquivo = string.Empty;

            var numLote = NomeArquivoXML.Substring(NomeArquivoXML.IndexOf("Temp") + "Temp\\".Length,
                                                        NomeArquivoXML.IndexOf("-") -
                                                        (NomeArquivoXML.LastIndexOf("Temp") + "Temp\\".Length));

            //string pastaRetorno = Empresas.Configuracoes[emp].PastaXmlRetorno;
            var nomeArquivoAux = NomeArquivoXML.Substring(0, NomeArquivoXML.IndexOf("Envio")) + "Retorno\\" + numLote + nomeArqErro;
            
            using (FileStream fileStream = File.Create(nomeArquivoAux))
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.WriteLine("[ERP] - Ocorreu uma inconsistência na transmissão da nota.");
                writer.WriteLine($"Mensagem: {strErro} - {strMesagemErro}");
            }
            
        }


        public static XmlDocument GeraArquivoErroERP(string strErro, string metodo)
        {
            XmlDocument document = new XmlDocument();

            XmlNode docNode = document.CreateXmlDeclaration("1.0", "utf-8", null);
            document.AppendChild(docNode);

            var gerarNotaNode = document.CreateElement(@""+ metodo + "Resposta");            
            document.AppendChild(gerarNotaNode);

            XmlNode nodeItemErro = document.CreateElement("Erro");
            nodeItemErro.AppendChild(document.CreateTextNode(strErro));
            gerarNotaNode.AppendChild(nodeItemErro);

            //XmlNode nodeItemMensagem = document.CreateElement("Mensagem");
            //nodeItemMensagem.AppendChild(document.CreateTextNode(strMesagemErro));
            //gerarNotaNode.AppendChild(nodeItemMensagem);

            return document;

        }


    }
}
