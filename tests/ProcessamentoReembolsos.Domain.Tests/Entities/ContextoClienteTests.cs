namespace ProcessamentoReembolsos.Tests.Domain.Entities;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;

public class ContextoClienteTests
{
    [Fact]
    public void ContextoCliente_IniciaComContadoresZerados()
    {
        var contexto = new ContextoCliente(123);
        var dataReferencia = DateTime.Today;

        contexto.ClienteId.Should().Be(123);
        contexto.ObterPedidosAprovadosNoPeriodo(dataReferencia).Should().Be(0);
        contexto.ObterValorReembolsadoNoPeriodo(dataReferencia).Should().Be(0);
    }

    [Fact]
    public void RegistrarAprovacao_IncrementaContadorEAcumulaValor()
    {
        var contexto = new ContextoCliente(123);
        var dataReferencia = DateTime.Today;

        contexto.RegistrarAprovacao(dataReferencia, 100m);

        contexto.ObterPedidosAprovadosNoPeriodo(dataReferencia).Should().Be(1);
        contexto.ObterValorReembolsadoNoPeriodo(dataReferencia).Should().Be(100m);
    }

    [Fact]
    public void RegistrarAprovacao_MultiplasAprovacoesAcumulamCorretamente()
    {
        var contexto = new ContextoCliente(123);
        var dataReferencia = DateTime.Today;

        contexto.RegistrarAprovacao(dataReferencia, 100m);
        contexto.RegistrarAprovacao(dataReferencia, 200m);
        contexto.RegistrarAprovacao(dataReferencia, 50m);

        contexto.ObterPedidosAprovadosNoPeriodo(dataReferencia).Should().Be(3);
        contexto.ObterValorReembolsadoNoPeriodo(dataReferencia).Should().Be(350m);
    }

    [Fact]
    public void ObterPedidosAprovadosNoPeriodo_FiltraPorData()
    {
        var contexto = new ContextoCliente(123);
        var dataReferencia = DateTime.Today;
        var dataAntes30Dias = dataReferencia.AddDays(-31);
        var dataDentro30Dias = dataReferencia.AddDays(-15);

        contexto.RegistrarAprovacao(dataAntes30Dias, 100m);
        contexto.RegistrarAprovacao(dataAntes30Dias, 200m);
        contexto.RegistrarAprovacao(dataDentro30Dias, 150m);
        contexto.RegistrarAprovacao(dataDentro30Dias, 250m);
        contexto.RegistrarAprovacao(dataReferencia, 300m);

        contexto.ObterPedidosAprovadosNoPeriodo(dataReferencia).Should().Be(3);
    }

    [Fact]
    public void ObterValorReembolsadoNoPeriodo_FiltraPorData()
    {
        var contexto = new ContextoCliente(123);
        var dataReferencia = DateTime.Today;
        var dataAntes30Dias = dataReferencia.AddDays(-31);
        var dataDentro30Dias = dataReferencia.AddDays(-15);

        contexto.RegistrarAprovacao(dataAntes30Dias, 100m);
        contexto.RegistrarAprovacao(dataAntes30Dias, 200m);
        contexto.RegistrarAprovacao(dataDentro30Dias, 150m);
        contexto.RegistrarAprovacao(dataDentro30Dias, 250m);
        contexto.RegistrarAprovacao(dataReferencia, 300m);

        contexto.ObterValorReembolsadoNoPeriodo(dataReferencia).Should().Be(700m);
    }
}
