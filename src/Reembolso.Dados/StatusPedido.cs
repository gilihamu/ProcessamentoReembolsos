namespace Reembolso.Dados;

// Os três resultados possíveis de um pedido, conforme o enunciado.
public enum StatusPedido
{
    Aprovado,
    Rejeitado,
    SuspeitoDeFraude
}

public static class StatusPedidoExtensions
{
    // O enum não pode ter espaço no nome, mas a saída precisa do texto exato
    // pedido no enunciado ("Suspeito de Fraude"). Esta é a única tradução.
    public static string ParaTexto(this StatusPedido status) => status switch
    {
        StatusPedido.Aprovado => "Aprovado",
        StatusPedido.Rejeitado => "Rejeitado",
        StatusPedido.SuspeitoDeFraude => "Suspeito de Fraude",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
