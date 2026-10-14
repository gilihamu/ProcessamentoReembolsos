namespace ProcessamentoReembolsos.Tests.Domain.Rules;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;

public class RegraLimitePedidosTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Processar_ClienteComMenosDe5Pedidos_NaoMarcaComoSuspeito(int pedidosAprovados)
    {
        var regra = new RegraLimitePedidos();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        var contexto = new ContextoCliente(123);

        for (int i = 0; i < pedidosAprovados; i++)
            contexto.RegistrarAprovacao(DateTime.Today, 100m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_ClienteComExatamente5Pedidos_MarcaComoSuspeito()
    {
        var regra = new RegraLimitePedidos();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        var contexto = new ContextoCliente(123);

        for (int i = 0; i < 5; i++)
            contexto.RegistrarAprovacao(DateTime.Today, 100m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Processar_ClienteComMaisDe5Pedidos_MarcaComoSuspeito()
    {
        var regra = new RegraLimitePedidos();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        var contexto = new ContextoCliente(123);

        for (int i = 0; i < 6; i++)
            contexto.RegistrarAprovacao(DateTime.Today, 100m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Processar_LimiteCustomizado2Pedidos_MarcaComoSuspeitoComExatamente2()
    {
        var regra = new RegraLimitePedidos(limitePedidosPorPeriodo: 2);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        var contexto = new ContextoCliente(123);

        for (int i = 0; i < 2; i++)
            contexto.RegistrarAprovacao(DateTime.Today, 100m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }
}
