namespace ProcessamentoReembolsos.Domain.Rules;

using ProcessamentoReembolsos.Domain.Entities;
using Serilog;

public class RegraValidacaoData : RegraBase
{
    private readonly DateTime _dataReferencia;
    private readonly int _prazoMaximoDias;
    private readonly ILogger _logger;

    public RegraValidacaoData(DateTime dataReferencia, int prazoMaximoDias = 90, ILogger? logger = null)
    {
        _dataReferencia = dataReferencia;
        _prazoMaximoDias = prazoMaximoDias;
        _logger = logger ?? Log.ForContext<RegraValidacaoData>();
    }

    public override void Processar(PedidoReembolso pedido, ContextoCliente contexto)
    {
        var diasDecorridos = (_dataReferencia - pedido.DataProcedimento).TotalDays;

        if (diasDecorridos > _prazoMaximoDias)
        {
            pedido.Rejeitar();
            _logger.Information("Pedido {PedidoId} rejeitado por prazo: {DiasDecorridos:N0} dias > {PrazoMaximo} dias", pedido.Id, diasDecorridos, _prazoMaximoDias);
            return;
        }

        base.Processar(pedido, contexto);
    }
}
