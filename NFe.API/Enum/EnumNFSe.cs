using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NFe.API.Enum
{
    public class EnumNFSe
    {
    }

    public enum EnumAmbiente
    {
        Producao = 1,
        Homologacao = 2
    }

    public enum EnumNFSeRPSStatus
    {
        srNormal = 1,
        srCancelado = 2
    }

    public enum EnumOperacao
    {
        Envio = 1,
        Consulta = 2,
        ConsultaLote = 3,
        Cancela = 4,
        ConsultaRPS = 5,
        ConsultaStatus = 6
    }

    public enum EnumNFSeSituacaoTributaria
    {
        stRetencao = 1,
        stNormal = 2,
        stSubstituicao = 3
    }

}
