namespace ProcessamentoReembolsos.Domain.Strategies;

using ProcessamentoReembolsos.Domain.Interfaces;

public class ConsultaMedicaCalculadora : ICalculadoraReembolso
{
    public decimal CalcularPercentual() => 0.80m;
}
