using Reembolso.Dados;

namespace Reembolso.Negocio;

// Calcula quanto o cliente recebe de volta por um procedimento.
public class CalculadoraReembolso
{
    // Regra 2 - teto de R$ 500,00 por procedimento.
    public const decimal ValorMaximoPorProcedimento = 500m;

    private readonly TabelaProcedimentos _tabela;

    public CalculadoraReembolso(TabelaProcedimentos tabela)
    {
        _tabela = tabela;
    }

    // Regras 1 e 2: aplica o percentual do tipo de procedimento sobre o valor
    // pago e limita o resultado a R$ 500,00, mesmo que o percentual dê mais.
    // Usa decimal (e não double) porque é dinheiro, e arredonda para 2 casas
    // no padrão comercial (0,005 sobe para 0,01).
    public decimal Calcular(Pedido pedido)
    {
        var percentual = _tabela.ObterPercentual(pedido.TipoProcedimento);
        var valor = Math.Round(pedido.ValorPago * percentual, 2, MidpointRounding.AwayFromZero);
        return Math.Min(valor, ValorMaximoPorProcedimento);
    }
}
