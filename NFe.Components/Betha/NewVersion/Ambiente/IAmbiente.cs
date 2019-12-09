using NFe.Components.Abstract;
using System.Xml;

namespace NFe.Components.Betha.NewVersion.Ambiente
{
    public interface IAmbiente : IEmiteNFSe
    {
        string NameSpaces { get; }

        void CancelarNfse(string file);
        void ConsultarLoteRps(string file);
        void ConsultarNfse(string file);
        void ConsultarNfsePorRps(string file);
        void ConsultarSituacaoLoteRps(string file);
        void EmiteNF(string file);
        void EmiteNFSincrono(string file);

        #region API
        XmlDocument EmiteNF(XmlDocument xml);
        XmlDocument EmiteNFSincrono(XmlDocument xml);
        XmlDocument CancelarNfse(XmlDocument xml);
        XmlDocument ConsultarLoteRps(XmlDocument xml);
        XmlDocument ConsultarNfse(XmlDocument xml);
        XmlDocument ConsultarNfsePorRps(XmlDocument xml);
        XmlDocument ConsultarSituacaoLoteRps(XmlDocument xml);

        #endregion


    }
}