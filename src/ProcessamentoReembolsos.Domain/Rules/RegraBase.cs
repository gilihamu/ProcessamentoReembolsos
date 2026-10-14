namespace ProcessamentoReembolsos.Domain.Rules;

using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Interfaces;

public abstract class RegraBase : IRegraProcessamento
{
    private IRegraProcessamento? _proxima;

    public IRegraProcessamento DefinirProxima(IRegraProcessamento proxima)
    {
        _proxima = proxima;
        return proxima;
    }

    public virtual void Processar(PedidoReembolso pedido, ContextoCliente contexto)
    {
        if (!pedido.JaProcessado)
            _proxima?.Processar(pedido, contexto);
    }
}
