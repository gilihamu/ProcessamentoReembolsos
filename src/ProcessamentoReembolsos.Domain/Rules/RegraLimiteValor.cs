namespace ProcessamentoReembolsos.Domain.Rules;

using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Strategies;
using Serilog;

public class RegraLimiteValor : RegraBase
{
    private readonly CalculadoraReembolsoFactory _factory;
    private readonly decimal _limiteValorPorPeriodo;
    private readonly decimal _capReembolsoIndividual;
    private readonly ILogger _logger;

    public RegraLimiteValor(CalculadoraReembolsoFactory factory, decimal limiteValorPorPeriodo = 1500m, decimal capReembolsoIndividual = 500m, ILogger? logger = null)
    {
        _factory = factory;
        _limiteValorPorPeriodo = limiteValorPorPeriodo;
        _capReembolsoIndividual = capReembolsoIndividual;
        _logger = logger ?? Log.ForContext<RegraLimiteValor>();
    }

    public override void Processar(PedidoReembolso pedido, ContextoCliente contexto)
    {
        var calculadora = _factory.ObterCalculadora(pedido.TipoProcedimento);
        var valorCalculado = pedido.ValorPago * calculadora.CalcularPercentual();
        var valorComCap = Math.Min(valorCalculado, _capReembolsoIndividual);
        var acumulado = contexto.ObterValorReembolsadoNoPeriodo(pedido.DataProcedimento);

        if (acumulado + valorComCap > _limiteValorPorPeriodo)
        {
            pedido.MarcarComoSuspeito();
            _logger.Information("Pedido {PedidoId} suspeito: valor acumulado {Acumulado:C} + {Atual:C} ultrapassa limite {Limite:C}", pedido.Id, acumulado, valorComCap, _limiteValorPorPeriodo);
            return;
        }

        base.Processar(pedido, contexto);
    }
}
