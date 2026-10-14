using Reembolso.Negocio;
using static Reembolso.Tests.Apoio;

namespace Reembolso.Tests;

public class CalculadoraReembolsoTests
{
    private readonly CalculadoraReembolso _calculadora = new(new TabelaProcedimentos());

    [Theory]
    [InlineData("Consulta Médica", 400, 320)]
    [InlineData("Exame de Imagem", 100, 90)]
    [InlineData("Exame Laboratorial", 200, 140)]
    [InlineData("Outros", 150, 75)]
    public void Aplica_o_percentual_de_cada_tipo_de_procedimento(string tipo, decimal pago, decimal esperado)
    {
        Assert.Equal(esperado, _calculadora.Calcular(NovoPedido(tipo: tipo, valorPago: pago)));
    }

    [Fact]
    public void Limita_o_reembolso_a_500_quando_o_percentual_passa_do_teto()
    {
        Assert.Equal(500m, _calculadora.Calcular(NovoPedido(tipo: "Exame de Imagem", valorPago: 600m)));
    }

    [Fact]
    public void Reembolso_exatamente_no_teto_de_500_e_mantido()
    {
        Assert.Equal(500m, _calculadora.Calcular(NovoPedido(tipo: "Consulta Médica", valorPago: 625m)));
    }

    [Fact]
    public void Valor_um_centavo_abaixo_do_teto_nao_e_alterado()
    {
        Assert.Equal(499.99m, _calculadora.Calcular(NovoPedido(tipo: "Outros", valorPago: 999.98m)));
    }

    [Fact]
    public void Tipo_desconhecido_e_calculado_como_Outros()
    {
        Assert.Equal(50m, _calculadora.Calcular(NovoPedido(tipo: "Fisioterapia", valorPago: 100m)));
    }

    [Fact]
    public void Tipo_ignora_maiusculas_e_espacos()
    {
        Assert.Equal(80m, _calculadora.Calcular(NovoPedido(tipo: "  consulta médica ", valorPago: 100m)));
    }

    [Fact]
    public void Arredonda_para_duas_casas_no_padrao_comercial()
    {
        // 0,15 x 70% = 0,105 -> 0,11
        Assert.Equal(0.11m, _calculadora.Calcular(NovoPedido(tipo: "Exame Laboratorial", valorPago: 0.15m)));
    }
}
