namespace Reembolso.Dados;

// Contrato de persistência dos pedidos processados.
// A regra de negócio depende só desta interface, então trocar a memória por
// um banco Oracle (ou qualquer outro) não exige mexer nas regras.
public interface IRepositorioReembolsos
{
    // Grava o resultado de um pedido (aprovado, rejeitado ou suspeito),
    // para histórico e para as próximas verificações de fraude.
    void Salvar(PedidoProcessado pedido);

    // Devolve os pedidos APROVADOS de um cliente com data entre "desde" e "ate"
    // (inclusive). É a base das regras antifraude de 30 dias: só o que foi de
    // fato reembolsado conta para os limites de quantidade e de valor.
    IReadOnlyList<PedidoProcessado> ObterAprovadosDoCliente(int clienteId, DateOnly desde, DateOnly ate);
}
