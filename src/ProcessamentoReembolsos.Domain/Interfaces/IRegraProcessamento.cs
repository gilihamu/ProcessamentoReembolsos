namespace ProcessamentoReembolsos.Domain.Interfaces;

using ProcessamentoReembolsos.Domain.Entities;

public interface IRegraProcessamento
{
    void Processar(PedidoReembolso pedido, ContextoCliente contexto);
    IRegraProcessamento DefinirProxima(IRegraProcessamento proxima);
}
