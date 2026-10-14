using Reembolso.Dados;
using Reembolso.Negocio;

namespace Reembolso.Tests;

// Atalhos para deixar os testes curtos e legíveis.
internal static class Apoio
{
    public static readonly DateOnly Hoje = new(2024, 4, 30);

    public static Pedido NovoPedido(
        int id = 1,
        string tipo = "Consulta Médica",
        DateOnly? data = null,
        decimal valorPago = 100m,
        int clienteId = 1)
        => new(id, tipo, data ?? Hoje, valorPago, clienteId);

    public static ProcessadorReembolsos NovoProcessador(LogFalso? log = null)
        => new(new TabelaProcedimentos(), new RepositorioReembolsosEmMemoria(), log ?? new LogFalso());
}

// Log em memória para verificar o que foi auditado sem depender do console.
internal class LogFalso : ILogAuditoria
{
    public List<PedidoProcessado> Pedidos { get; } = new();
    public List<string> Avisos { get; } = new();

    public void RegistrarPedido(PedidoProcessado pedido) => Pedidos.Add(pedido);
    public void RegistrarAviso(string mensagem) => Avisos.Add(mensagem);
}
