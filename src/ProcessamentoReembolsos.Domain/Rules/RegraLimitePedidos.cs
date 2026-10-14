namespace ProcessamentoReembolsos.Domain.Rules;

using ProcessamentoReembolsos.Domain.Entities;
using Serilog;

public class RegraLimitePedidos : RegraBase
{
    private readonly int _limitePedidosPorPeriodo;
    private readonly ILogger _logger;

    public RegraLimitePedidos(int limitePedidosPorPeriodo = 5, ILogger? logger = null)
    {
        _limitePedidosPorPeriodo = limitePedidosPorPeriodo;
        _logger = logger ?? Log.ForContext<RegraLimitePedidos>();
    }

    public override void Processar(PedidoReembolso pedido, ContextoCliente contexto)
    {
        var quantidade = contexto.ObterPedidosAprovadosNoPeriodo(pedido.DataProcedimento);

        if (quantidade >= _limitePedidosPorPeriodo)
        {
            pedido.MarcarComoSuspeito();
            _logger.Information("Pedido {PedidoId} suspeito: cliente {ClienteId} com {Quantidade} pedidos no período (limite: {Limite})", pedido.Id, pedido.ClienteId, quantidade, _limitePedidosPorPeriodo);
            return;
        }

        base.Processar(pedido, contexto);
    }
}
