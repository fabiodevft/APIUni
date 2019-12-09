using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace NFe.Components.SigCorp
{
    public class SigCorp : SigCorpBase
    {
        public override string NameSpaces
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region Construtures
        public SigCorp(TipoAmbiente tpAmb, string pastaRetorno, int codMun)
            : base(tpAmb, pastaRetorno, codMun)
        { }

        public SigCorp(TipoAmbiente tpAmb, int codMun)
            : base(tpAmb, codMun)
        { }
        
        #endregion
    }
}
