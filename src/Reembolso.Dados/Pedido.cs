namespace Reembolso.Dados;

// Pedido de reembolso exatamente como chega na entrada (uma linha do CSV).
// Fica na camada de dados porque é o que será gravado/lido do banco.
public record Pedido(
    int Id,
    string TipoProcedimento,
    DateOnly DataProcedimento,
    decimal ValorPago,
    int ClienteId);

// Pedido depois de passar pelas regras de negócio: quanto será reembolsado,
// qual foi a decisão e por quê (os motivos alimentam o log de auditoria).
public record PedidoProcessado(
    Pedido Pedido,
    decimal ValorReembolsado,
    StatusPedido Status,
    IReadOnlyList<string> Motivos);
