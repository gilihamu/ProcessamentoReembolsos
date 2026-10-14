namespace ProcessamentoReembolsos.Application;

using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Interfaces;

public class ProcessadorReembolsos
{
    private readonly IRegraProcessamento _pipeline;

    public ProcessadorReembolsos(IRegraProcessamento pipeline)
    {
        _pipeline = pipeline;
    }

    public void Processar(List<PedidoReembolso> pedidos)
    {
        var contextos = pedidos
            .Select(p => p.ClienteId)
            .Distinct()
            .ToDictionary(id => id, id => new ContextoCliente(id));

        foreach (var pedido in pedidos)
        {
            var contexto = contextos[pedido.ClienteId];
            _pipeline.Processar(pedido, contexto);

            if (pedido.Status == StatusPedido.Aprovado)
                contexto.RegistrarAprovacao(pedido.DataProcedimento, pedido.ValorReembolsado);
        }
    }
}
