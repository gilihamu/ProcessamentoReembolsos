namespace ProcessamentoReembolsos.Domain.Strategies;

using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Domain.Interfaces;

public class CalculadoraReembolsoFactory
{
    private readonly Dictionary<TipoProcedimento, ICalculadoraReembolso> _calculadoras;

    public CalculadoraReembolsoFactory()
    {
        _calculadoras = new Dictionary<TipoProcedimento, ICalculadoraReembolso>
        {
            { TipoProcedimento.ConsultaMedica, new ConsultaMedicaCalculadora() },
            { TipoProcedimento.ExameImagem, new ExameImagemCalculadora() },
            { TipoProcedimento.ExameLaboratorial, new ExameLaboratorialCalculadora() },
            { TipoProcedimento.Outros, new OutrosCalculadora() }
        };
    }

    public ICalculadoraReembolso ObterCalculadora(TipoProcedimento tipo)
    {
        return _calculadoras[tipo];
    }
}
