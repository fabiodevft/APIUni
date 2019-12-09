﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace NFe.Components.br.com.rlz.ceiss.santafedosul.p {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="Webservice PrefeituraBinding", Namespace="urn:server.issqn")]
    [System.Xml.Serialization.SoapIncludeAttribute(typeof(Servico))]
    public partial class WebservicePrefeitura : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback gravaNotaOperationCompleted;
        
        private System.Threading.SendOrPostCallback gravaNotaXMLOperationCompleted;
        
        private System.Threading.SendOrPostCallback listarNotasOperationCompleted;
        
        private System.Threading.SendOrPostCallback listarNotasXMLOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public WebservicePrefeitura() {
            this.Url = global::NFe.Components.Properties.Settings.Default.NFe_Components_br_com_rlz_ceiss_santafedosul_p_Webservice_Prefeitura;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event gravaNotaCompletedEventHandler gravaNotaCompleted;
        
        /// <remarks/>
        public event gravaNotaXMLCompletedEventHandler gravaNotaXMLCompleted;
        
        /// <remarks/>
        public event listarNotasCompletedEventHandler listarNotasCompleted;
        
        /// <remarks/>
        public event listarNotasXMLCompletedEventHandler listarNotasXMLCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:server.issqn#gravaNota", RequestNamespace="urn:server.issqn", ResponseNamespace="urn:server.issqn")]
        [return: System.Xml.Serialization.SoapElementAttribute("return")]
        public Nota gravaNota(paramsGravaNota @params) {
            object[] results = this.Invoke("gravaNota", new object[] {
                        @params});
            return ((Nota)(results[0]));
        }
        
        /// <remarks/>
        public void gravaNotaAsync(paramsGravaNota @params) {
            this.gravaNotaAsync(@params, null);
        }
        
        /// <remarks/>
        public void gravaNotaAsync(paramsGravaNota @params, object userState) {
            if ((this.gravaNotaOperationCompleted == null)) {
                this.gravaNotaOperationCompleted = new System.Threading.SendOrPostCallback(this.OngravaNotaOperationCompleted);
            }
            this.InvokeAsync("gravaNota", new object[] {
                        @params}, this.gravaNotaOperationCompleted, userState);
        }
        
        private void OngravaNotaOperationCompleted(object arg) {
            if ((this.gravaNotaCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.gravaNotaCompleted(this, new gravaNotaCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:server.issqn#gravaNotaXML", RequestNamespace="urn:server.issqn", ResponseNamespace="urn:server.issqn")]
        [return: System.Xml.Serialization.SoapElementAttribute("return")]
        public string gravaNotaXML(string @params) {
            object[] results = this.Invoke("gravaNotaXML", new object[] {
                        @params});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void gravaNotaXMLAsync(string @params) {
            this.gravaNotaXMLAsync(@params, null);
        }
        
        /// <remarks/>
        public void gravaNotaXMLAsync(string @params, object userState) {
            if ((this.gravaNotaXMLOperationCompleted == null)) {
                this.gravaNotaXMLOperationCompleted = new System.Threading.SendOrPostCallback(this.OngravaNotaXMLOperationCompleted);
            }
            this.InvokeAsync("gravaNotaXML", new object[] {
                        @params}, this.gravaNotaXMLOperationCompleted, userState);
        }
        
        private void OngravaNotaXMLOperationCompleted(object arg) {
            if ((this.gravaNotaXMLCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.gravaNotaXMLCompleted(this, new gravaNotaXMLCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:server.issqn#listarNotas", RequestNamespace="urn:server.issqn", ResponseNamespace="urn:server.issqn")]
        [return: System.Xml.Serialization.SoapElementAttribute("return")]
        public Nota[] listarNotas(paramsListarNotas @params) {
            object[] results = this.Invoke("listarNotas", new object[] {
                        @params});
            return ((Nota[])(results[0]));
        }
        
        /// <remarks/>
        public void listarNotasAsync(paramsListarNotas @params) {
            this.listarNotasAsync(@params, null);
        }
        
        /// <remarks/>
        public void listarNotasAsync(paramsListarNotas @params, object userState) {
            if ((this.listarNotasOperationCompleted == null)) {
                this.listarNotasOperationCompleted = new System.Threading.SendOrPostCallback(this.OnlistarNotasOperationCompleted);
            }
            this.InvokeAsync("listarNotas", new object[] {
                        @params}, this.listarNotasOperationCompleted, userState);
        }
        
        private void OnlistarNotasOperationCompleted(object arg) {
            if ((this.listarNotasCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.listarNotasCompleted(this, new listarNotasCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:server.issqn#listarNotasXML", RequestNamespace="urn:server.issqn", ResponseNamespace="urn:server.issqn")]
        [return: System.Xml.Serialization.SoapElementAttribute("return")]
        public string listarNotasXML(string @params) {
            object[] results = this.Invoke("listarNotasXML", new object[] {
                        @params});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void listarNotasXMLAsync(string @params) {
            this.listarNotasXMLAsync(@params, null);
        }
        
        /// <remarks/>
        public void listarNotasXMLAsync(string @params, object userState) {
            if ((this.listarNotasXMLOperationCompleted == null)) {
                this.listarNotasXMLOperationCompleted = new System.Threading.SendOrPostCallback(this.OnlistarNotasXMLOperationCompleted);
            }
            this.InvokeAsync("listarNotasXML", new object[] {
                        @params}, this.listarNotasXMLOperationCompleted, userState);
        }
        
        private void OnlistarNotasXMLOperationCompleted(object arg) {
            if ((this.listarNotasXMLCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.listarNotasXMLCompleted(this, new listarNotasXMLCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public partial class paramsGravaNota {
        
        private string cpfcnpjField;
        
        private string inscricaoField;
        
        private string chaveField;
        
        private string cepField;
        
        private System.DateTime dataField;
        
        private string modeloField;
        
        private string serieField;
        
        private string faturaField;
        
        private string orcamentoField;
        
        private System.DateTime vencimentoField;
        
        private tipoDeducao tipoField;
        
        private double pisField;
        
        private double csllField;
        
        private double cofinsField;
        
        private double irffField;
        
        private string situacaoField;
        
        private string optanteField;
        
        private double aliquotaField;
        
        private string textoField;
        
        private Servico[] servicosField;
        
        private Contribuinte tomadorField;
        
        /// <remarks/>
        public string cpfcnpj {
            get {
                return this.cpfcnpjField;
            }
            set {
                this.cpfcnpjField = value;
            }
        }
        
        /// <remarks/>
        public string inscricao {
            get {
                return this.inscricaoField;
            }
            set {
                this.inscricaoField = value;
            }
        }
        
        /// <remarks/>
        public string chave {
            get {
                return this.chaveField;
            }
            set {
                this.chaveField = value;
            }
        }
        
        /// <remarks/>
        public string cep {
            get {
                return this.cepField;
            }
            set {
                this.cepField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="date")]
        public System.DateTime data {
            get {
                return this.dataField;
            }
            set {
                this.dataField = value;
            }
        }
        
        /// <remarks/>
        public string modelo {
            get {
                return this.modeloField;
            }
            set {
                this.modeloField = value;
            }
        }
        
        /// <remarks/>
        public string serie {
            get {
                return this.serieField;
            }
            set {
                this.serieField = value;
            }
        }
        
        /// <remarks/>
        public string fatura {
            get {
                return this.faturaField;
            }
            set {
                this.faturaField = value;
            }
        }
        
        /// <remarks/>
        public string orcamento {
            get {
                return this.orcamentoField;
            }
            set {
                this.orcamentoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="date")]
        public System.DateTime vencimento {
            get {
                return this.vencimentoField;
            }
            set {
                this.vencimentoField = value;
            }
        }
        
        /// <remarks/>
        public tipoDeducao tipo {
            get {
                return this.tipoField;
            }
            set {
                this.tipoField = value;
            }
        }
        
        /// <remarks/>
        public double pis {
            get {
                return this.pisField;
            }
            set {
                this.pisField = value;
            }
        }
        
        /// <remarks/>
        public double csll {
            get {
                return this.csllField;
            }
            set {
                this.csllField = value;
            }
        }
        
        /// <remarks/>
        public double cofins {
            get {
                return this.cofinsField;
            }
            set {
                this.cofinsField = value;
            }
        }
        
        /// <remarks/>
        public double irff {
            get {
                return this.irffField;
            }
            set {
                this.irffField = value;
            }
        }
        
        /// <remarks/>
        public string situacao {
            get {
                return this.situacaoField;
            }
            set {
                this.situacaoField = value;
            }
        }
        
        /// <remarks/>
        public string optante {
            get {
                return this.optanteField;
            }
            set {
                this.optanteField = value;
            }
        }
        
        /// <remarks/>
        public double aliquota {
            get {
                return this.aliquotaField;
            }
            set {
                this.aliquotaField = value;
            }
        }
        
        /// <remarks/>
        public string texto {
            get {
                return this.textoField;
            }
            set {
                this.textoField = value;
            }
        }
        
        /// <remarks/>
        public Servico[] servicos {
            get {
                return this.servicosField;
            }
            set {
                this.servicosField = value;
            }
        }
        
        /// <remarks/>
        public Contribuinte tomador {
            get {
                return this.tomadorField;
            }
            set {
                this.tomadorField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public enum tipoDeducao {
        
        /// <remarks/>
        Percentual,
        
        /// <remarks/>
        Valor,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public partial class Servico {
        
        private double quantidadeField;
        
        private string atividadeField;
        
        private double valorField;
        
        private double deducaoField;
        
        private string codigoservicoField;
        
        private double aliquotaField;
        
        private double inssField;
        
        private double totalField;
        
        /// <remarks/>
        public double quantidade {
            get {
                return this.quantidadeField;
            }
            set {
                this.quantidadeField = value;
            }
        }
        
        /// <remarks/>
        public string atividade {
            get {
                return this.atividadeField;
            }
            set {
                this.atividadeField = value;
            }
        }
        
        /// <remarks/>
        public double valor {
            get {
                return this.valorField;
            }
            set {
                this.valorField = value;
            }
        }
        
        /// <remarks/>
        public double deducao {
            get {
                return this.deducaoField;
            }
            set {
                this.deducaoField = value;
            }
        }
        
        /// <remarks/>
        public string codigoservico {
            get {
                return this.codigoservicoField;
            }
            set {
                this.codigoservicoField = value;
            }
        }
        
        /// <remarks/>
        public double aliquota {
            get {
                return this.aliquotaField;
            }
            set {
                this.aliquotaField = value;
            }
        }
        
        /// <remarks/>
        public double inss {
            get {
                return this.inssField;
            }
            set {
                this.inssField = value;
            }
        }
        
        /// <remarks/>
        public double total {
            get {
                return this.totalField;
            }
            set {
                this.totalField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public partial class paramsListarNotas {
        
        private string anoField;
        
        private string mesField;
        
        private string cpfcnpjField;
        
        private string inscricaoField;
        
        private string chaveField;
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string ano {
            get {
                return this.anoField;
            }
            set {
                this.anoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string mes {
            get {
                return this.mesField;
            }
            set {
                this.mesField = value;
            }
        }
        
        /// <remarks/>
        public string cpfcnpj {
            get {
                return this.cpfcnpjField;
            }
            set {
                this.cpfcnpjField = value;
            }
        }
        
        /// <remarks/>
        public string inscricao {
            get {
                return this.inscricaoField;
            }
            set {
                this.inscricaoField = value;
            }
        }
        
        /// <remarks/>
        public string chave {
            get {
                return this.chaveField;
            }
            set {
                this.chaveField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public partial class Nota {
        
        private string guiaField;
        
        private string numeroField;
        
        private string mesField;
        
        private string cidadeField;
        
        private string ufField;
        
        private string exercicioField;
        
        private System.DateTime dataField;
        
        private string modeloField;
        
        private string serieField;
        
        private string apuracaoField;
        
        private double valorField;
        
        private double valorimpostoField;
        
        private string situacaoField;
        
        private double deducaoField;
        
        private double basecalculoField;
        
        private Servico[] servicosField;
        
        private Contribuinte tomadorField;
        
        private Contribuinte prestadorField;
        
        private string urlField;
        
        private string codigoField;
        
        private string mensagemField;
        
        /// <remarks/>
        public string guia {
            get {
                return this.guiaField;
            }
            set {
                this.guiaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string numero {
            get {
                return this.numeroField;
            }
            set {
                this.numeroField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string mes {
            get {
                return this.mesField;
            }
            set {
                this.mesField = value;
            }
        }
        
        /// <remarks/>
        public string cidade {
            get {
                return this.cidadeField;
            }
            set {
                this.cidadeField = value;
            }
        }
        
        /// <remarks/>
        public string uf {
            get {
                return this.ufField;
            }
            set {
                this.ufField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string exercicio {
            get {
                return this.exercicioField;
            }
            set {
                this.exercicioField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="date")]
        public System.DateTime data {
            get {
                return this.dataField;
            }
            set {
                this.dataField = value;
            }
        }
        
        /// <remarks/>
        public string modelo {
            get {
                return this.modeloField;
            }
            set {
                this.modeloField = value;
            }
        }
        
        /// <remarks/>
        public string serie {
            get {
                return this.serieField;
            }
            set {
                this.serieField = value;
            }
        }
        
        /// <remarks/>
        public string apuracao {
            get {
                return this.apuracaoField;
            }
            set {
                this.apuracaoField = value;
            }
        }
        
        /// <remarks/>
        public double valor {
            get {
                return this.valorField;
            }
            set {
                this.valorField = value;
            }
        }
        
        /// <remarks/>
        public double valorimposto {
            get {
                return this.valorimpostoField;
            }
            set {
                this.valorimpostoField = value;
            }
        }
        
        /// <remarks/>
        public string situacao {
            get {
                return this.situacaoField;
            }
            set {
                this.situacaoField = value;
            }
        }
        
        /// <remarks/>
        public double deducao {
            get {
                return this.deducaoField;
            }
            set {
                this.deducaoField = value;
            }
        }
        
        /// <remarks/>
        public double basecalculo {
            get {
                return this.basecalculoField;
            }
            set {
                this.basecalculoField = value;
            }
        }
        
        /// <remarks/>
        public Servico[] servicos {
            get {
                return this.servicosField;
            }
            set {
                this.servicosField = value;
            }
        }
        
        /// <remarks/>
        public Contribuinte tomador {
            get {
                return this.tomadorField;
            }
            set {
                this.tomadorField = value;
            }
        }
        
        /// <remarks/>
        public Contribuinte prestador {
            get {
                return this.prestadorField;
            }
            set {
                this.prestadorField = value;
            }
        }
        
        /// <remarks/>
        public string url {
            get {
                return this.urlField;
            }
            set {
                this.urlField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string codigo {
            get {
                return this.codigoField;
            }
            set {
                this.codigoField = value;
            }
        }
        
        /// <remarks/>
        public string mensagem {
            get {
                return this.mensagemField;
            }
            set {
                this.mensagemField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1586.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.SoapTypeAttribute(Namespace="urn:server.issqn")]
    public partial class Contribuinte {
        
        private string enderecoField;
        
        private string numeroField;
        
        private string complementoField;
        
        private string bairroField;
        
        private string cepField;
        
        private string cidadeField;
        
        private string ufField;
        
        private string paisField;
        
        private string nomeField;
        
        private string nomefantasiaField;
        
        private string inscricaoField;
        
        private string passaporteField;
        
        private string cpfcnpjField;
        
        private string rgieField;
        
        private string emailField;
        
        private string dddField;
        
        private string foneField;
        
        /// <remarks/>
        public string endereco {
            get {
                return this.enderecoField;
            }
            set {
                this.enderecoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string numero {
            get {
                return this.numeroField;
            }
            set {
                this.numeroField = value;
            }
        }
        
        /// <remarks/>
        public string complemento {
            get {
                return this.complementoField;
            }
            set {
                this.complementoField = value;
            }
        }
        
        /// <remarks/>
        public string bairro {
            get {
                return this.bairroField;
            }
            set {
                this.bairroField = value;
            }
        }
        
        /// <remarks/>
        public string cep {
            get {
                return this.cepField;
            }
            set {
                this.cepField = value;
            }
        }
        
        /// <remarks/>
        public string cidade {
            get {
                return this.cidadeField;
            }
            set {
                this.cidadeField = value;
            }
        }
        
        /// <remarks/>
        public string uf {
            get {
                return this.ufField;
            }
            set {
                this.ufField = value;
            }
        }
        
        /// <remarks/>
        public string pais {
            get {
                return this.paisField;
            }
            set {
                this.paisField = value;
            }
        }
        
        /// <remarks/>
        public string nome {
            get {
                return this.nomeField;
            }
            set {
                this.nomeField = value;
            }
        }
        
        /// <remarks/>
        public string nomefantasia {
            get {
                return this.nomefantasiaField;
            }
            set {
                this.nomefantasiaField = value;
            }
        }
        
        /// <remarks/>
        public string inscricao {
            get {
                return this.inscricaoField;
            }
            set {
                this.inscricaoField = value;
            }
        }
        
        /// <remarks/>
        public string passaporte {
            get {
                return this.passaporteField;
            }
            set {
                this.passaporteField = value;
            }
        }
        
        /// <remarks/>
        public string cpfcnpj {
            get {
                return this.cpfcnpjField;
            }
            set {
                this.cpfcnpjField = value;
            }
        }
        
        /// <remarks/>
        public string rgie {
            get {
                return this.rgieField;
            }
            set {
                this.rgieField = value;
            }
        }
        
        /// <remarks/>
        public string email {
            get {
                return this.emailField;
            }
            set {
                this.emailField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string ddd {
            get {
                return this.dddField;
            }
            set {
                this.dddField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.SoapElementAttribute(DataType="integer")]
        public string fone {
            get {
                return this.foneField;
            }
            set {
                this.foneField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    public delegate void gravaNotaCompletedEventHandler(object sender, gravaNotaCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class gravaNotaCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal gravaNotaCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public Nota Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((Nota)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    public delegate void gravaNotaXMLCompletedEventHandler(object sender, gravaNotaXMLCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class gravaNotaXMLCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal gravaNotaXMLCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    public delegate void listarNotasCompletedEventHandler(object sender, listarNotasCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class listarNotasCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal listarNotasCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public Nota[] Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((Nota[])(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    public delegate void listarNotasXMLCompletedEventHandler(object sender, listarNotasXMLCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.6.1586.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class listarNotasXMLCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal listarNotasXMLCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591