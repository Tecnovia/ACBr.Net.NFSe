﻿// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : Rafael Dias
// Created          : 27-01-2020
//
// Last Modified By : Rafael Dias
// Last Modified On : 06-02-2020
// ***********************************************************************
// <copyright file="ProviderABRASF201.cs" company="ACBr.Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Grupo ACBr.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ACBr.Net.Core;
using ACBr.Net.Core.Extensions;
using ACBr.Net.DFe.Core;
using ACBr.Net.DFe.Core.Document;
using ACBr.Net.DFe.Core.Serializer;
using ACBr.Net.NFSe.Configuracao;
using ACBr.Net.NFSe.Nota;

namespace ACBr.Net.NFSe.Providers
{
    /// <summary>
    /// Classe base para trabalhar com provedores que usam o padrão ABRASF 2.02
    /// </summary>
    /// <seealso cref="ProviderBase" />
    public abstract class ProviderABRASF201 : ProviderBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderABRASF201"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="municipio">The municipio.</param>
        protected ProviderABRASF201(ConfigNFSe config, ACBrMunicipioNFSe municipio) : base(config, municipio)
        {
            Name = "ABRASFv201";
            Versao = "2.01";
            UsaPrestadorEnvio = false;
            ConsultarNfseRpsResposta = "ConsultarNfseRpsResposta";
        }

        #endregion Constructors

        #region Properties

        protected string Versao { get; set; }

        protected bool UsaPrestadorEnvio { get; set; }

        protected string ConsultarNfseRpsResposta { get; set; }

        #endregion Properties

        #region Methods

        #region LoadXml

        /// <inheritdoc />
        public override NotaFiscal LoadXml(XDocument xml)
        {
            Guard.Against<XmlException>(xml == null, "Xml invalido.");

            XElement rootNFSe = null;
            XElement rootCanc = null;
            XElement rootSub = null;
            XElement rootRps;

            var rootGrupo = xml.ElementAnyNs("CompNfse");
            if (rootGrupo != null)
            {
                rootNFSe = rootGrupo.ElementAnyNs("Nfse")?.ElementAnyNs("InfNfse");
                rootSub = rootGrupo.ElementAnyNs("NfseCancelamento");
                rootCanc = rootGrupo.ElementAnyNs("NfseSubstituicao");
                rootRps = rootNFSe.ElementAnyNs("DeclaracaoPrestacaoServico")?.ElementAnyNs("InfDeclaracaoPrestacaoServico");
            }
            else
            {
                rootRps = xml.ElementAnyNs("Rps").ElementAnyNs("InfDeclaracaoPrestacaoServico");
            }

            Guard.Against<XmlException>(rootNFSe == null && rootRps == null, "Xml de RPS ou NFSe invalido.");

            var ret = new NotaFiscal();

            if (rootNFSe != null)
            {
                LoadNFSe(ret, rootNFSe);
                if (rootSub != null) LoadNFSeSub(ret, rootSub);
                if (rootCanc != null) LoadNFSeCancel(ret, rootCanc);
            }

            LoadRps(ret, rootRps);

            return ret;
        }

        protected virtual void LoadRps(NotaFiscal nota, XElement rpsRoot)
        {
            var rps = rpsRoot.ElementAnyNs("Rps");
            var ideRps = rps.ElementAnyNs("IdentificacaoRps");
            var subRps = ideRps.ElementAnyNs("RpsSubstituido");

            nota.IdentificacaoRps.Numero = ideRps.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
            nota.IdentificacaoRps.Serie = ideRps.ElementAnyNs("Serie")?.GetValue<string>() ?? string.Empty;
            nota.IdentificacaoRps.Tipo = ideRps.ElementAnyNs("Tipo")?.GetValue<TipoRps>() ?? TipoRps.RPS;

            nota.IdentificacaoRps.DataEmissao = rps.ElementAnyNs("DataEmissao")?.GetValue<DateTime>() ?? DateTime.MinValue;
            nota.Situacao = rps.ElementAnyNs("Status")?.GetValue<SituacaoNFSeRps>() ?? SituacaoNFSeRps.Normal;

            if (subRps != null)
            {
                nota.RpsSubstituido.NumeroRps = subRps.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
                nota.RpsSubstituido.Serie = subRps.ElementAnyNs("Serie")?.GetValue<string>() ?? string.Empty;
                nota.RpsSubstituido.Tipo = subRps.ElementAnyNs("Tipo")?.GetValue<TipoRps>() ?? TipoRps.RPS;
            }

            nota.Competencia = rpsRoot.ElementAnyNs("Competencia")?.GetValue<DateTime>() ?? DateTime.MinValue;

            var rootServico = rpsRoot.ElementAnyNs("Servico");
            if (rootServico != null)
            {
                var rootServicoValores = rootServico.ElementAnyNs("Valores");
                if (rootServicoValores != null)
                {
                    nota.Servico.Valores.ValorServicos = rootServicoValores.ElementAnyNs("ValorServicos")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorDeducoes = rootServicoValores.ElementAnyNs("ValorDeducoes")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorPis = rootServicoValores.ElementAnyNs("ValorPis")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorCofins = rootServicoValores.ElementAnyNs("ValorCofins")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorInss = rootServicoValores.ElementAnyNs("ValorInss")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorIr = rootServicoValores.ElementAnyNs("ValorIr")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorCsll = rootServicoValores.ElementAnyNs("ValorCsll")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorOutrasRetencoes = rootServicoValores.ElementAnyNs("OutrasRetencoes")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.ValorIss = rootServicoValores.ElementAnyNs("ValorIss")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.Aliquota = rootServicoValores.ElementAnyNs("Aliquota")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.DescontoCondicionado = rootServicoValores.ElementAnyNs("DescontoCondicionado")?.GetValue<decimal>() ?? 0;
                    nota.Servico.Valores.DescontoIncondicionado = rootServicoValores.ElementAnyNs("DescontoIncondicionado")?.GetValue<decimal>() ?? 0;
                }

                nota.Servico.Valores.IssRetido = (rootServico.ElementAnyNs("IssRetido")?.GetValue<int>() ?? 0) == 1 ? SituacaoTributaria.Retencao : SituacaoTributaria.Normal;

                if (rootServico.ElementAnyNs("ResponsavelRetencao") != null)
                    nota.Servico.ResponsavelRetencao = rootServico.ElementAnyNs("ResponsavelRetencao").GetValue<int>() == 1 ? ResponsavelRetencao.Prestador : ResponsavelRetencao.Tomador;

                nota.Servico.ItemListaServico = rootServico.ElementAnyNs("ItemListaServico")?.GetValue<string>() ?? string.Empty;
                nota.Servico.CodigoCnae = rootServico.ElementAnyNs("CodigoCnae")?.GetValue<string>() ?? string.Empty;
                nota.Servico.CodigoTributacaoMunicipio = rootServico.ElementAnyNs("CodigoTributacaoMunicipio")?.GetValue<string>() ?? string.Empty;
                nota.Servico.Discriminacao = rootServico.ElementAnyNs("Discriminacao")?.GetValue<string>() ?? string.Empty;
                nota.Servico.CodigoMunicipio = rootServico.ElementAnyNs("CodigoMunicipio")?.GetValue<int>() ?? 0;
                nota.Servico.CodigoPais = rootServico.ElementAnyNs("CodigoPais")?.GetValue<int>() ?? 0;
                nota.Servico.ExigibilidadeIss = (ExigibilidadeIss)(rootServico.ElementAnyNs("ExigibilidadeISS")?.GetValue<int>() - 1 ?? 0);
                nota.Servico.MunicipioIncidencia = rootServico.ElementAnyNs("MunicipioIncidencia")?.GetValue<int>() ?? 0;
                nota.Servico.NumeroProcesso = rootServico.ElementAnyNs("NumeroProcesso")?.GetValue<string>() ?? string.Empty;
            }

            // Prestador (RPS)
            var rootPrestador = rpsRoot.ElementAnyNs("Prestador");
            if (rootPrestador != null)
            {
                nota.Prestador.CpfCnpj = rootPrestador.ElementAnyNs("CpfCnpj")?.GetCPF_CNPJ();
                nota.Prestador.InscricaoMunicipal = rootPrestador.ElementAnyNs("InscricaoMunicipal")?.GetValue<string>() ?? string.Empty;
            }

            LoadTomador(nota, rpsRoot);

            // Intermediario
            var rootIntermediario = rpsRoot.ElementAnyNs("Intermediario");
            if (rootIntermediario != null)
            {
                nota.Intermediario.RazaoSocial = rootIntermediario.ElementAnyNs("RazaoSocial")?.GetValue<string>() ?? string.Empty;

                var intermediarioIdentificacao = rootIntermediario.ElementAnyNs("IdentificacaoIntermediario");
                if (intermediarioIdentificacao != null)
                {
                    nota.Intermediario.CpfCnpj = intermediarioIdentificacao.ElementAnyNs("CpfCnpj")?.GetCPF_CNPJ();
                    nota.Intermediario.InscricaoMunicipal = intermediarioIdentificacao.ElementAnyNs("InscricaoMunicipal")?.GetValue<string>() ?? string.Empty;
                }
            }

            // Construção Civil
            var rootConstrucaoCivil = rpsRoot.ElementAnyNs("ConstrucaoCivil");
            if (rootConstrucaoCivil != null)
            {
                nota.ConstrucaoCivil.CodigoObra = rootConstrucaoCivil.ElementAnyNs("CodigoObra")?.GetValue<string>() ?? string.Empty;
                nota.ConstrucaoCivil.ArtObra = rootConstrucaoCivil.ElementAnyNs("Art")?.GetValue<string>() ?? string.Empty;
            }

            // Simples Nacional
            if (rpsRoot.ElementAnyNs("OptanteSimplesNacional")?.GetValue<int>() == 1)
            {
                nota.RegimeEspecialTributacao = RegimeEspecialTributacao.SimplesNacional;
            }
            else
            {
                // Regime Especial de Tributaçao
                switch (rpsRoot.ElementAnyNs("RegimeEspecialTributacao")?.GetValue<int>())
                {
                    case 1:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.MicroEmpresaMunicipal;
                        break;

                    case 2:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.Estimativa;
                        break;

                    case 3:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.SociedadeProfissionais;
                        break;

                    case 4:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.Cooperativa;
                        break;

                    case 5:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.MicroEmpresarioIndividual;
                        break;

                    case 6:
                        nota.RegimeEspecialTributacao = RegimeEspecialTributacao.MicroEmpresarioEmpresaPP;
                        break;
                }
            }

            // Incentivador Culturalstr
            switch (rpsRoot.ElementAnyNs("IncentivadorCultural")?.GetValue<int>())
            {
                case 1:
                    nota.IncentivadorCultural = NFSeSimNao.Sim;
                    break;

                case 2:
                    nota.IncentivadorCultural = NFSeSimNao.Nao;
                    break;
            }
        }

        protected virtual void LoadTomador(NotaFiscal nota, XElement rpsRoot)
        {
            // Tomador
            var rootTomador = rpsRoot.ElementAnyNs("Tomador");
            if (rootTomador == null) return;

            var tomadorIdentificacao = rootTomador.ElementAnyNs("IdentificacaoTomador");
            if (tomadorIdentificacao != null)
            {
                nota.Tomador.CpfCnpj = tomadorIdentificacao.ElementAnyNs("CpfCnpj")?.GetCPF_CNPJ();
                nota.Tomador.InscricaoMunicipal = tomadorIdentificacao.ElementAnyNs("InscricaoMunicipal")?.GetValue<string>() ?? string.Empty;
            }

            nota.Tomador.RazaoSocial = rootTomador.ElementAnyNs("RazaoSocial")?.GetValue<string>() ?? string.Empty;

            var rootTomadorEndereco = rootTomador.ElementAnyNs("Endereco");
            if (rootTomadorEndereco != null)
            {
                nota.Tomador.Endereco.Logradouro = rootTomadorEndereco.ElementAnyNs("Endereco")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.Endereco.Numero = rootTomadorEndereco.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.Endereco.Complemento = rootTomadorEndereco.ElementAnyNs("Complemento")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.Endereco.Bairro = rootTomadorEndereco.ElementAnyNs("Bairro")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.Endereco.CodigoMunicipio = rootTomadorEndereco.ElementAnyNs("CodigoMunicipio")?.GetValue<int>() ?? 0;
                nota.Tomador.Endereco.Uf = rootTomadorEndereco.ElementAnyNs("Uf")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.Endereco.CodigoPais = rootTomadorEndereco.ElementAnyNs("CodigoPais")?.GetValue<int>() ?? 0;
                nota.Tomador.Endereco.Cep = rootTomadorEndereco.ElementAnyNs("Cep")?.GetValue<string>() ?? string.Empty;
            }

            var rootTomadorContato = rootTomador.ElementAnyNs("Contato");
            if (rootTomadorContato != null)
            {
                nota.Tomador.DadosContato.DDD = "";
                nota.Tomador.DadosContato.Telefone = rootTomadorContato.ElementAnyNs("Telefone")?.GetValue<string>() ?? string.Empty;
                nota.Tomador.DadosContato.Email = rootTomadorContato.ElementAnyNs("Email")?.GetValue<string>() ?? string.Empty;
            }
        }

        protected virtual void LoadNFSe(NotaFiscal nota, XElement rootNFSe)
        {
            nota.IdentificacaoNFSe.Numero = rootNFSe.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
            nota.IdentificacaoNFSe.Chave = rootNFSe.ElementAnyNs("CodigoVerificacao")?.GetValue<string>() ?? string.Empty;
            nota.IdentificacaoNFSe.DataEmissao = rootNFSe.ElementAnyNs("DataEmissao")?.GetValue<DateTime>() ?? DateTime.MinValue;

            nota.RpsSubstituido.NumeroNfse = rootNFSe.ElementAnyNs("NfseSubstituida")?.GetValue<string>() ?? string.Empty;
            nota.OutrasInformacoes = rootNFSe.ElementAnyNs("OutrasInformacoes")?.GetValue<string>() ?? string.Empty;

            // Valores NFSe
            var valoresNFSe = rootNFSe.ElementAnyNs("ValoresNfse");
            if (valoresNFSe != null)
            {
                nota.Servico.Valores.BaseCalculo = valoresNFSe.ElementAnyNs("BaseCalculo")?.GetValue<decimal>() ?? 0;
                nota.Servico.Valores.Aliquota = valoresNFSe.ElementAnyNs("Aliquota")?.GetValue<decimal>() ?? 0;
                nota.Servico.Valores.ValorIss = valoresNFSe.ElementAnyNs("ValorIss")?.GetValue<decimal>() ?? 0;
                nota.Servico.Valores.ValorLiquidoNfse = valoresNFSe.ElementAnyNs("ValorLiquidoNfse")?.GetValue<decimal>() ?? 0;
            }

            nota.DescricaoCodigoTributacaoMunicípio = rootNFSe.ElementAnyNs("DescricaoCodigoTributacaoMunicípio")?.GetValue<string>() ?? string.Empty;
            nota.ValorCredito = rootNFSe.ElementAnyNs("ValorCredito")?.GetValue<decimal>() ?? 0;

            LoadPrestador(nota, rootNFSe);

            // Orgão Gerador
            var rootOrgaoGerador = rootNFSe.ElementAnyNs("OrgaoGerador");
            if (rootOrgaoGerador == null) return;

            nota.OrgaoGerador.CodigoMunicipio = rootOrgaoGerador.ElementAnyNs("CodigoMunicipio")?.GetValue<int>() ?? 0;
            nota.OrgaoGerador.Uf = rootOrgaoGerador.ElementAnyNs("Uf")?.GetValue<string>() ?? string.Empty;
        }

        protected virtual void LoadPrestador(NotaFiscal nota, XElement rootNFSe)
        {
            // Endereco Prestador
            var enderecoPrestador = rootNFSe.ElementAnyNs("EnderecoPrestadorServico");
            if (enderecoPrestador == null) return;

            nota.Prestador.Endereco.Logradouro = enderecoPrestador.ElementAnyNs("Endereco")?.GetValue<string>() ?? string.Empty;
            nota.Prestador.Endereco.Numero = enderecoPrestador.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
            nota.Prestador.Endereco.Complemento = enderecoPrestador.ElementAnyNs("Complemento")?.GetValue<string>() ?? string.Empty;
            nota.Prestador.Endereco.Bairro = enderecoPrestador.ElementAnyNs("Bairro")?.GetValue<string>() ?? string.Empty;
            nota.Prestador.Endereco.CodigoMunicipio = enderecoPrestador.ElementAnyNs("CodigoMunicipio")?.GetValue<int>() ?? 0;
            nota.Prestador.Endereco.Uf = enderecoPrestador.ElementAnyNs("Uf")?.GetValue<string>() ?? string.Empty;
            nota.Prestador.Endereco.CodigoPais = enderecoPrestador.ElementAnyNs("CodigoPais")?.GetValue<int>() ?? 0;
            nota.Prestador.Endereco.Cep = enderecoPrestador.ElementAnyNs("Cep")?.GetValue<string>() ?? string.Empty;
        }

        protected virtual void LoadNFSeSub(NotaFiscal nota, XElement rootSub)
        {
            nota.RpsSubstituido.NFSeSubstituidora = rootSub.ElementAnyNs("SubstituicaoNfse")?.ElementAnyNs("NfseSubstituida")?.GetValue<string>() ?? string.Empty;
            nota.RpsSubstituido.Signature = LoadSignature(rootSub.ElementAnyNs("Signature"));
        }

        protected virtual void LoadNFSeCancel(NotaFiscal nota, XElement rootCanc)
        {
            nota.Situacao = SituacaoNFSeRps.Cancelado;
            nota.Cancelamento.DataHora = rootCanc.ElementAnyNs("Confirmacao").ElementAnyNs("DataHora")?.GetValue<DateTime>() ?? DateTime.MinValue;
            nota.Cancelamento.Signature = LoadSignature(rootCanc.ElementAnyNs("Signature"));

            nota.Cancelamento.Pedido.CodigoCancelamento = rootCanc.ElementAnyNs("Confirmacao").ElementAnyNs("Pedido").ElementAnyNs("InfPedidoCancelamento")?.ElementAnyNs("CodigoCancelamento")?.GetValue<string>() ?? string.Empty;
            nota.Cancelamento.Pedido.Signature = LoadSignature(rootCanc.ElementAnyNs("Confirmacao").ElementAnyNs("Pedido").ElementAnyNs("Signature"));
        }

        #endregion LoadXml

        #region RPS

        /// <inheritdoc />
        public override string WriteXmlRps(NotaFiscal nota, bool identado = true, bool showDeclaration = true)
        {
            var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
            xmlDoc.Add(WriteRps(nota));
            return xmlDoc.AsString(identado, showDeclaration);
        }

        protected virtual XElement WriteRps(NotaFiscal nota)
        {
            var rootRps = new XElement("Rps");

            var infServico = new XElement("InfDeclaracaoPrestacaoServico", new XAttribute("Id", $"R{nota.IdentificacaoRps.Numero.OnlyNumbers()}"));
            rootRps.Add(infServico);

            infServico.Add(WriteRpsRps(nota));

            infServico.AddChild(AdicionarTag(TipoCampo.Dat, "", "Competencia", 10, 10, Ocorrencia.Obrigatoria, nota.Competencia));

            infServico.AddChild(WriteServicosRps(nota));
            infServico.AddChild(WritePrestadorRps(nota));
            infServico.AddChild(WriteTomadorRps(nota));
            infServico.AddChild(WriteIntermediarioRps(nota));
            infServico.AddChild(WriteConstrucaoCivilRps(nota));

            string regimeEspecialTributacao;
            string optanteSimplesNacional;
            if (nota.RegimeEspecialTributacao == RegimeEspecialTributacao.SimplesNacional)
            {
                regimeEspecialTributacao = "6";
                optanteSimplesNacional = "1";
            }
            else
            {
                regimeEspecialTributacao = ((int)nota.RegimeEspecialTributacao).ToString();
                optanteSimplesNacional = "2";
            }

            if (nota.RegimeEspecialTributacao != RegimeEspecialTributacao.Nenhum)
                infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "RegimeEspecialTributacao", 1, 1, Ocorrencia.NaoObrigatoria, regimeEspecialTributacao));

            infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "OptanteSimplesNacional", 1, 1, Ocorrencia.Obrigatoria, optanteSimplesNacional));
            infServico.AddChild(AdicionarTag(TipoCampo.Int, "", "IncentivoFiscal", 1, 1, Ocorrencia.Obrigatoria, nota.IncentivadorCultural == NFSeSimNao.Sim ? 1 : 2));

            return rootRps;
        }

        protected virtual XElement WriteRpsRps(NotaFiscal nota)
        {
            var rps = new XElement("Rps");

            rps.Add(WriteIdentificacaoRps(nota));

            rps.AddChild(AdicionarTag(TipoCampo.Dat, "", "DataEmissao", 10, 10, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.DataEmissao));
            rps.AddChild(AdicionarTag(TipoCampo.Int, "", "Status", 1, 1, Ocorrencia.Obrigatoria, (int)nota.Situacao + 1));

            rps.AddChild(WriteSubstituidoRps(nota));

            return rps;
        }

        protected virtual XElement WriteIdentificacaoRps(NotaFiscal nota)
        {
            var indRps = new XElement("IdentificacaoRps");

            indRps.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Numero", 1, 15, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.Numero));

            var serie = nota.IdentificacaoRps.Serie;

            //Algumas prefeituras não permitem controle de série de RPS
            // Sertãozinho/SP
            if (Municipio.Codigo.IsIn(3551702))
                serie = "00000";

            indRps.AddChild(AdicionarTag(TipoCampo.Str, "", "Serie", 1, 5, Ocorrencia.Obrigatoria, serie));
            indRps.AddChild(AdicionarTag(TipoCampo.Int, "", "Tipo", 1, 1, Ocorrencia.Obrigatoria, (int)nota.IdentificacaoRps.Tipo + 1));

            return indRps;
        }

        protected virtual XElement WriteSubstituidoRps(NotaFiscal nota)
        {
            if (nota.RpsSubstituido.NumeroRps.IsEmpty()) return null;

            var rpsSubstituto = new XElement("RpsSubstituido");

            rpsSubstituto.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Numero", 1, 15, Ocorrencia.Obrigatoria, nota.RpsSubstituido.NumeroRps));
            rpsSubstituto.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Serie", 1, 15, Ocorrencia.Obrigatoria, nota.RpsSubstituido.Serie));
            rpsSubstituto.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Tipo", 1, 15, Ocorrencia.Obrigatoria, (int)nota.RpsSubstituido.Tipo + 1));

            return rpsSubstituto;
        }

        protected virtual XElement WriteServicosRps(NotaFiscal nota)
        {
            var servico = new XElement("Servico");

            servico.Add(WriteValoresRps(nota));

            servico.AddChild(AdicionarTag(TipoCampo.Int, "", "IssRetido", 1, 1, Ocorrencia.Obrigatoria, nota.Servico.Valores.IssRetido == SituacaoTributaria.Retencao ? 1 : 2));

            if (nota.Servico.ResponsavelRetencao.HasValue)
                servico.AddChild(AdicionarTag(TipoCampo.Int, "", "ResponsavelRetencao", 1, 1, Ocorrencia.NaoObrigatoria, (int)nota.Servico.ResponsavelRetencao + 1));

            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "ItemListaServico", 1, 5, Ocorrencia.Obrigatoria, nota.Servico.ItemListaServico));
            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoCnae", 1, 7, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoCnae));
            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoTributacaoMunicipio", 1, 20, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoTributacaoMunicipio));
            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "Discriminacao", 1, 2000, Ocorrencia.Obrigatoria, nota.Servico.Discriminacao));
            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoMunicipio", 1, 20, Ocorrencia.Obrigatoria, nota.Servico.CodigoMunicipio));
            servico.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoPais", 4, 4, Ocorrencia.MaiorQueZero, nota.Servico.CodigoPais));
            servico.AddChild(AdicionarTag(TipoCampo.Int, "", "ExigibilidadeISS", 1, 1, Ocorrencia.Obrigatoria, (int)nota.Servico.ExigibilidadeIss + 1));
            servico.AddChild(AdicionarTag(TipoCampo.Int, "", "MunicipioIncidencia", 7, 7, Ocorrencia.MaiorQueZero, nota.Servico.MunicipioIncidencia));
            servico.AddChild(AdicionarTag(TipoCampo.Str, "", "NumeroProcesso", 1, 30, Ocorrencia.NaoObrigatoria, nota.Servico.NumeroProcesso));

            return servico;
        }

        protected virtual XElement WriteValoresRps(NotaFiscal nota)
        {
            var valores = new XElement("Valores");

            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorServicos", 1, 15, Ocorrencia.Obrigatoria, nota.Servico.Valores.ValorServicos));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorDeducoes", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorDeducoes));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorPis", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorPis));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorCofins", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorCofins));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorInss", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorInss));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorIr", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorIr));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorCsll", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorCsll));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "OutrasRetencoes", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.OutrasRetencoes));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorIss", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorIss));
            valores.AddChild(AdicionarTag(TipoCampo.De4, "", "Aliquota", 1, 6, Ocorrencia.MaiorQueZero, nota.Servico.Valores.Aliquota));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "DescontoIncondicionado", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.DescontoIncondicionado));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "DescontoCondicionado", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.DescontoCondicionado));

            return valores;
        }

        protected virtual XElement WritePrestadorRps(NotaFiscal nota)
        {
            if (nota.Prestador.CpfCnpj.IsEmpty() && nota.Prestador.InscricaoMunicipal.IsEmpty()) return null;

            var prestador = new XElement("Prestador");

            var cpfCnpjPrestador = new XElement("CpfCnpj");
            prestador.Add(cpfCnpjPrestador);

            cpfCnpjPrestador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Prestador.CpfCnpj));

            prestador.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria, nota.Prestador.InscricaoMunicipal));
            return prestador;
        }

        protected virtual XElement WriteTomadorRps(NotaFiscal nota)
        {
            if (nota.Tomador.CpfCnpj.IsEmpty()) return null;

            var tomador = new XElement("Tomador");

            var ideTomador = new XElement("IdentificacaoTomador");
            tomador.Add(ideTomador);

            var cpfCnpjTomador = new XElement("CpfCnpj");
            ideTomador.Add(cpfCnpjTomador);

            cpfCnpjTomador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Tomador.CpfCnpj));

            ideTomador.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria, nota.Tomador.InscricaoMunicipal));

            tomador.AddChild(AdicionarTag(TipoCampo.Str, "", "RazaoSocial", 1, 115, Ocorrencia.NaoObrigatoria, nota.Tomador.RazaoSocial));

            if (!nota.Tomador.Endereco.Logradouro.IsEmpty() ||
                !nota.Tomador.Endereco.Numero.IsEmpty() ||
                !nota.Tomador.Endereco.Complemento.IsEmpty() ||
                !nota.Tomador.Endereco.Bairro.IsEmpty() ||
                nota.Tomador.Endereco.CodigoMunicipio > 0 ||
                !nota.Tomador.Endereco.Uf.IsEmpty() ||
                nota.Tomador.Endereco.CodigoPais > 0 ||
                !nota.Tomador.Endereco.Cep.IsEmpty())
            {
                var endereco = new XElement("Endereco");
                tomador.Add(endereco);

                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Endereco", 1, 125, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Logradouro));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Numero", 1, 10, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Numero));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Complemento", 1, 60, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Complemento));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Bairro", 1, 60, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Bairro));
                endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoMunicipio", 7, 7, Ocorrencia.MaiorQueZero, nota.Tomador.Endereco.CodigoMunicipio));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Uf", 2, 2, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Uf));
                endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoPais", 4, 4, Ocorrencia.MaiorQueZero, nota.Tomador.Endereco.CodigoPais));
                endereco.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Cep", 8, 8, Ocorrencia.NaoObrigatoria, nota.Tomador.Endereco.Cep));
            }

            if (!nota.Tomador.DadosContato.Telefone.IsEmpty() ||
                !nota.Tomador.DadosContato.Email.IsEmpty())
            {
                var contato = new XElement("Contato");
                tomador.Add(contato);

                contato.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Telefone", 1, 11, Ocorrencia.NaoObrigatoria, nota.Tomador.DadosContato.DDD + nota.Tomador.DadosContato.Telefone));
                contato.AddChild(AdicionarTag(TipoCampo.Str, "", "Email", 1, 80, Ocorrencia.NaoObrigatoria, nota.Tomador.DadosContato.Email));
            }

            return tomador;
        }

        protected virtual XElement WriteIntermediarioRps(NotaFiscal nota)
        {
            if (nota.Intermediario.CpfCnpj.IsEmpty()) return null;

            var intermediario = new XElement("Intermediario");
            var ideIntermediario = new XElement("IdentificacaoIntermediario");
            intermediario.Add(ideIntermediario);

            var cpfCnpj = new XElement("CpfCnpj");
            ideIntermediario.Add(cpfCnpj);

            cpfCnpj.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Intermediario.CpfCnpj));

            ideIntermediario.AddChild(AdicionarTag(TipoCampo.Str, "", "InscricaoMunicipal", 1, 15, Ocorrencia.NaoObrigatoria, nota.Intermediario.InscricaoMunicipal));
            intermediario.AddChild(AdicionarTag(TipoCampo.Str, "", "RazaoSocial", 1, 115, Ocorrencia.NaoObrigatoria, nota.Intermediario.RazaoSocial));

            return intermediario;
        }

        protected virtual XElement WriteConstrucaoCivilRps(NotaFiscal nota)
        {
            if (nota.ConstrucaoCivil.ArtObra.IsEmpty()) return null;

            var construcao = new XElement("ConstrucaoCivil");
            construcao.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoObra", 1, 15, Ocorrencia.NaoObrigatoria, nota.ConstrucaoCivil.CodigoObra));
            construcao.AddChild(AdicionarTag(TipoCampo.Str, "", "Art", 1, 15, Ocorrencia.Obrigatoria, nota.ConstrucaoCivil.ArtObra));

            return construcao;
        }

        #endregion RPS

        #region NFSe

        public override string WriteXmlNFSe(NotaFiscal nota, bool identado = true, bool showDeclaration = true)
        {
            var xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
            var compNfse = new XElement("CompNfse");

            compNfse.AddChild(WriteNFSe(nota));
            compNfse.AddChild(WriteNFSeCancelamento(nota));
            compNfse.AddChild(WriteNFSeSubstituicao(nota));

            xmlDoc.AddChild(compNfse);
            return xmlDoc.AsString(identado, showDeclaration);
        }

        protected virtual XElement WriteNFSe(NotaFiscal nota)
        {
            var nfse = new XElement("Nfse", new XAttribute("versao", Versao));

            var infNfse = WriteInfoNFSe(nota);
            nfse.AddChild(infNfse);

            var valores = WriteValoresNFse(nota);
            infNfse.AddChild(valores);

            infNfse.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorCredito", 1, 15, Ocorrencia.MaiorQueZero, nota.ValorCredito));

            var endereco = WritePrestador(nota);
            infNfse.AddChild(endereco);

            var orgao = WriteOrgaoGerador(nota);
            infNfse.AddChild(orgao);

            var declaracao = WriteDeclaracaoServicoNFSe(nota);
            infNfse.AddChild(declaracao);

            return nfse;
        }

        protected virtual XElement WriteInfoNFSe(NotaFiscal nota)
        {
            var infNfse = new XElement("InfNfse", new XAttribute("Id", $"{nota.IdentificacaoNFSe.Numero.OnlyNumbers()}"));

            infNfse.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Numero", 1, 15, Ocorrencia.Obrigatoria, nota.IdentificacaoNFSe.Numero));
            infNfse.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoVerificacao", 1, 5, Ocorrencia.Obrigatoria, nota.IdentificacaoNFSe.Chave));
            infNfse.AddChild(AdicionarTag(TipoCampo.DatHor, "", "DataEmissao", 1, 1, Ocorrencia.Obrigatoria, nota.IdentificacaoNFSe.DataEmissao));
            infNfse.AddChild(AdicionarTag(TipoCampo.Str, "", "NfseSubstituida", 1, 15, Ocorrencia.NaoObrigatoria, nota.RpsSubstituido.NumeroNfse));
            infNfse.AddChild(AdicionarTag(TipoCampo.Str, "", "OutrasInformacoes", 1, 255, Ocorrencia.NaoObrigatoria, nota.OutrasInformacoes));

            return infNfse;
        }

        protected virtual XElement WriteValoresNFse(NotaFiscal nota)
        {
            var valores = new XElement("ValoresNfse");

            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "BaseCalculo", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.BaseCalculo));
            valores.AddChild(AdicionarTag(TipoCampo.De4, "", "Aliquota", 1, 6, Ocorrencia.MaiorQueZero, nota.Servico.Valores.Aliquota));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorIss", 1, 15, Ocorrencia.MaiorQueZero, nota.Servico.Valores.ValorIss));
            valores.AddChild(AdicionarTag(TipoCampo.De2, "", "ValorLiquidoNfse", 1, 15, Ocorrencia.Obrigatoria, nota.Servico.Valores.ValorLiquidoNfse));

            return valores;
        }

        protected virtual XElement WritePrestador(NotaFiscal nota)
        {
            if (nota.Prestador.Endereco.Logradouro.IsEmpty() && nota.Prestador.Endereco.Numero.IsEmpty() &&
                nota.Prestador.Endereco.Complemento.IsEmpty() && nota.Prestador.Endereco.Bairro.IsEmpty() &&
                nota.Prestador.Endereco.CodigoMunicipio <= 0 && nota.Prestador.Endereco.Uf.IsEmpty() &&
                nota.Prestador.Endereco.CodigoPais <= 0 && nota.Prestador.Endereco.Cep.IsEmpty()) return null;

            var endereco = new XElement("EnderecoPrestadorServico");
            endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Endereco", 1, 125, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Logradouro));
            endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Numero", 1, 10, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Numero));
            endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Complemento", 1, 60, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Complemento));
            endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Bairro", 1, 60, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Bairro));
            endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoMunicipio", 7, 7, Ocorrencia.MaiorQueZero, nota.Prestador.Endereco.CodigoMunicipio));
            endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "Uf", 2, 2, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Uf));
            endereco.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoPais", 4, 4, Ocorrencia.MaiorQueZero, nota.Prestador.Endereco.CodigoPais));
            endereco.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Cep", 8, 8, Ocorrencia.NaoObrigatoria, nota.Prestador.Endereco.Cep));

            return endereco;
        }

        protected virtual XElement WriteOrgaoGerador(NotaFiscal nota)
        {
            var orgao = new XElement("OrgaoGerador");

            orgao.AddChild(AdicionarTag(TipoCampo.Int, "", "CodigoMunicipio", 1, 20, Ocorrencia.Obrigatoria, nota.OrgaoGerador.CodigoMunicipio));
            orgao.AddChild(AdicionarTag(TipoCampo.Str, "", "Uf", 2, 2, Ocorrencia.Obrigatoria, nota.OrgaoGerador.Uf));

            return orgao;
        }

        protected virtual XElement WriteDeclaracaoServicoNFSe(NotaFiscal nota)
        {
            var declaracao = WriteRps(nota);
            declaracao.Name = "DeclaracaoPrestacaoServico";
            return declaracao;
        }

        protected virtual XElement WriteNFSeCancelamento(NotaFiscal nota)
        {
            if (nota.Situacao != SituacaoNFSeRps.Cancelado) return null;

            var cancelamento = new XElement("NfseCancelamento", new XAttribute("Versão", Versao));

            var confirmacao = new XElement("Confirmacao", new XAttribute("Id", nota.Cancelamento.Id));
            cancelamento.AddChild(confirmacao);

            var pedido = new XElement("Pedido");
            confirmacao.AddChild(pedido);

            var infPedido = new XElement("InfPedidoCancelamento", new XAttribute("Id", nota.Cancelamento.Id));
            pedido.AddChild(infPedido);

            var idNfSe = new XElement("IdentificacaoNfse");
            infPedido.AddChild(idNfSe);

            idNfSe.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "Numero", 1, 15, Ocorrencia.Obrigatoria, nota.Cancelamento.Pedido.IdentificacaoNFSe.Numero));

            var cpfCnpjTomador = new XElement("CpfCnpj");
            idNfSe.Add(cpfCnpjTomador);

            cpfCnpjTomador.AddChild(AdicionarTagCNPJCPF("", "Cpf", "Cnpj", nota.Prestador.CpfCnpj));

            idNfSe.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "InscricaoMunicipal", 1, 15, Ocorrencia.Obrigatoria, nota.Prestador.InscricaoMunicipal));
            idNfSe.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CodigoMunicipio", 1, 7, Ocorrencia.Obrigatoria, nota.Prestador.Endereco.CodigoMunicipio));

            infPedido.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CodigoCancelamento", 1, 4, Ocorrencia.Obrigatoria, nota.Cancelamento.Pedido.CodigoCancelamento));

            pedido.AddChild(WriteSignature(nota.Cancelamento.Pedido.Signature));

            confirmacao.AddChild(AdicionarTag(TipoCampo.DatHor, "", "DataHora", 20, 20, Ocorrencia.Obrigatoria, nota.Cancelamento.DataHora));

            cancelamento.AddChild(WriteSignature(nota.Cancelamento.Signature));

            return cancelamento;
        }

        protected virtual XElement WriteNFSeSubstituicao(NotaFiscal nota)
        {
            if (nota.RpsSubstituido.NFSeSubstituidora.IsEmpty()) return null;

            var substituidora = new XElement("NfseSubstituicao", new XAttribute("Versão", Versao));
            var subNFSe = new XElement("SubstituicaoNfse", new XAttribute("Id", nota.RpsSubstituido.Id));
            substituidora.AddChild(subNFSe);

            subNFSe.AddChild(AdicionarTag(TipoCampo.Int, "", "NfseSubstituidora", 1, 15, Ocorrencia.Obrigatoria, nota.RpsSubstituido.NFSeSubstituidora));
            subNFSe.AddChild(WriteSignature(nota.RpsSubstituido.Signature));

            return substituidora;
        }

        #endregion NFSe

        #region Services

        /// <inheritdoc />
        public override RetornoWebservice Enviar(int lote, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            if (lote == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lote não informado." });
            if (notas.Count == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "RPS não informado." });
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var xmlLoteRps = new StringBuilder();

            foreach (var nota in notas)
            {
                var xmlRps = WriteXmlRps(nota, false, false);
                GravarRpsEmDisco(xmlRps, $"Rps-{nota.IdentificacaoRps.DataEmissao:yyyyMMdd}-{nota.IdentificacaoRps.Numero}.xml", nota.IdentificacaoRps.DataEmissao);
                xmlLoteRps.Append(xmlRps);
            }

            var xmlLote = new StringBuilder();
            xmlLote.Append($"<EnviarLoteRpsEnvio {GetNamespace()}>");
            xmlLote.Append($"<LoteRps Id=\"L{lote}\" {GetVersao()}>");
            xmlLote.Append($"<NumeroLote>{lote}</NumeroLote>");
            if (UsaPrestadorEnvio) xmlLote.Append("<Prestador>");
            xmlLote.Append("<CpfCnpj>");
            xmlLote.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            xmlLote.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) xmlLote.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            if (UsaPrestadorEnvio) xmlLote.Append("</Prestador>");
            xmlLote.Append($"<QuantidadeRps>{notas.Count}</QuantidadeRps>");
            xmlLote.Append("<ListaRps>");
            xmlLote.Append(xmlLoteRps);
            xmlLote.Append("</ListaRps>");
            xmlLote.Append("</LoteRps>");
            xmlLote.Append("</EnviarLoteRpsEnvio>");
            retornoWebservice.XmlEnvio = xmlLote.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            retornoWebservice.XmlEnvio = XmlSigning.AssinarXmlTodos(retornoWebservice.XmlEnvio, "Rps", "InfDeclaracaoPrestacaoServico", Certificado);
            retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "EnviarLoteRpsEnvio", "LoteRps", Certificado);

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"lote-{lote}-env.xml");

            retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoverDeclaracaoXml();

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.Enviar));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.Enviar))
                {
                    retornoWebservice.XmlRetorno = cliente.RecepcionarLoteRps(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }
            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"lote-{lote}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            retornoWebservice.NumeroLote = xmlRet.Root?.ElementAnyNs("NumeroLote")?.GetValue<string>() ?? string.Empty;
            retornoWebservice.DataLote = xmlRet.Root?.ElementAnyNs("DataRecebimento")?.GetValue<DateTime>() ?? DateTime.MinValue;
            retornoWebservice.Protocolo = xmlRet.Root?.ElementAnyNs("Protocolo")?.GetValue<string>() ?? string.Empty;
            retornoWebservice.Sucesso = !retornoWebservice.NumeroLote.IsEmpty();

            if (!retornoWebservice.Sucesso) return retornoWebservice;

            // ReSharper disable once SuggestVarOrType_SimpleTypes
            foreach (NotaFiscal nota in notas)
            {
                nota.NumeroLote = retornoWebservice.NumeroLote;
            }

            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice EnviarSincrono(int lote, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            if (lote == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lote não informado." });
            if (notas.Count == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "RPS não informado." });
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var xmlLoteRps = new StringBuilder();

            foreach (var nota in notas)
            {
                var xmlRps = WriteXmlRps(nota, false, false);
                GravarRpsEmDisco(xmlRps, $"Rps-{nota.IdentificacaoRps.DataEmissao:yyyyMMdd}-{nota.IdentificacaoRps.Numero}.xml", nota.IdentificacaoRps.DataEmissao);
                xmlLoteRps.Append(xmlRps);
            }

            var xmlLote = new StringBuilder();
            xmlLote.Append($"<EnviarLoteRpsSincronoEnvio {GetNamespace()}>");
            xmlLote.Append($"<LoteRps Id=\"L{lote}\" {GetVersao()}>");
            xmlLote.Append($"<NumeroLote>{lote}</NumeroLote>");
            if (UsaPrestadorEnvio) xmlLote.Append("<Prestador>");
            xmlLote.Append("<CpfCnpj>");
            xmlLote.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            xmlLote.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) xmlLote.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            if (UsaPrestadorEnvio) xmlLote.Append("</Prestador>");
            xmlLote.Append($"<QuantidadeRps>{notas.Count}</QuantidadeRps>");
            xmlLote.Append("<ListaRps>");
            xmlLote.Append(xmlLoteRps);
            xmlLote.Append("</ListaRps>");
            xmlLote.Append("</LoteRps>");
            xmlLote.Append("</EnviarLoteRpsSincronoEnvio>");
            retornoWebservice.XmlEnvio = xmlLote.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            retornoWebservice.XmlEnvio = XmlSigning.AssinarXmlTodos(retornoWebservice.XmlEnvio, "Rps", "InfDeclaracaoPrestacaoServico", Certificado);
            retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "EnviarLoteRpsSincronoEnvio", "LoteRps", Certificado);

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"lote-{lote}-env.xml");

            retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoverDeclaracaoXml();

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.EnviarSincrono));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.EnviarSincrono))
                {
                    retornoWebservice.XmlRetorno = cliente.RecepcionarLoteRpsSincrono(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }

            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"lote-{lote}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            MensagemErro(retornoWebservice, xmlRet, "EnviarLoteRpsSincronoResposta");
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            retornoWebservice.NumeroLote = xmlRet.Root?.ElementAnyNs("NumeroLote")?.GetValue<string>() ?? string.Empty;
            retornoWebservice.DataLote = xmlRet.Root?.ElementAnyNs("DataRecebimento")?.GetValue<DateTime>() ?? DateTime.MinValue;
            retornoWebservice.Protocolo = xmlRet.Root?.ElementAnyNs("Protocolo")?.GetValue<string>() ?? string.Empty;
            retornoWebservice.Sucesso = !retornoWebservice.NumeroLote.IsEmpty();

            if (!retornoWebservice.Sucesso) return retornoWebservice;

            var listaNfse = xmlRet.Root.ElementAnyNs("ListaNfse");

            if (listaNfse == null)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lista de NFSe não encontrada! (ListaNfse)" });
                return retornoWebservice;
            }

            foreach (var compNfse in listaNfse.ElementsAnyNs("CompNfse"))
            {
                var nfse = compNfse.ElementAnyNs("Nfse").ElementAnyNs("InfNfse");
                var numeroNFSe = nfse.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
                var chaveNFSe = nfse.ElementAnyNs("CodigoVerificacao")?.GetValue<string>() ?? string.Empty;
                var dataNFSe = nfse.ElementAnyNs("DataEmissao")?.GetValue<DateTime>() ?? DateTime.Now;
                var numeroRps = nfse.ElementAnyNs("DeclaracaoPrestacaoServico")?
                                    .ElementAnyNs("InfDeclaracaoPrestacaoServico")?
                                    .ElementAnyNs("Rps")?
                                    .ElementAnyNs("IdentificacaoRps")?
                                    .ElementAnyNs("Numero").GetValue<string>() ?? string.Empty;

                GravarNFSeEmDisco(compNfse.AsString(true), $"NFSe-{numeroNFSe}-{chaveNFSe}-.xml", dataNFSe);

                var nota = notas.FirstOrDefault(x => x.IdentificacaoRps.Numero == numeroRps);
                if (nota == null)
                {
                    notas.Load(compNfse.ToString());
                }
                else
                {
                    nota.IdentificacaoNFSe.Numero = numeroNFSe;
                    nota.IdentificacaoNFSe.Chave = chaveNFSe;
                }

                nota.Protocolo = retornoWebservice.Protocolo;
            }

            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice ConsultarSituacao(int lote, string protocolo)
        {
            return ConsultarLoteRps(lote, protocolo, null);
        }

        /// <inheritdoc />
        public override RetornoWebservice ConsultarLoteRps(int lote, string protocolo, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            var loteBuilder = new StringBuilder();
            loteBuilder.Append($"<ConsultarLoteRpsEnvio {GetNamespace()}>");
            loteBuilder.Append("<Prestador>");
            loteBuilder.Append("<CpfCnpj>");
            loteBuilder.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            loteBuilder.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) loteBuilder.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            loteBuilder.Append("</Prestador>");
            loteBuilder.Append($"<Protocolo>{protocolo}</Protocolo>");
            loteBuilder.Append("</ConsultarLoteRpsEnvio>");
            retornoWebservice.XmlEnvio = loteBuilder.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"ConsultarLote-{DateTime.Now:yyyyMMddssfff}-{protocolo}-env.xml");

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.ConsultarLoteRps));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.ConsultarLoteRps))
                {
                    retornoWebservice.XmlRetorno = cliente.ConsultarLoteRps(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }
            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"ConsultarLote-{DateTime.Now:yyyyMMddssfff}-{lote}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);

            retornoWebservice.NumeroLote = xmlRet.Root?.ElementAnyNs("NumeroLote")?.GetValue<string>() ?? string.Empty;
            retornoWebservice.Protocolo = protocolo;

            var retornoLote = xmlRet.ElementAnyNs("ConsultarLoteRpsResposta");
            var situacao = retornoLote.ElementAnyNs("Situacao");
            if (situacao != null)
            {
                switch (situacao.GetValue<int>())
                {
                    case 2:
                        retornoWebservice.Situacao = "2 – Não Processado";
                        break;

                    case 3:
                        retornoWebservice.Situacao = "3 – Processado com Erro";
                        break;

                    case 4:
                        retornoWebservice.Situacao = "4 – Processado com Sucesso";
                        break;

                    default:
                        retornoWebservice.Situacao = "1 – Não Recebido";
                        break;
                }
            }

            MensagemErro(retornoWebservice, xmlRet, "ConsultarLoteRpsResposta");
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            retornoWebservice.Sucesso = true;

            if (notas == null) return retornoWebservice;

            var listaNfse = retornoLote?.ElementAnyNs("ListaNfse");

            if (listaNfse == null)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lista de NFSe não encontrada! (ListaNfse)" });
                return retornoWebservice;
            }

            foreach (var compNfse in listaNfse.ElementsAnyNs("CompNfse"))
            {
                var nfse = compNfse.ElementAnyNs("Nfse").ElementAnyNs("InfNfse");
                var numeroNFSe = nfse.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
                var chaveNFSe = nfse.ElementAnyNs("CodigoVerificacao")?.GetValue<string>() ?? string.Empty;
                var dataNFSe = nfse.ElementAnyNs("DataEmissao")?.GetValue<DateTime>() ?? DateTime.Now;
                var numeroRps = nfse.ElementAnyNs("DeclaracaoPrestacaoServico")?
                                    .ElementAnyNs("InfDeclaracaoPrestacaoServico")?
                                    .ElementAnyNs("Rps")?
                                    .ElementAnyNs("IdentificacaoRps")?
                                    .ElementAnyNs("Numero").GetValue<string>() ?? string.Empty;

                GravarNFSeEmDisco(compNfse.AsString(true), $"NFSe-{numeroNFSe}-{chaveNFSe}-.xml", dataNFSe);

                var nota = notas.FirstOrDefault(x => x.IdentificacaoRps.Numero == numeroRps);
                if (nota == null)
                {
                    nota = notas.Load(compNfse.ToString());
                }
                else
                {
                    nota.IdentificacaoNFSe.Numero = numeroNFSe;
                    nota.IdentificacaoNFSe.Chave = chaveNFSe;
                    nota.IdentificacaoNFSe.DataEmissao = dataNFSe;
                }

                nota.Protocolo = retornoWebservice.Protocolo;
                retornoWebservice.NotasFiscais.Add(nota);
            }

            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice CancelaNFSe(string codigoCancelamento, string numeroNFSe, string motivo, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            if (numeroNFSe.IsEmpty() || codigoCancelamento.IsEmpty())
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Número da NFSe/Codigo de cancelamento não informado para cancelamento." });
                return retornoWebservice;
            }

            var loteBuilder = new StringBuilder();

            loteBuilder.Append($"<CancelarNfseEnvio {GetNamespace()}>");
            loteBuilder.Append("<Pedido>");
            loteBuilder.Append($"<InfPedidoCancelamento Id=\"N{numeroNFSe}\">");
            loteBuilder.Append("<IdentificacaoNfse>");
            loteBuilder.Append($"<Numero>{numeroNFSe}</Numero>");
            loteBuilder.Append("<CpfCnpj>");
            loteBuilder.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            loteBuilder.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) loteBuilder.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            loteBuilder.Append($"<CodigoMunicipio>{Configuracoes.PrestadorPadrao.Endereco.CodigoMunicipio}</CodigoMunicipio>");
            loteBuilder.Append("</IdentificacaoNfse>");
            loteBuilder.Append($"<CodigoCancelamento>{codigoCancelamento}</CodigoCancelamento>");
            loteBuilder.Append("</InfPedidoCancelamento>");
            loteBuilder.Append("</Pedido>");
            loteBuilder.Append("</CancelarNfseEnvio>");
            retornoWebservice.XmlEnvio = loteBuilder.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "Pedido", "InfPedidoCancelamento", Certificado);

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"CanNFSe-{numeroNFSe}-env.xml");

            retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoverDeclaracaoXml();

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.CancelaNFSe));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.CancelaNFSe))
                {
                    retornoWebservice.XmlRetorno = cliente.CancelarNFSe(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }

            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"CanNFSe-{numeroNFSe}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            MensagemErro(retornoWebservice, xmlRet, "CancelarNfseResposta");
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var confirmacaoCancelamento = xmlRet.ElementAnyNs("CancelarNfseResposta")?.ElementAnyNs("RetCancelamento")?.ElementAnyNs("NfseCancelamento")?.ElementAnyNs("Confirmacao");
            if (confirmacaoCancelamento == null)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Confirmação do cancelamento não encontrada!" });
                return retornoWebservice;
            }

            retornoWebservice.DataLote = confirmacaoCancelamento.ElementAnyNs("DataHora")?.GetValue<DateTime>() ?? DateTime.MinValue;
            retornoWebservice.Sucesso = retornoWebservice.DataLote != DateTime.MinValue;

            // Se a nota fiscal cancelada existir na coleção de Notas Fiscais, atualiza seu status:
            var nota = notas.FirstOrDefault(x => x.IdentificacaoNFSe.Numero.Trim() == numeroNFSe);
            if (nota == null) return retornoWebservice;

            nota.Situacao = SituacaoNFSeRps.Cancelado;
            nota.Cancelamento.Pedido.CodigoCancelamento = codigoCancelamento;
            nota.Cancelamento.DataHora = confirmacaoCancelamento.ElementAnyNs("DataHora")?.GetValue<DateTime>() ?? DateTime.MinValue;
            nota.Cancelamento.MotivoCancelamento = motivo;
            nota.Cancelamento.Signature = DFeSignature.Load(confirmacaoCancelamento.ElementAnyNs("Pedido").ElementAnyNs("Signature").ToString());

            retornoWebservice.NotasFiscais.Add(nota);
            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice ConsultaNFSeRps(string numero, string serie, TipoRps tipo, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            if (numero.IsEmpty())
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Número da NFSe não informado para a consulta." });
                return retornoWebservice;
            }

            // Sertãozinho/SP
            if (Municipio.Codigo.IsIn(3551702))
                serie = "00000";

            var loteBuilder = new StringBuilder();
            loteBuilder.Append($"<ConsultarNfseRpsEnvio {GetNamespace()}>");
            loteBuilder.Append("<IdentificacaoRps>");
            loteBuilder.Append($"<Numero>{numero}</Numero>");
            loteBuilder.Append($"<Serie>{serie}</Serie>");
            loteBuilder.Append($"<Tipo>{(int)tipo + 1}</Tipo>");
            loteBuilder.Append("</IdentificacaoRps>");
            loteBuilder.Append("<Prestador>");
            loteBuilder.Append("<CpfCnpj>");
            loteBuilder.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            loteBuilder.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) loteBuilder.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            loteBuilder.Append("</Prestador>");
            loteBuilder.Append("</ConsultarNfseRpsEnvio>");
            retornoWebservice.XmlEnvio = loteBuilder.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"ConNotaRps-{numero}-env.xml");

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.ConsultaNFSeRps));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.ConsultaNFSeRps))
                {
                    retornoWebservice.XmlRetorno = cliente.ConsultarNFSePorRps(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }

            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"ConNotaRps-{numero}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            MensagemErro(retornoWebservice, xmlRet, ConsultarNfseRpsResposta);
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var compNfse = xmlRet.ElementAnyNs(ConsultarNfseRpsResposta)?.ElementAnyNs("CompNfse");

            if (compNfse == null)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Nota Fiscal não encontrada! (CompNfse)" });
                return retornoWebservice;
            }

            var nfse = compNfse.ElementAnyNs("Nfse").ElementAnyNs("InfNfse");
            var numeroNFSe = nfse.ElementAnyNs("Numero")?.GetValue<string>() ?? string.Empty;
            var chaveNFSe = nfse.ElementAnyNs("CodigoVerificacao")?.GetValue<string>() ?? string.Empty;
            var dataNFSe = nfse.ElementAnyNs("DataEmissao")?.GetValue<DateTime>() ?? DateTime.Now;
            var numeroRps = nfse.ElementAnyNs("DeclaracaoPrestacaoServico")?
                                .ElementAnyNs("InfDeclaracaoPrestacaoServico")?
                                .ElementAnyNs("Rps")?
                                .ElementAnyNs("IdentificacaoRps")?
                                .ElementAnyNs("Numero").GetValue<string>() ?? string.Empty;

            GravarNFSeEmDisco(compNfse.AsString(true), $"NFSe-{numeroNFSe}-{chaveNFSe}-.xml", dataNFSe);

            // Carrega a nota fiscal na coleção de Notas Fiscais
            var nota = notas.FirstOrDefault(x => x.IdentificacaoRps.Numero == numeroRps);

            if (nota == null)
            {
                nota = notas.Load(compNfse.ToString());
            }
            else
            {
                nota.IdentificacaoNFSe.Numero = numeroNFSe;
                nota.IdentificacaoNFSe.Chave = chaveNFSe;
                nota.IdentificacaoNFSe.DataEmissao = dataNFSe;
            }

            nota.Protocolo = retornoWebservice.Protocolo;
            retornoWebservice.NotasFiscais.Add(nota);

            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice ConsultaNFSe(DateTime? inicio, DateTime? fim, string numeroNfse, int pagina, string cnpjTomador,
            string imTomador, string nomeInter, string cnpjInter, string imInter, string serie, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();

            var loteBuilder = new StringBuilder();
            loteBuilder.Append($"<ConsultarNfseServicoPrestadoEnvio {GetNamespace()}>");
            loteBuilder.Append("<Prestador>");
            loteBuilder.Append("<CpfCnpj>");
            loteBuilder.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            loteBuilder.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) loteBuilder.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            loteBuilder.Append("</Prestador>");

            if (!numeroNfse.IsEmpty())
                loteBuilder.Append($"<NumeroNfse>{numeroNfse}</NumeroNfse>");

            if (inicio.HasValue && fim.HasValue)
            {
                loteBuilder.Append("<PeriodoEmissao>");
                loteBuilder.Append($"<DataInicial>{inicio:yyyy-MM-dd}</DataInicial>");
                loteBuilder.Append($"<DataFinal>{fim:yyyy-MM-dd}</DataFinal>");
                loteBuilder.Append("</PeriodoEmissao>");
            }

            if (!cnpjTomador.IsEmpty() && !imTomador.IsEmpty())
            {
                loteBuilder.Append("<Tomador>");
                loteBuilder.Append("<CpfCnpj>");
                loteBuilder.Append(cnpjTomador.IsCNPJ()
                    ? $"<Cnpj>{cnpjTomador.ZeroFill(14)}</Cnpj>"
                    : $"<Cpf>{cnpjTomador.ZeroFill(11)}</Cpf>");
                loteBuilder.Append("</CpfCnpj>");
                loteBuilder.Append($"<InscricaoMunicipal>{imTomador}</InscricaoMunicipal>");
                loteBuilder.Append("</Tomador>");
            }

            if (!cnpjInter.IsEmpty())
            {
                loteBuilder.Append("<Intermediario>");
                loteBuilder.Append("<CpfCnpj>");
                loteBuilder.Append(cnpjInter.IsCNPJ()
                    ? $"<Cnpj>{cnpjInter.ZeroFill(14)}</Cnpj>"
                    : $"<Cpf>{cnpjInter.ZeroFill(11)}</Cpf>");
                loteBuilder.Append("</CpfCnpj>");
                if (!imInter.IsEmpty()) loteBuilder.Append($"<InscricaoMunicipal>{imInter}</InscricaoMunicipal>");
                loteBuilder.Append("</Intermediario>");
            }

            if (pagina > 0)
                loteBuilder.Append($"<Pagina>{pagina}</Pagina>");
            loteBuilder.Append("</ConsultarNfseServicoPrestadoEnvio>");
            retornoWebservice.XmlEnvio = loteBuilder.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"ConNota-{DateTime.Now:yyyyMMddssfff}-{numeroNfse}-env.xml");

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.ConsultaNFSe));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.ConsultaNFSe))
                {
                    retornoWebservice.XmlRetorno = cliente.ConsultarNFSeServicoPrestado(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }
            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"ConNota-{DateTime.Now:yyyyMMddssfff}-{numeroNfse}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            MensagemErro(retornoWebservice, xmlRet, "ConsultarNfseServicoPrestadoResposta");
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var retornoLote = xmlRet.ElementAnyNs("ConsultarNfseServicoPrestadoResposta");
            var listaNfse = retornoLote?.ElementAnyNs("ListaNfse");
            if (listaNfse == null)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lista de NFSe não encontrada! (ListaNfse)" });
                return retornoWebservice;
            }

            foreach (var compNfse in listaNfse.ElementsAnyNs("CompNfse"))
            {
                // Carrega a nota fiscal na coleção de Notas Fiscais
                var nota = LoadXml(compNfse.AsString());

                GravarNFSeEmDisco(compNfse.AsString(true), $"NFSe-{nota.IdentificacaoNFSe.Numero}-{nota.IdentificacaoNFSe.Chave}-.xml", nota.IdentificacaoNFSe.DataEmissao);

                retornoWebservice.NotasFiscais.Add(nota);
                notas.Add(nota);
            }

            retornoWebservice.ProximaPagina = listaNfse.ElementAnyNs("ProximaPagina")?.GetValue<int>() ?? 0;
            retornoWebservice.Sucesso = true;

            return retornoWebservice;
        }

        /// <inheritdoc />
        public override RetornoWebservice SubstituirNFSe(string codigoCancelamento, string numeroNFSe, string motivo, NotaFiscalCollection notas)
        {
            var retornoWebservice = new RetornoWebservice();
            if (numeroNFSe.IsEmpty()) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Número da NFSe não informado para substituição." });
            if (codigoCancelamento.IsEmpty()) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Codigo de cancelamento não informado para substituição." });
            if (motivo.IsEmpty()) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Motivo não informado para substituição." });
            if (notas.Count < 1) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Nota para subituição não informada." });

            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var pedidoCancelamento = new StringBuilder();
            pedidoCancelamento.Append("<Pedido>");
            pedidoCancelamento.Append($"<InfPedidoCancelamento Id=\"N{numeroNFSe}\">");
            pedidoCancelamento.Append("<IdentificacaoNfse>");
            pedidoCancelamento.Append($"<Numero>{numeroNFSe}</Numero>");
            pedidoCancelamento.Append("<CpfCnpj>");
            pedidoCancelamento.Append(Configuracoes.PrestadorPadrao.CpfCnpj.IsCNPJ()
                ? $"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>"
                : $"<Cpf>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(11)}</Cpf>");
            pedidoCancelamento.Append("</CpfCnpj>");
            if (!Configuracoes.PrestadorPadrao.InscricaoMunicipal.IsEmpty()) pedidoCancelamento.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            pedidoCancelamento.Append($"<CodigoMunicipio>{Configuracoes.PrestadorPadrao.Endereco.CodigoMunicipio}</CodigoMunicipio>");
            pedidoCancelamento.Append("</IdentificacaoNfse>");
            pedidoCancelamento.Append($"<CodigoCancelamento>{codigoCancelamento}</CodigoCancelamento>");
            pedidoCancelamento.Append("</InfPedidoCancelamento>");
            pedidoCancelamento.Append("</Pedido>");

            var loteBuilder = new StringBuilder();
            loteBuilder.Append($"<SubstituirNfseEnvio {GetNamespace()}>");
            loteBuilder.Append($"<SubstituicaoNfse Id=\"SB{codigoCancelamento}\">");

            loteBuilder.Append(XmlSigning.AssinarXml(pedidoCancelamento.ToString(), "Pedido", "InfPedidoCancelamento", Certificado).RemoverDeclaracaoXml());

            var xmlRps = WriteXmlRps(notas[0], false, false);
            loteBuilder.Append(XmlSigning.AssinarXml(xmlRps, "Rps", "InfDeclaracaoPrestacaoServico", Certificado).RemoverDeclaracaoXml());
            GravarRpsEmDisco(xmlRps, $"Rps-{notas[0].IdentificacaoRps.DataEmissao:yyyyMMdd}-{notas[0].IdentificacaoRps.Numero}.xml", notas[0].IdentificacaoRps.DataEmissao);

            loteBuilder.Append("</SubstituicaoNfse>");
            loteBuilder.Append("</SubstituirNfseEnvio>");

            retornoWebservice.XmlEnvio = loteBuilder.ToString();

            if (Configuracoes.Geral.RetirarAcentos)
            {
                retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoveAccent();
            }

            retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "SubstituirNfseEnvio", "SubstituicaoNfse", Certificado);

            GravarArquivoEmDisco(retornoWebservice.XmlEnvio, $"SubsNFSe-{numeroNFSe}-env.xml");

            retornoWebservice.XmlEnvio = retornoWebservice.XmlEnvio.RemoverDeclaracaoXml();

            // Verifica Schema
            ValidarSchema(retornoWebservice, GetSchema(TipoUrl.SubstituirNFSe));
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            // Recebe mensagem de retorno
            try
            {
                using (var cliente = GetClient(TipoUrl.SubstituirNFSe))
                {
                    retornoWebservice.XmlRetorno = cliente.SubstituirNFSe(GerarCabecalho(), retornoWebservice.XmlEnvio);
                    retornoWebservice.EnvelopeEnvio = cliente.EnvelopeEnvio;
                    retornoWebservice.EnvelopeRetorno = cliente.EnvelopeRetorno;
                }
            }
            catch (Exception ex)
            {
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = ex.Message });
                return retornoWebservice;
            }
            GravarArquivoEmDisco(retornoWebservice.XmlRetorno, $"SubsNFSe-{numeroNFSe}-ret.xml");

            // Analisa mensagem de retorno
            var xmlRet = XDocument.Parse(retornoWebservice.XmlRetorno);
            MensagemErro(retornoWebservice, xmlRet, "SubstituirNfseResposta");
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            var retornoLote = xmlRet.ElementAnyNs("RetSubstituicao");
            var nfseSubstituida = retornoLote?.ElementAnyNs("NfseSubstituida");
            var nfseSubstituidora = retornoLote?.ElementAnyNs("NfseSubstituidora");

            if (nfseSubstituida == null) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "NFSe Substituida não encontrada! (NfseSubstituida)" });
            if (nfseSubstituidora == null) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "NFSe Substituidora não encontrada! (NfseSubstituidora)" });
            if (retornoWebservice.Erros.Any()) return retornoWebservice;

            retornoWebservice.Sucesso = true;

            notas.Load(nfseSubstituidora.ElementAnyNs("CompNfse").ToString());

            var compNfse = nfseSubstituida.ElementAnyNs("CompNfse");
            var nfse = compNfse.ElementAnyNs("Nfse").ElementAnyNs("InfNfse");
            var numeroRps = nfse.ElementAnyNs("DeclaracaoPrestacaoServico")?
                                .ElementAnyNs("InfDeclaracaoPrestacaoServico")?
                                .ElementAnyNs("Rps")?
                                .ElementAnyNs("IdentificacaoRps")?
                                .ElementAnyNs("Numero").GetValue<string>() ?? string.Empty;

            var nota = notas.FirstOrDefault(x => x.IdentificacaoRps.Numero == numeroRps);
            if (nota != null) return retornoWebservice;

            nota.RpsSubstituido.NFSeSubstituidora = notas.Last().IdentificacaoNFSe.Numero;
            nota.RpsSubstituido.DataEmissaoNfseSubstituida = notas.Last().IdentificacaoNFSe.DataEmissao;
            nota.RpsSubstituido.Id = notas.Last().Id;
            nota.RpsSubstituido.NumeroRps = notas.Last().IdentificacaoRps.Numero;
            nota.RpsSubstituido.Serie = notas.Last().IdentificacaoRps.Serie;
            nota.RpsSubstituido.Signature = notas.Last().Signature;

            return retornoWebservice;
        }

        #endregion Services

        #region Protected Methods

        protected abstract IABRASF2Client GetClient(TipoUrl tipo);

        protected virtual string GetVersao()
        {
            return $"versao=\"{Versao}\"";
        }

        protected virtual string GetNamespace()
        {
            return "xmlns=\"http://www.abrasf.org.br/nfse.xsd\"";
        }

        protected virtual string GetSchema(TipoUrl tipo)
        {
            return "nfse.xsd";
        }

        protected virtual string GerarCabecalho()
        {
            return $"<cabecalho {GetVersao()} {GetNamespace()}><versaoDados>{Versao}</versaoDados></cabecalho>";
        }

        protected virtual void MensagemErro(RetornoWebservice retornoWs, XContainer xmlRet, string xmlTag)
        {
            var mensagens = xmlRet?.ElementAnyNs(xmlTag);
            mensagens = mensagens?.ElementAnyNs("ListaMensagemRetorno") ?? mensagens?.ElementAnyNs("ListaMensagemRetornoLote");
            if (mensagens == null) return;

            foreach (var mensagem in mensagens.ElementsAnyNs("MensagemRetorno"))
            {
                var evento = new Evento
                {
                    Codigo = mensagem?.ElementAnyNs("Codigo")?.GetValue<string>() ?? string.Empty,
                    Descricao = mensagem?.ElementAnyNs("Mensagem")?.GetValue<string>() ?? string.Empty,
                    Correcao = mensagem?.ElementAnyNs("Correcao")?.GetValue<string>() ?? string.Empty
                };

                retornoWs.Erros.Add(evento);
            }
        }

        #endregion Protected Methods

        #endregion Methods
    }
}