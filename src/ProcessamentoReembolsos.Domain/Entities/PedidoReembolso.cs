namespace ProcessamentoReembolsos.Domain.Entities;

using ProcessamentoReembolsos.Domain.Enums;

public class PedidoReembolso
{
    public int Id { get; }
    public TipoProcedimento TipoProcedimento { get; }
    public DateTime DataProcedimento { get; }
    public decimal ValorPago { get; }
    public decimal ValorReembolsado { get; private set; }
    public int ClienteId { get; }
    public StatusPedido Status { get; private set; } = StatusPedido.Pendente;

    public PedidoReembolso(int id, TipoProcedimento tipo, DateTime data, decimal valorPago, int clienteId)
    {
        Id = id;
        TipoProcedimento = tipo;
        DataProcedimento = data;
        ValorPago = valorPago;
        ClienteId = clienteId;
    }

    public bool JaProcessado => Status != StatusPedido.Pendente;

    public void Aprovar(decimal valorReembolsado)
    {
        Status = StatusPedido.Aprovado;
        ValorReembolsado = valorReembolsado;
    }

    public void Rejeitar()
    {
        Status = StatusPedido.Rejeitado;
        ValorReembolsado = 0;
    }

    public void MarcarComoSuspeito()
    {
        Status = StatusPedido.SuspeitoDeFraude;
        ValorReembolsado = 0;
    }
}
