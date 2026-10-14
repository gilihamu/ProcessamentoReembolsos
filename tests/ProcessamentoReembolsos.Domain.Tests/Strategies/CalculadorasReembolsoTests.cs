namespace ProcessamentoReembolsos.Tests.Domain.Strategies;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Strategies;

public class CalculadorasReembolsoTests
{
    [Fact]
    public void ConsultaMedicaCalculadora_RetornaPercentualCorreto()
    {
        var calculadora = new ConsultaMedicaCalculadora();

        calculadora.CalcularPercentual().Should().Be(0.80m);
    }

    [Fact]
    public void ExameImagemCalculadora_RetornaPercentualCorreto()
    {
        var calculadora = new ExameImagemCalculadora();

        calculadora.CalcularPercentual().Should().Be(0.90m);
    }

    [Fact]
    public void ExameLaboratorialCalculadora_RetornaPercentualCorreto()
    {
        var calculadora = new ExameLaboratorialCalculadora();

        calculadora.CalcularPercentual().Should().Be(0.70m);
    }

    [Fact]
    public void OutrosCalculadora_RetornaPercentualCorreto()
    {
        var calculadora = new OutrosCalculadora();

        calculadora.CalcularPercentual().Should().Be(0.50m);
    }
}
