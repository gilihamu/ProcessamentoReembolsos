namespace ProcessamentoReembolsos.Tests.Domain.Rules;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;
using ProcessamentoReembolsos.Domain.Strategies;

public class RegraCalculoReembolsoTests
{
    [Fact]
    public void Processar_ConsultaMedica_CalculaCorretamente()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 400m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(320m);
    }

    [Fact]
    public void Processar_ExameImagem_AplicaCapDe500()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today, 600m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(500m);
    }

    [Fact]
    public void Processar_ExameLaboratorial_CalculaCorretamente()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameLaboratorial, DateTime.Today, 200m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(140m);
    }

    [Fact]
    public void Processar_Outros_CalculaCorretamente()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.Outros, DateTime.Today, 150m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(75m);
    }

    [Fact]
    public void Processar_ValorExatamenteNoCap_RetornaCap()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 625m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(500m);
    }

    [Fact]
    public void Processar_ValorAcimaDoCap_RetornaCap()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 1000m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(500m);
    }

    [Fact]
    public void Processar_CapCustomizado200_LimitaReembolsoA200()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraCalculoReembolso(factory, capReembolsoIndividual: 200m);
        // ConsultaMedica = 80%, 400 * 0.8 = 320, cap 200 => 200
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 400m, 123);
        var contexto = new ContextoCliente(123);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Aprovado);
        pedido.ValorReembolsado.Should().Be(200m);
    }
}
