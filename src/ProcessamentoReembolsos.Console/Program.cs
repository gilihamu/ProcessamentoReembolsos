using System.Diagnostics;
using ProcessamentoReembolsos.Application;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Rules;
using ProcessamentoReembolsos.Domain.Strategies;
using ProcessamentoReembolsos.Infrastructure;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
    .CreateLogger();

try
{
    var sw = Stopwatch.StartNew();

    var factory = new CalculadoraReembolsoFactory();

    var regraData = new RegraValidacaoData(DateTime.Today, logger: Log.ForContext<RegraValidacaoData>());
    var regraLimitePedidos = new RegraLimitePedidos(logger: Log.ForContext<RegraLimitePedidos>());
    var regraLimiteValor = new RegraLimiteValor(factory, logger: Log.ForContext<RegraLimiteValor>());
    var regraCalculo = new RegraCalculoReembolso(factory);

    regraData.DefinirProxima(regraLimitePedidos);
    regraLimitePedidos.DefinirProxima(regraLimiteValor);
    regraLimiteValor.DefinirProxima(regraCalculo);

    var parser = new CsvParser();
    var writer = new CsvWriter();
    var processador = new ProcessadorReembolsos(regraData);

    var pedidos = parser.Parse(Console.In);
    processador.Processar(pedidos);
    writer.Write(Console.Out, pedidos);

    sw.Stop();

    var rejeitados = pedidos.Count(p => p.Status == StatusPedido.Rejeitado);
    var suspeitos = pedidos.Count(p => p.Status == StatusPedido.SuspeitoDeFraude);

    Log.Information("{Total} pedidos processados em {Tempo}ms — {Rejeitados} rejeitados, {Suspeitos} suspeitos de fraude",
        pedidos.Count, sw.ElapsedMilliseconds, rejeitados, suspeitos);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal no processamento de reembolsos");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
