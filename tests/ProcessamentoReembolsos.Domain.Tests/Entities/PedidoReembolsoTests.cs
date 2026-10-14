namespace ProcessamentoReembolsos.Tests.Domain.Entities;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;

public class PedidoReembolsoTests
{
    [Fact]
    public void PedidoReembolso_IniciaComStatusPendente()
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        pedido.Status.Should().Be(StatusPedido.Pendente);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Aprovar_DeveDefinirStatusEValorReembolsado()
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        pedido.Aprovar(80m);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(80m);
    }

    [Fact]
    public void Rejeitar_DeveDefinirStatusRejeitadoEValorZero()
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        pedido.Rejeitar();

        pedido.Status.Should().Be(StatusPedido.Rejeitado);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void MarcarComoSuspeito_DeveDefinirStatusSuspeitoEValorZero()
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        pedido.MarcarComoSuspeito();

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void JaProcessado_RetornaFalseQuandoPendente()
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        pedido.JaProcessado.Should().BeFalse();
    }

    [Theory]
    [InlineData(StatusPedido.Aprovado)]
    [InlineData(StatusPedido.Rejeitado)]
    [InlineData(StatusPedido.SuspeitoDeFraude)]
    public void JaProcessado_RetornaVerdadeiroParaOutrosStatus(StatusPedido status)
    {
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        if (status == StatusPedido.Aprovado)
            pedido.Aprovar(80m);
        else if (status == StatusPedido.Rejeitado)
            pedido.Rejeitar();
        else
            pedido.MarcarComoSuspeito();

        pedido.JaProcessado.Should().BeTrue();
    }
}
