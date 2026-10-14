namespace ProcessamentoReembolsos.Domain.Strategies;

using ProcessamentoReembolsos.Domain.Interfaces;

public class ExameImagemCalculadora : ICalculadoraReembolso
{
    public decimal CalcularPercentual() => 0.90m;
}
