namespace ProcessamentoReembolsos.Tests.Integration;

using FluentAssertions;
using ProcessamentoReembolsos.Application;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;
using ProcessamentoReembolsos.Domain.Strategies;
using ProcessamentoReembolsos.Infrastructure;

public class FluxoCompletoTests
{
    [Fact]
    public void FluxoCompleto_ExemploDoDesafio_ProcessaCorretamente()
    {
        var csvEntrada = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-02-10,400,123
2,Exame de Imagem,2026-03-01,600,124
3,Exame Laboratorial,2026-03-15,200,125
4,Outros,2024-01-20,150,126";

        var factory = new CalculadoraReembolsoFactory();
        var regraData = new RegraValidacaoData(new DateTime(2026, 03, 19));
        var regraLimitePedidos = new RegraLimitePedidos();
        var regraLimiteValor = new RegraLimiteValor(factory);
        var regraCalculo = new RegraCalculoReembolso(factory);

        regraData.DefinirProxima(regraLimitePedidos);
        regraLimitePedidos.DefinirProxima(regraLimiteValor);
        regraLimiteValor.DefinirProxima(regraCalculo);

        var parser = new CsvParser();
        var writer = new CsvWriter();
        var processador = new ProcessadorReembolsos(regraData);

        var pedidos = parser.Parse(new StringReader(csvEntrada));
        processador.Processar(pedidos);

        var stringWriter = new StringWriter();
        writer.Write(stringWriter, pedidos);
        var csvSaida = stringWriter.ToString();

        pedidos[0].Status.Should().Be(StatusPedido.Aprovado);
        pedidos[0].ValorReembolsado.Should().Be(320m);

        pedidos[1].Status.Should().Be(StatusPedido.Aprovado);
        pedidos[1].ValorReembolsado.Should().Be(500m);

        pedidos[2].Status.Should().Be(StatusPedido.Aprovado);
        pedidos[2].ValorReembolsado.Should().Be(140m);

        pedidos[3].Status.Should().Be(StatusPedido.Rejeitado);
        pedidos[3].ValorReembolsado.Should().Be(0);

        csvSaida.Should().Contain("1,Consulta Médica,2026-02-10,400,320,123,Aprovado");
        csvSaida.Should().Contain("2,Exame de Imagem,2026-03-01,600,500,124,Aprovado");
        csvSaida.Should().Contain("3,Exame Laboratorial,2026-03-15,200,140,125,Aprovado");
        csvSaida.Should().Contain("4,Outros,2024-01-20,150,0,126,Rejeitado");
    }

    [Fact]
    public void FluxoCompleto_MaisQue5PedidosMesmoCliente_MarcaSextoComoSuspeito()
    {
        var csvEntrada = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-03-01,100,100
2,Consulta Médica,2026-03-02,100,100
3,Consulta Médica,2026-03-03,100,100
4,Consulta Médica,2026-03-04,100,100
5,Consulta Médica,2026-03-05,100,100
6,Consulta Médica,2026-03-06,100,100";

        var factory = new CalculadoraReembolsoFactory();
        var regraData = new RegraValidacaoData(new DateTime(2026, 03, 19));
        var regraLimitePedidos = new RegraLimitePedidos();
        var regraLimiteValor = new RegraLimiteValor(factory);
        var regraCalculo = new RegraCalculoReembolso(factory);

        regraData.DefinirProxima(regraLimitePedidos);
        regraLimitePedidos.DefinirProxima(regraLimiteValor);
        regraLimiteValor.DefinirProxima(regraCalculo);

        var parser = new CsvParser();
        var processador = new ProcessadorReembolsos(regraData);

        var pedidos = parser.Parse(new StringReader(csvEntrada));
        processador.Processar(pedidos);

        pedidos.Take(5).Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
        pedidos[5].Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }

    [Fact]
    public void FluxoCompleto_ValorTotalMaiorQue1500_MarcaComoSuspeito()
    {
        var csvEntrada = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Exame de Imagem,2026-03-01,555,200
2,Exame de Imagem,2026-03-02,555,200
3,Exame de Imagem,2026-03-03,555,200
4,Exame de Imagem,2026-03-04,555,200";

        var factory = new CalculadoraReembolsoFactory();
        var regraData = new RegraValidacaoData(new DateTime(2026, 03, 19));
        var regraLimitePedidos = new RegraLimitePedidos();
        var regraLimiteValor = new RegraLimiteValor(factory);
        var regraCalculo = new RegraCalculoReembolso(factory);

        regraData.DefinirProxima(regraLimitePedidos);
        regraLimitePedidos.DefinirProxima(regraLimiteValor);
        regraLimiteValor.DefinirProxima(regraCalculo);

        var parser = new CsvParser();
        var processador = new ProcessadorReembolsos(regraData);

        var pedidos = parser.Parse(new StringReader(csvEntrada));
        processador.Processar(pedidos);

        pedidos.Take(3).Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
        pedidos[3].Status.Should().Be(StatusPedido.SuspeitoDeFraude);
    }

    [Fact]
    public void FluxoCompleto_PedidosEspacados_JanelaTemporalRespeitada()
    {
        var csvEntrada = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-01-15,100,300
2,Consulta Médica,2026-01-16,100,300
3,Consulta Médica,2026-01-17,100,300
4,Consulta Médica,2026-01-18,100,300
5,Consulta Médica,2026-01-19,100,300
6,Consulta Médica,2026-03-01,100,300";

        var factory = new CalculadoraReembolsoFactory();
        var regraData = new RegraValidacaoData(new DateTime(2026, 03, 19));
        var regraLimitePedidos = new RegraLimitePedidos();
        var regraLimiteValor = new RegraLimiteValor(factory);
        var regraCalculo = new RegraCalculoReembolso(factory);

        regraData.DefinirProxima(regraLimitePedidos);
        regraLimitePedidos.DefinirProxima(regraLimiteValor);
        regraLimiteValor.DefinirProxima(regraCalculo);

        var parser = new CsvParser();
        var processador = new ProcessadorReembolsos(regraData);

        var pedidos = parser.Parse(new StringReader(csvEntrada));
        processador.Processar(pedidos);

        pedidos.Should().AllSatisfy(p => p.Status.Should().Be(StatusPedido.Aprovado));
    }
}
