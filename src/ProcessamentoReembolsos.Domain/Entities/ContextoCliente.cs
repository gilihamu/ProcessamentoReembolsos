namespace ProcessamentoReembolsos.Domain.Entities;

public class ContextoCliente
{
    private readonly List<(DateTime Data, decimal Valor)> _aprovacoes = [];

    public int ClienteId { get; }

    public ContextoCliente(int clienteId)
    {
        ClienteId = clienteId;
    }

    public void RegistrarAprovacao(DateTime dataProcedimento, decimal valorReembolsado)
    {
        _aprovacoes.Add((dataProcedimento, valorReembolsado));
    }

    public int ObterPedidosAprovadosNoPeriodo(DateTime dataReferencia)
    {
        var limite = dataReferencia.AddDays(-30);
        return _aprovacoes.Count(a => a.Data >= limite);
    }

    public decimal ObterValorReembolsadoNoPeriodo(DateTime dataReferencia)
    {
        var limite = dataReferencia.AddDays(-30);
        return _aprovacoes.Where(a => a.Data >= limite).Sum(a => a.Valor);
    }
}
