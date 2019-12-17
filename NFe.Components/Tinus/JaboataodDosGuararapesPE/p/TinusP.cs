using System;
using NFe.Components.Abstract;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_CancelarNfse;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_RecepcionarLoteRps;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_ConsultarNfse;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_ConsultarLoteRps;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_ConsultarNfsePorRps;
using NFe.Components.PJaboataoDosGuararapesPE_TINUS_ConsultarSituacaoLoteRps;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace NFe.Components.Tinus.JaboataodDosGuararapesPE.p
{
    public class TinusP : EmiteNFSeBase
    {
        private System.Web.Services.Protocols.SoapHttpClientProtocol Service;
        private X509Certificate2 Certificado;

        public override string NameSpaces
        {
            get
            {
                return "http://www.tinus.com.br";
            }
        }

        #region construtores
        public TinusP(TipoAmbiente tpAmb, string pastaRetorno, string proxyuser, string proxypass, string proxyserver, X509Certificate2 certificado)
            : base(tpAmb, pastaRetorno)
        {
            ServicePointManager.Expect100Continue = false;
            Certificado = certificado;
        }

        #endregion construtores

        #region Métodos

        public override void EmiteNF(string file)
        {
            RecepcionarLoteRps Service = new RecepcionarLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<RecepcionarLoteRps>(Service);

            EnviarLoteRpsEnvio envio = DeserializarObjeto<EnviarLoteRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.CallRecepcionarLoteRps(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.EnvLoteRps).RetornoXML);
        }

        public override void CancelarNfse(string file)
        {
            CancelarNfse Service = new CancelarNfse();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<CancelarNfse>(Service);

            CancelarNfseEnvio envio = DeserializarObjeto<CancelarNfseEnvio>(file);
            string strResult = SerializarObjeto(Service.CallCancelarNfse(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedCanNFSe).RetornoXML);
        }

        public override void ConsultarLoteRps(string file)
        {
            ConsultarLoteRps Service = new ConsultarLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarLoteRps>(Service);

            ConsultarLoteRpsEnvio envio = DeserializarObjeto<ConsultarLoteRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.CallConsultarLoteRps(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedLoteRps).RetornoXML);
        }

        public override void ConsultarSituacaoLoteRps(string file)
        {
            ConsultarSituacaoLoteRps Service = new ConsultarSituacaoLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarSituacaoLoteRps>(Service);

            ConsultarSituacaoLoteRpsEnvio envio = DeserializarObjeto<ConsultarSituacaoLoteRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.CallConsultarSituacaoLoteRps(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitLoteRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitLoteRps).RetornoXML);
        }

        public override void ConsultarNfse(string file)
        {
            ConsultarNfse Service = new ConsultarNfse();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarNfse>(Service);

            ConsultarNfseEnvio envio = DeserializarObjeto<ConsultarNfseEnvio>(file);
            string strResult = SerializarObjeto(Service.CallConsultarNfse(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSe).RetornoXML);
        }

        public override void ConsultarNfsePorRps(string file)
        {
            ConsultarNfsePorRps Service = new ConsultarNfsePorRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarNfsePorRps>(Service);

            ConsultarNfseRpsEnvio envio = DeserializarObjeto<ConsultarNfseRpsEnvio>(file);
            string strResult = SerializarObjeto(Service.CallConsultarNfsePorRps(envio));

            GerarRetorno(file,
                strResult,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).EnvioXML,
                Propriedade.Extensao(Propriedade.TipoEnvio.PedSitNFSeRps).RetornoXML);
        }


        #region API

        public override XmlDocument EmiteNF(XmlDocument xml)
        {
            RecepcionarLoteRps Service = new RecepcionarLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<RecepcionarLoteRps>(Service);

            EnviarLoteRpsEnvio envio = DeserializarObjeto<EnviarLoteRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallRecepcionarLoteRps(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        public override XmlDocument CancelarNfse(XmlDocument xml)
        {
            CancelarNfse Service = new CancelarNfse();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<CancelarNfse>(Service);

            CancelarNfseEnvio envio = DeserializarObjeto<CancelarNfseEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallCancelarNfse(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        public override XmlDocument ConsultarLoteRps(XmlDocument xml)
        {
            ConsultarLoteRps Service = new ConsultarLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarLoteRps>(Service);

            ConsultarLoteRpsEnvio envio = DeserializarObjeto<ConsultarLoteRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallConsultarLoteRps(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        public override XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml)
        {
            ConsultarSituacaoLoteRps Service = new ConsultarSituacaoLoteRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarSituacaoLoteRps>(Service);

            ConsultarSituacaoLoteRpsEnvio envio = DeserializarObjeto<ConsultarSituacaoLoteRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallConsultarSituacaoLoteRps(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        public override XmlDocument ConsultarNfse(XmlDocument xml)
        {
            ConsultarNfse Service = new ConsultarNfse();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarNfse>(Service);

            ConsultarNfseEnvio envio = DeserializarObjeto<ConsultarNfseEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallConsultarNfse(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        public override XmlDocument ConsultarNfsePorRps(XmlDocument xml)
        {
            ConsultarNfsePorRps Service = new ConsultarNfsePorRps();
            Service.ClientCertificates.Add(Certificado);
            DefinirProxy<ConsultarNfsePorRps>(Service);

            ConsultarNfseRpsEnvio envio = DeserializarObjeto<ConsultarNfseRpsEnvio>(xml);
            string strResult = SerializarObjeto(Service.CallConsultarNfsePorRps(envio));

            XmlDocument doc = new XmlDocument();
            doc.Load(strResult);

            return doc;
        }

        #endregion

        #endregion Métodos
    }
}