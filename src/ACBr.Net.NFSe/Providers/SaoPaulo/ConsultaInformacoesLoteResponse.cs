﻿using System.ServiceModel;

namespace ACBr.Net.NFSe.Providers.SaoPaulo
{
    [MessageContractAttribute(WrapperName = "ConsultaInformacoesLoteResponse", WrapperNamespace = "http://www.prefeitura.sp.gov.br/nfe", IsWrapped = true)]
    public partial class ConsultaInformacoesLoteResponse
    {

        [MessageBodyMemberAttribute(Namespace = "http://www.prefeitura.sp.gov.br/nfe", Order = 0)]
        public string RetornoXML;

        public ConsultaInformacoesLoteResponse()
        {
        }

        public ConsultaInformacoesLoteResponse(string RetornoXML)
        {
            this.RetornoXML = RetornoXML;
        }
    }
}
