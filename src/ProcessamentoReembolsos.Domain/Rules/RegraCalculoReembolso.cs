namespace ProcessamentoReembolsos.Domain.Rules;

using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Strategies;

public class RegraCalculoReembolso : RegraBase
{
    private readonly CalculadoraReembolsoFactory _factory;
    private readonly decimal _capReembolsoIndividual;

    public RegraCalculoReembolso(CalculadoraReembolsoFactory factory, decimal capReembolsoIndividual = 500m)
    {
        _factory = factory;
        _capReembolsoIndividual = capReembolsoIndividual;
    }

    public override void Processar(PedidoReembolso pedido, ContextoCliente contexto)
    {
        var calculadora = _factory.ObterCalculadora(pedido.TipoProcedimento);
        var percentual = calculadora.CalcularPercentual();
        var valor = pedido.ValorPago * percentual;
        var valorFinal = Math.Min(valor, _capReembolsoIndividual);

        pedido.Aprovar(valorFinal);
    }
}
