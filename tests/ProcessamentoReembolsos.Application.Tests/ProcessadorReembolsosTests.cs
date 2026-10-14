namespace ProcessamentoReembolsos.Tests.Application;

using FluentAssertions;
using ProcessamentoReembolsos.Application;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;
using ProcessamentoReembolsos.Domain.Strategies;

public class ProcessadorReembolsosTests
{
    private ProcessadorReembolsos CriarProcessador()
    {
        var factory = new CalculadoraReembolsoFactory();
        var regraData = new RegraValidacaoData(DateTime.Today);
        var regraLimitePedidos = new RegraLimitePedidos();
        var regraLimiteValor = new RegraLimiteValor(factory);
        var regraCalculo = new RegraCalculoReembolso(factory);

        regraData.DefinirProxima(regraLimitePedidos);
        regraLimitePedidos.DefinirProxima(regraLimiteValor);
        regraLimiteValor.DefinirProxima(regraCalculo);

        return new ProcessadorReembolsos(regraData);
    }

    [Fact]
    public void Processar_ClientesDiferentes_TodosAprovados()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>
        {
            new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-10), 100m, 1),
            new PedidoReembolso(2, TipoProcedimento.ExameImagem, DateTime.Today.AddDays(-10), 100m, 2),
            new PedidoReembolso(3, TipoProcedimento.ExameLaboratorial, DateTime.Today.AddDays(-10), 100m, 3)
        };

        processador.Processar(pedidos);

        pedidos.Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
    }

    [Fact]
    public void Processar_MesmoClienteCom6Pedidos_QuintoAprovadoSextoSuspeito()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>();

        for (int i = 1; i <= 6; i++)
            pedidos.Add(new PedidoReembolso(i, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-10), 100m, 100));

        processador.Processar(pedidos);

        pedidos.Take(5).Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
        pedidos[5].Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }

    [Fact]
    public void Processar_MesmoClienteUltrapassaLimiteValor_MarcaComoSuspeito()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>
        {
            new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today.AddDays(-10), 555m, 100),
            new PedidoReembolso(2, TipoProcedimento.ExameImagem, DateTime.Today.AddDays(-9), 555m, 100),
            new PedidoReembolso(3, TipoProcedimento.ExameImagem, DateTime.Today.AddDays(-8), 555m, 100),
            new PedidoReembolso(4, TipoProcedimento.ExameImagem, DateTime.Today.AddDays(-7), 555m, 100)
        };

        processador.Processar(pedidos);

        pedidos.Take(3).Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
        pedidos[3].Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }

    [Fact]
    public void Processar_PedidoForaDe90Dias_Rejeitado()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>
        {
            new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-100), 100m, 100)
        };

        processador.Processar(pedidos);

        pedidos[0].Status.Should().Be(StatusPedido.Rejeitado);
    }

    [Fact]
    public void Processar_ConflitoPrazoEFraude_StatusRejeitado()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>();

        for (int i = 1; i <= 5; i++)
            pedidos.Add(new PedidoReembolso(i, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-10), 100m, 100));

        pedidos.Add(new PedidoReembolso(6, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-100), 100m, 100));

        processador.Processar(pedidos);

        pedidos[5].Status.Should().Be(StatusPedido.Rejeitado);
    }

    [Fact]
    public void Processar_OrdemImporta_ContextoAtualizadoIncrementalmente()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>
        {
            new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-10), 100m, 100),
            new PedidoReembolso(2, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-9), 100m, 100)
        };

        processador.Processar(pedidos);

        pedidos[0].Status.Should().Be(StatusPedido.Aprovado);
        pedidos[0].ValorReembolsado.Should().Be(80m);
        pedidos[1].Status.Should().Be(StatusPedido.Aprovado);
        pedidos[1].ValorReembolsado.Should().Be(80m);
    }

    [Fact]
    public void Processar_PedidosForaDaJanela30Dias_NaoContamParaFraude()
    {
        var processador = CriarProcessador();
        var pedidos = new List<PedidoReembolso>();

        for (int i = 1; i <= 5; i++)
            pedidos.Add(new PedidoReembolso(i, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-40), 100m, 100));

        pedidos.Add(new PedidoReembolso(6, TipoProcedimento.ConsultaMedica, DateTime.Today.AddDays(-5), 100m, 100));

        processador.Processar(pedidos);

        pedidos.Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
    }
}
