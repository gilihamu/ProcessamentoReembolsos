namespace ProcessamentoReembolsos.Tests.Domain.Rules;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;

public class RegraValidacaoDataTests
{
    [Fact]
    public void Processar_PedidoDentroDe90Dias_NaoRejeita()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, new DateTime(2026, 03, 01), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_PedidoExatamenteCom90Dias_NaoRejeita()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, dataReferencia.AddDays(-90), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_PedidoCom91Dias_Rejeita()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, dataReferencia.AddDays(-91), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Rejeitado);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Processar_PedidoMuitoAntigo_Rejeita()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, dataReferencia.AddYears(-1), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Rejeitado);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Processar_PedidoNoFuturo_NaoRejeita()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, dataReferencia.AddDays(10), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_PrazoCustomizado30Dias_RejeitaPedidoCom31Dias()
    {
        var dataReferencia = new DateTime(2026, 03, 19);
        var regra = new RegraValidacaoData(dataReferencia, prazoMaximoDias: 30);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, dataReferencia.AddDays(-31), 100m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Rejeitado);
    }
}
