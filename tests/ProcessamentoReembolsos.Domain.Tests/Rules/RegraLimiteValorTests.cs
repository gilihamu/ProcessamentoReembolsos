namespace ProcessamentoReembolsos.Tests.Domain.Rules;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;
using ProcessamentoReembolsos.Domain.Strategies;

public class RegraLimiteValorTests
{
    [Fact]
    public void Processar_ValorAcumuladoMaisAtualMenorOuIgualA1500_NaoMarcaComoSuspeito()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraLimiteValor(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        var contexto = new ContextoCliente(123);

        contexto.RegistrarAprovacao(DateTime.Today, 1000m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_ValorExatamenteNoLimite_NaoMarcaComoSuspeito()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraLimiteValor(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today, 555m, 123);
        var contexto = new ContextoCliente(123);

        contexto.RegistrarAprovacao(DateTime.Today, 1000m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_ValorAcumuladoMaisAtualUltrapassaLimite_MarcaComoSuspeito()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraLimiteValor(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today, 600m, 123);
        var contexto = new ContextoCliente(123);

        contexto.RegistrarAprovacao(DateTime.Today, 1100m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
        pedido.ValorReembolsado.Should().Be(0);
    }

    [Fact]
    public void Processar_ConsideraCapDe500NoCalculo()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraLimiteValor(factory);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today, 1000m, 123);
        var contexto = new ContextoCliente(123);

        contexto.RegistrarAprovacao(DateTime.Today, 1000m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.Pendente);
    }

    [Fact]
    public void Processar_LimitesCustomizados_MarcaComoSuspeitoComLimiteReduzido()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regra = new RegraLimiteValor(factory, limiteValorPorPeriodo: 300m, capReembolsoIndividual: 200m);
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 400m, 123);
        var contexto = new ContextoCliente(123);

        contexto.RegistrarAprovacao(DateTime.Today, 200m);

        regra.Processar(pedido, contexto);

        pedido.Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }
}
