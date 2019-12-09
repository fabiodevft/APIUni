using System;
using System.Security.Cryptography.X509Certificates;

namespace NFe.Components.SALVADOR_BA
{
    public class SALVADOR_BA : SALVADOR_BABase
    {
        public override string NameSpaces
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region Construtures
        public SALVADOR_BA(TipoAmbiente tpAmb, string pastaRetorno, int codMun, string proxyserver, string proxyuser, string proxypass, string proxySenha, string proxyServidor, X509Certificate2 certificado)
            : base(tpAmb, pastaRetorno, codMun, proxyserver, proxyuser, proxypass, certificado)
        { }
        #endregion

    }
}
