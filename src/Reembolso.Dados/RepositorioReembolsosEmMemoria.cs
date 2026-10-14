namespace Reembolso.Dados;

// Implementação usada hoje: guarda tudo em memória durante a execução.
// O enunciado proíbe banco de dados, e como a CLI processa um lote por vez,
// a memória é suficiente para manter o histórico necessário às regras de fraude.
public class RepositorioReembolsosEmMemoria : IRepositorioReembolsos
{
    // Pedidos agrupados por cliente: as consultas de fraude são sempre por
    // cliente, então não precisamos varrer os pedidos de todo mundo.
    private readonly Dictionary<int, List<PedidoProcessado>> _pedidosPorCliente = new();

    // Adiciona o pedido à lista do cliente (cria a lista no primeiro pedido).
    public void Salvar(PedidoProcessado pedido)
    {
        var clienteId = pedido.Pedido.ClienteId;
        if (!_pedidosPorCliente.TryGetValue(clienteId, out var pedidos))
        {
            pedidos = new List<PedidoProcessado>();
            _pedidosPorCliente[clienteId] = pedidos;
        }

        pedidos.Add(pedido);
    }

    // Filtra somente os aprovados do cliente dentro do período informado.
    public IReadOnlyList<PedidoProcessado> ObterAprovadosDoCliente(int clienteId, DateOnly desde, DateOnly ate)
    {
        if (!_pedidosPorCliente.TryGetValue(clienteId, out var pedidos))
            return [];

        return pedidos
            .Where(p => p.Status == StatusPedido.Aprovado
                        && p.Pedido.DataProcedimento >= desde
                        && p.Pedido.DataProcedimento <= ate)
            .ToList();
    }
}
