namespace ProcessamentoReembolsos.Domain.Strategies;

using ProcessamentoReembolsos.Domain.Interfaces;

public class ExameLaboratorialCalculadora : ICalculadoraReembolso
{
    public decimal CalcularPercentual() => 0.70m;
}
