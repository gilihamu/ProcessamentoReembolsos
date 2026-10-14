using Reembolso.Dados;

namespace Reembolso.Negocio;

// Aplica todas as regras do enunciado a um lote de pedidos e decide o status
// de cada um. É o único ponto que conhece a ordem de precedência das regras.
//
// Ordem de precedência (resolução de conflitos):
//   1. Rejeitado          -> pedido inválido (valor <= 0, data futura ou fora dos 90 dias).
//                            Pedido inválido nem chega à antifraude.
//   2. Suspeito de Fraude -> limite de 5 pedidos e/ou limite de R$ 1.500 em 30 dias.
//                            Se as duas regras de fraude disparam, o status é um só
//                            e os dois motivos vão para o log.
//   3. Aprovado           -> passou por todas as regras.
public class ProcessadorReembolsos
{
    // Regra 3 - só pedidos dos últimos 90 dias podem ser processados.
    public const int PrazoMaximoEmDias = 90;

    // Regra 4 - janela da antifraude e seus limites.
    public const int JanelaFraudeEmDias = 30;
    public const int MaximoPedidosNaJanela = 5;
    public const decimal ValorMaximoNaJanela = 1500m;

    private readonly TabelaProcedimentos _tabela;
    private readonly CalculadoraReembolso _calculadora;
    private readonly IRepositorioReembolsos _repositorio;
    private readonly ILogAuditoria _log;

    public ProcessadorReembolsos(TabelaProcedimentos tabela, IRepositorioReembolsos repositorio, ILogAuditoria log)
    {
        _tabela = tabela;
        _calculadora = new CalculadoraReembolso(tabela);
        _repositorio = repositorio;
        _log = log;
    }

    // Processa o lote inteiro e devolve os resultados NA MESMA ORDEM da entrada.
    //
    // Internamente os pedidos são avaliados em ordem cronológica (data e, no
    // empate, Id). Isso é necessário porque as regras de fraude olham para os
    // pedidos anteriores do cliente: o "6º pedido em 30 dias" é o 6º no tempo,
    // não o 6º linha do arquivo. Assim o resultado não depende de como o
    // arquivo foi ordenado.
    //
    // "dataReferencia" é o "hoje" usado na regra dos 90 dias. Recebê-la por
    // parâmetro (em vez de usar DateTime.Today aqui dentro) torna o
    // processamento reproduzível e testável.
    public IReadOnlyList<PedidoProcessado> Processar(IReadOnlyList<Pedido> pedidos, DateOnly dataReferencia)
    {
        var resultados = new PedidoProcessado[pedidos.Count];

        var posicoesEmOrdemCronologica = Enumerable.Range(0, pedidos.Count)
            .OrderBy(i => pedidos[i].DataProcedimento)
            .ThenBy(i => pedidos[i].Id);

        foreach (var posicao in posicoesEmOrdemCronologica)
        {
            var processado = ProcessarPedido(pedidos[posicao], dataReferencia);

            _repositorio.Salvar(processado);
            if (processado.Status != StatusPedido.Aprovado)
                _log.RegistrarPedido(processado);

            resultados[posicao] = processado;
        }

        return resultados;
    }

    // Decide o status de um único pedido seguindo a ordem de precedência
    // descrita no topo da classe. O valor reembolsável é sempre calculado e
    // informado na saída, mesmo para pedidos não aprovados, como no exemplo
    // do enunciado (pedido 4: Rejeitado com ValorReembolsado 75).
    private PedidoProcessado ProcessarPedido(Pedido pedido, DateOnly dataReferencia)
    {
        if (!_tabela.Conhece(pedido.TipoProcedimento))
            _log.RegistrarAviso($"Pedido {pedido.Id}: tipo de procedimento '{pedido.TipoProcedimento}' não cadastrado, calculado como '{TabelaProcedimentos.Outros}'.");

        var valorReembolsavel = _calculadora.Calcular(pedido);

        var motivoRejeicao = VerificarValidade(pedido, dataReferencia);
        if (motivoRejeicao is not null)
            return new PedidoProcessado(pedido, valorReembolsavel, StatusPedido.Rejeitado, [motivoRejeicao]);

        var motivosFraude = VerificarFraude(pedido, valorReembolsavel);
        if (motivosFraude.Count > 0)
            return new PedidoProcessado(pedido, valorReembolsavel, StatusPedido.SuspeitoDeFraude, motivosFraude);

        return new PedidoProcessado(pedido, valorReembolsavel, StatusPedido.Aprovado, []);
    }

    // Regra 3 - validade do pedido. Devolve o motivo da rejeição ou null se válido.
    //  - Valor pago zero ou negativo não gera reembolso: é dado inválido.
    //  - Data no futuro não pode ter sido realizada ainda: é dado inválido.
    //  - Data com mais de 90 dias antes da data de referência está fora do prazo.
    //    O dia 90 ainda é aceito (limite inclusivo).
    private static string? VerificarValidade(Pedido pedido, DateOnly dataReferencia)
    {
        if (pedido.ValorPago <= 0)
            return "valor pago deve ser maior que zero";

        if (pedido.DataProcedimento > dataReferencia)
            return $"data do procedimento ({pedido.DataProcedimento:yyyy-MM-dd}) está no futuro";

        var dataLimite = dataReferencia.AddDays(-PrazoMaximoEmDias);
        if (pedido.DataProcedimento < dataLimite)
            return $"fora do prazo de {PrazoMaximoEmDias} dias (aceita a partir de {dataLimite:yyyy-MM-dd})";

        return null;
    }

    // Regra 4 - prevenção de fraudes. Devolve a lista de motivos (vazia = ok).
    //
    // A janela de 30 dias termina na data do pedido e inclui o próprio dia:
    // [data - 29, data]. Só contam pedidos JÁ APROVADOS do cliente, porque
    // rejeitados e suspeitos não geraram reembolso e não devem "gastar" o
    // limite do cliente.
    //
    //  a) Quantidade: se o cliente já tem 5 aprovados na janela, este seria o 6º.
    //  b) Valor: se o total já aprovado + este reembolso passar de R$ 1.500,00.
    //     Exatamente R$ 1.500,00 ainda é permitido ("não pode ultrapassar").
    //     O pedido não é aprovado parcialmente: ou cabe inteiro, ou é suspeito.
    //
    // As duas regras são sempre avaliadas para que o log mostre todos os motivos.
    private List<string> VerificarFraude(Pedido pedido, decimal valorReembolsavel)
    {
        var motivos = new List<string>();

        var inicioJanela = pedido.DataProcedimento.AddDays(-(JanelaFraudeEmDias - 1));
        var aprovadosNaJanela = _repositorio.ObterAprovadosDoCliente(pedido.ClienteId, inicioJanela, pedido.DataProcedimento);

        if (aprovadosNaJanela.Count >= MaximoPedidosNaJanela)
            motivos.Add($"cliente já possui {aprovadosNaJanela.Count} reembolsos aprovados em {JanelaFraudeEmDias} dias (máximo {MaximoPedidosNaJanela})");

        var totalJaReembolsado = aprovadosNaJanela.Sum(p => p.ValorReembolsado);
        if (totalJaReembolsado + valorReembolsavel > ValorMaximoNaJanela)
            motivos.Add($"total em {JanelaFraudeEmDias} dias seria {totalJaReembolsado + valorReembolsavel:0.00} (máximo {ValorMaximoNaJanela:0.00})");

        return motivos;
    }
}
