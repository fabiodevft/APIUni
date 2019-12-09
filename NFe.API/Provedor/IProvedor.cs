﻿using NFe.API.Domain;
using NFe.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using static NFe.API.Domain.Notas;

namespace NFe.API.Provedor
{
    public interface IProvedor
    {
        EnumProvedor Nome { get; set; }
        XmlDocument GeraXmlNota(NFSeNota nota);
        XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe);
        XmlDocument GerarXmlConsulta(NFSeNota nota, string numeroNFSe, DateTime emissao);
        XmlDocument GerarXmlConsulta(NFSeNota nota, long numeroLote);
        RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo);
        RetornoTransmitir LerRetorno(NFSeNota nota, string arquivo, string numNF);
        XmlDocument GerarXmlConsultaNotaValida(NFSeNota nota, string numeroNFSe, string hash);
        XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe);
        XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo);
        XmlDocument GerarXmlCancelaNota(NFSeNota nota, string numeroNFSe, string motivo, long numeroLote, string codigoVerificacao);
        RetornoTransmitir ValidarNFSe(NFSeNota nota);
    }
}
