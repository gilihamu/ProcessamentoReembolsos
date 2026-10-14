namespace ProcessamentoReembolsos.Tests.Domain.Strategies;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Strategies;

public class CalculadoraReembolsoFactoryTests
{
    [Theory]
    [InlineData(TipoProcedimento.ConsultaMedica, typeof(ConsultaMedicaCalculadora))]
    [InlineData(TipoProcedimento.ExameImagem, typeof(ExameImagemCalculadora))]
    [InlineData(TipoProcedimento.ExameLaboratorial, typeof(ExameLaboratorialCalculadora))]
    [InlineData(TipoProcedimento.Outros, typeof(OutrosCalculadora))]
    public void ObterCalculadora_RetornaCalculadoraCorreta(TipoProcedimento tipo, Type tipoEsperado)
    {
        var factory = new CalculadoraReembolsoFactory();

        var calculadora = factory.ObterCalculadora(tipo);

        calculadora.Should().BeOfType(tipoEsperado);
    }
}
