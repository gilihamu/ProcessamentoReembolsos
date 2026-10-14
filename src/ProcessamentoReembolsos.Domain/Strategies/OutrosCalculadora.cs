namespace ProcessamentoReembolsos.Domain.Strategies;

using ProcessamentoReembolsos.Domain.Interfaces;

public class OutrosCalculadora : ICalculadoraReembolso
{
    public decimal CalcularPercentual() => 0.50m;
}
