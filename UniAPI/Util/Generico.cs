using System;
using System.Text.RegularExpressions;
using static UniAPI.Util.Extensions;

namespace UniAPI.Util
{
    public static class Generico
    {
        public static string RetornarApenasNumeros(string Texto)
        {
            string retorno = "";
            retorno = String.Join("", System.Text.RegularExpressions.Regex.Split(Texto, @"[^\d]"));
            return retorno;
        }

        public static string RetornarNumeroZerosEsquerda(string Texto, int QtdLength, int QtdDecimais = 0)
        {
            var reg = new TRegistroEDI();
            reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliDireita______, 1, QtdLength, QtdDecimais, Texto, '0'));
            reg.CodificarLinha();

            return reg.LinhaRegistro;
        }

        public static string TratarString(string texto)
        {
            return RemoverCaracterEspecial(TrocarCaractercomAcentos(texto));
        }

        public static string RemoverCaracterEspecial(string texto)
        {
            return Regex.Replace(texto, "[^0-9a-zA-Z_+-.,!@#$%&*();\\/|<>:?= ]+", "");
        }

        public static string TrocarCaractercomAcentos(string texto)
        {
            if (texto != null && texto != "")
            {
                string comAcentos = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
                string semAcentos = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";

                for (int i = 0; i < comAcentos.Length; i++)
                {
                    texto = texto.Replace(comAcentos[i].ToString(), semAcentos[i].ToString());
                }
                return texto;
            }
            else
            {
                return "";
            }
        }

        public static string RetornaApenasLetrasNumeros(string texto)
        {
            return Regex.Replace(texto, "[^a-zA-Z0-9]+", "");
        }

        public static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;
            string digito;
            string tempCnpj;
            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14 && cnpj.Equals("00000000000000"))
                return false;
            tempCnpj = cnpj.Substring(0, 12);
            soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cnpj.EndsWith(digito);
        }

        public static bool IsCpf(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 1 && cpf.Equals("00000000000"))
                return false;
            tempCpf = cpf.Substring(0, 9);
            soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cpf.EndsWith(digito);
        }

        public static bool ValidarEmail(String email)
        {
            bool emailValido = false;

            string emailRegex = string.Format("{0}{1}",
                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))",
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$");

            try
            {
                emailValido = Regex.IsMatch(
                    email,
                    emailRegex);
            }
            catch (Exception ex)
            {
                emailValido = false;
            }

            return emailValido;
        }

        public static string ReplaceGenerico(string valor)
        {
            valor = valor.Replace("ns2:", "");
            valor = valor.Replace("ns3:", "");
            valor = valor.Replace("nfse:", "");
            valor = valor.Replace("ii:", "");
            return valor;
        }
    }
}
