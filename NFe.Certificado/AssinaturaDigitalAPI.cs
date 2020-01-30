using NFe.Components;
using NFe.Exceptions;
using NFe.Settings;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace NFe.Certificado
{
    public class AssinaturaDigitalAPI
    {
        public bool AssinaturaValida { get; private set; }
        public bool TesteCertificado { get; set; }

        public XmlDocument arquivoXML { get; set; }


        /// <summary>
        /// O método assina digitalmente o arquivo XML passado por parâmetro e
        /// grava o XML assinado com o mesmo nome, sobreponto o XML informado por parâmetro.
        /// Disponibiliza também uma propriedade com uma string do xml assinado (this.vXmlStringAssinado)
        /// </summary>
        /// <param name="conteudoXML">Conteúdo do XML</param>
        /// <param name="tagAssinatura">Nome da tag onde é para ficar a assinatura</param>
        /// <param name="tagAtributoId">Nome da tag que tem o atributo ID, tag que vai ser assinada</param>
        /// <param name="x509Cert">Certificado a ser utilizado na assinatura</param>
        /// <param name="empresa">Índice da empresa que está solicitando a assinatura</param>
        /// <param name="algorithmType">Tipo de algoritimo para assinatura do XML.</param>
        /// <remarks>
        /// Autor: Wandrey Mundin Ferreira
        /// Data: 04/06/2008
        /// </remarks>
        private void AssinarAPI(XmlDocument conteudoXML, string tagAssinatura, string tagAtributoId, X509Certificate2 x509Cert, Empresa empresa,
            AlgorithmType algorithmType,
            bool comURI)
        {
            try
            {
                if (x509Cert == null)
                    throw new ExceptionCertificadoDigital(ErroPadrao.CertificadoNaoEncontrado);

                if (conteudoXML.GetElementsByTagName(tagAssinatura).Count == 0)
                {
                    throw new Exception("A tag de assinatura " + tagAssinatura.Trim() + " não existe no XML. (Código do Erro: 5)");
                }
                else if (conteudoXML.GetElementsByTagName(tagAtributoId).Count == 0)
                {
                    throw new Exception("A tag de assinatura " + tagAtributoId.Trim() + " não existe no XML. (Código do Erro: 4)");
                }
                // Existe mais de uma tag a ser assinada
                else
                {
                    XmlNodeList lists = conteudoXML.GetElementsByTagName(tagAssinatura);
                    XmlNode listRPS = null;

                    /// Esta condição foi feita especificamente para prefeitura de Governador Valadares pois o AtribudoID e o Elemento assinado devem possuir o mesmo nome.
                    /// Talvez tenha que ser reavaliado.

                    #region Governador Valadares

                    if (tagAssinatura.Equals(tagAtributoId) && empresa.UnidadeFederativaCodigo == 3127701)
                    {
                        foreach (XmlNode item in lists)
                        {
                            if (listRPS == null)
                            {
                                listRPS = item;
                            }

                            if (item.Name.Equals(tagAssinatura))
                            {
                                lists = item.ChildNodes;
                                break;
                            }
                        }
                    }

                    #endregion Governador Valadares

                    foreach (XmlNode nodes in lists)
                    {
                        foreach (XmlNode childNodes in nodes.ChildNodes)
                        {
                            if (!childNodes.Name.Equals(tagAtributoId))
                                continue;

                            // Create a reference to be signed
                            Reference reference = new Reference();
                            reference.Uri = "";

                            // pega o uri que deve ser assinada
                            XmlElement childElemen = (XmlElement)childNodes;

                            if (comURI)
                            {
                                if (childElemen.GetAttributeNode("Id") != null)
                                {
                                    reference.Uri = "#" + childElemen.GetAttributeNode("Id").Value;
                                }
                                else if (childElemen.GetAttributeNode("id") != null)
                                {
                                    reference.Uri = "#" + childElemen.GetAttributeNode("id").Value;
                                }
                            }

                            // Create a SignedXml object.
                            SignedXml signedXml = new SignedXml(conteudoXML);

#if _fw46
                            //A3
                            if (!String.IsNullOrEmpty(empresa.CertificadoPIN) &&
                                clsX509Certificate2Extension.IsA3(x509Cert) &&
                                !empresa.CertificadoPINCarregado)
                            {
                                x509Cert.SetPinPrivateKey(empresa.CertificadoPIN);
                                empresa.CertificadoPINCarregado = true;
                            }

                            if (algorithmType.Equals(AlgorithmType.Sha256))
                            {
                                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                                signedXml.SigningKey = x509Cert.GetRSAPrivateKey();
                            }
#endif

                            if (algorithmType.Equals(AlgorithmType.Sha1))
                            {
                                signedXml.SigningKey = x509Cert.PrivateKey;
                                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                                reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
                            }

                            // Add an enveloped transformation to the reference.
                            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                            reference.AddTransform(new XmlDsigC14NTransform());

                            // Add the reference to the SignedXml object.
                            signedXml.AddReference(reference);

#if _fw46
                            if (algorithmType.Equals(AlgorithmType.Sha256))
                            {
                                reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                            }
#endif

                            // Create a new KeyInfo object
                            KeyInfo keyInfo = new KeyInfo();

                            // Load the certificate into a KeyInfoX509Data object
                            // and add it to the KeyInfo object.
                            keyInfo.AddClause(new KeyInfoX509Data(x509Cert));

                            // Add the KeyInfo object to the SignedXml object.
                            signedXml.KeyInfo = keyInfo;
                            signedXml.ComputeSignature();

                            // Get the XML representation of the signature and save
                            // it to an XmlElement object.
                            XmlElement xmlDigitalSignature = signedXml.GetXml();

                            if (tagAssinatura.Equals(tagAtributoId) && empresa.UnidadeFederativaCodigo == 3127701)
                            {
                                ///Desenvolvido especificamente para prefeitura de governador valadares
                                listRPS.AppendChild(conteudoXML.ImportNode(xmlDigitalSignature, true));
                            }
                            else
                            {
                                // Gravar o elemento no documento XML
                                nodes.AppendChild(conteudoXML.ImportNode(xmlDigitalSignature, true));
                            }
                        }
                    }
                }

                arquivoXML = conteudoXML;
            }
            catch (CryptographicException ex)
            {
                #region #10316

                /*
                 * Solução para o problema do certificado do tipo A3
                 * Marcelo
                 * 29/07/2013
                 */

                AssinaturaValida = false;

                if (clsX509Certificate2Extension.IsA3(x509Cert))
                {
                    x509Cert = Empresas.ResetCertificado(empresa);
                    if (!TesteCertificado)
                        throw new Exception("O certificado deverá ser reiniciado.\r\n Retire o certificado.\r\nAguarde o LED terminar de piscar.\r\n Recoloque o certificado e informe o PIN novamente.\r\n" + ex.ToString());// #12342 concatenar com a mensagem original
                }
                else
                {
                    throw;
                }

                #endregion #10316
            }
            catch
            {
                throw;
            }
        }

        public RSACryptoServiceProvider LerDispositivo(string PIN, int providerType, string provider)
        {
            CspParameters csp = new CspParameters(providerType, provider);

            SecureString ss = new SecureString();
            char[] PINs = PIN.ToCharArray();
            foreach (char a in PINs)
            {
                ss.AppendChar(a);
            }
            csp.KeyPassword = ss;
            csp.KeyNumber = 1;
            csp.Flags = CspProviderFlags.NoPrompt;
            csp.KeyContainerName = "";

            // Initialize an RSACryptoServiceProvider object using
            // the CspParameters object.
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(csp);
            //rsa.PersistKeyInCsp = false; //importante, senão ele lota o certificado de chaves!
            rsa.PersistKeyInCsp = false;
            rsa.ToXmlString(false);
            return rsa;
        }

        #region Assinar()

        public void AssinarAPI(XmlDocument conteudoXML, Empresa emp, int UFCod, AlgorithmType algorithmType = AlgorithmType.Sha1, bool comURI = true)
        {
            if (emp.UsaCertificado)
            {
                TipoArquivoXML v = new TipoArquivoXML(conteudoXML, UFCod, false);

                if (!string.IsNullOrEmpty(v.TagAssinatura0))
                {
                    if (!Assinado(conteudoXML, v.TagAssinatura0))
                        AssinarAPI(conteudoXML, v.TagAssinatura0, v.TagAtributoId0, emp.X509Certificado, emp, algorithmType, comURI);
                }

                if (!string.IsNullOrEmpty(v.TagAssinatura))
                {
                    if (!Assinado(conteudoXML, v.TagAssinatura))
                        AssinarAPI(conteudoXML, v.TagAssinatura, v.TagAtributoId, emp.X509Certificado, emp, algorithmType, comURI);
                }

                //Assinar o lote
                if (!string.IsNullOrEmpty(v.TagLoteAssinatura))
                    if (!Assinado(conteudoXML, v.TagLoteAssinatura))
                        AssinarAPI(conteudoXML, v.TagLoteAssinatura, v.TagLoteAtributoId, emp.X509Certificado, emp, algorithmType, comURI);
            }
        }

        #endregion Assinar()

        #region Assinado()

        /// <summary>
        /// Verificar se o XML já tem assinatura
        /// </summary>
        /// <param name="conteudoXML">Conteúdo do XML</param>
        /// <param name="tagAssinatura">Tag de assinatura onde vamos pesquisar</param>
        /// <returns>true = Já está assinado</returns>
        private bool Assinado(XmlDocument conteudoXML, string tagAssinatura)
        {
            bool retorno = false;

            try
            {
                if (conteudoXML.GetElementsByTagName(tagAssinatura)[0].LastChild.Name == "Signature")
                    retorno = true;
            }
            catch { }

            return retorno;
        }

        #endregion Assinado()

    }
}
