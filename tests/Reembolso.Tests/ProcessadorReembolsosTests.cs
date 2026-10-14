using Reembolso.Dados;
using static Reembolso.Tests.Apoio;

namespace Reembolso.Tests;

public class ProcessadorReembolsosTests
{
    private static IReadOnlyList<PedidoProcessado> Processar(params Pedido[] pedidos)
        => NovoProcessador().Processar(pedidos, Hoje);

    // ---------- Regra 3: prazo de 90 dias e validade ----------

    [Fact]
    public void Pedido_de_hoje_e_aprovado()
    {
        Assert.Equal(StatusPedido.Aprovado, Processar(NovoPedido(data: Hoje))[0].Status);
    }

    [Fact]
    public void Pedido_com_exatamente_90_dias_e_aprovado()
    {
        Assert.Equal(StatusPedido.Aprovado, Processar(NovoPedido(data: Hoje.AddDays(-90)))[0].Status);
    }

    [Fact]
    public void Pedido_com_91_dias_e_rejeitado()
    {
        Assert.Equal(StatusPedido.Rejeitado, Processar(NovoPedido(data: Hoje.AddDays(-91)))[0].Status);
    }

    [Fact]
    public void Pedido_com_data_futura_e_rejeitado()
    {
        Assert.Equal(StatusPedido.Rejeitado, Processar(NovoPedido(data: Hoje.AddDays(1)))[0].Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Pedido_com_valor_pago_zero_ou_negativo_e_rejeitado(decimal valor)
    {
        Assert.Equal(StatusPedido.Rejeitado, Processar(NovoPedido(valorPago: valor))[0].Status);
    }

    [Fact]
    public void Pedido_rejeitado_ainda_informa_o_valor_calculado_como_no_enunciado()
    {
        var resultado = Processar(NovoPedido(tipo: "Outros", valorPago: 150m, data: Hoje.AddDays(-100)))[0];

        Assert.Equal(StatusPedido.Rejeitado, resultado.Status);
        Assert.Equal(75m, resultado.ValorReembolsado);
    }

    // ---------- Regra 4a: no máximo 5 pedidos em 30 dias ----------

    [Fact]
    public void Sexto_pedido_em_30_dias_e_suspeito_de_fraude()
    {
        var pedidos = Enumerable.Range(1, 6)
            .Select(i => NovoPedido(id: i, data: Hoje.AddDays(-10 + i), valorPago: 100m))
            .ToArray();

        var resultados = Processar(pedidos);

        Assert.All(resultados.Take(5), r => Assert.Equal(StatusPedido.Aprovado, r.Status));
        Assert.Equal(StatusPedido.SuspeitoDeFraude, resultados[5].Status);
    }

    [Fact]
    public void Sexto_pedido_no_31o_dia_e_aprovado_pois_a_janela_andou()
    {
        var inicio = Hoje.AddDays(-40);
        var pedidos = Enumerable.Range(1, 5)
            .Select(i => NovoPedido(id: i, data: inicio.AddDays(i - 1), valorPago: 100m))
            .Append(NovoPedido(id: 6, data: inicio.AddDays(30), valorPago: 100m)) // 1º pedido sai da janela
            .ToArray();

        Assert.All(Processar(pedidos), r => Assert.Equal(StatusPedido.Aprovado, r.Status));
    }

    [Fact]
    public void Pedidos_suspeitos_nao_contam_para_o_limite_de_quantidade()
    {
        var pedidos = Enumerable.Range(1, 7)
            .Select(i => NovoPedido(id: i, data: Hoje.AddDays(-10), valorPago: 10m))
            .ToArray();

        var resultados = Processar(pedidos);

        Assert.Equal(5, resultados.Count(r => r.Status == StatusPedido.Aprovado));
        Assert.Equal(2, resultados.Count(r => r.Status == StatusPedido.SuspeitoDeFraude));
    }

    [Fact]
    public void Pedidos_rejeitados_por_prazo_nao_contam_para_o_limite_de_quantidade()
    {
        // 5 pedidos com 91 dias (rejeitados) e 1 pedido com 85 dias, dentro da mesma janela de 30 dias.
        var pedidos = Enumerable.Range(1, 5)
            .Select(i => NovoPedido(id: i, data: Hoje.AddDays(-91), valorPago: 100m))
            .Append(NovoPedido(id: 6, data: Hoje.AddDays(-85), valorPago: 100m))
            .ToArray();

        var resultados = Processar(pedidos);

        Assert.All(resultados.Take(5), r => Assert.Equal(StatusPedido.Rejeitado, r.Status));
        Assert.Equal(StatusPedido.Aprovado, resultados[5].Status);
    }

    // ---------- Regra 4b: no máximo R$ 1.500 em 30 dias ----------

    [Fact]
    public void Total_exatamente_1500_e_aprovado_e_1500_e_1_centavo_e_suspeito()
    {
        var resultados = Processar(
            NovoPedido(id: 1, tipo: "Exame de Imagem", valorPago: 600m), // 500
            NovoPedido(id: 2, tipo: "Exame de Imagem", valorPago: 600m), // 500
            NovoPedido(id: 3, tipo: "Exame de Imagem", valorPago: 600m), // 500 -> total 1500
            NovoPedido(id: 4, tipo: "Outros", valorPago: 0.02m));        // 0,01 -> 1500,01

        Assert.Equal(StatusPedido.Aprovado, resultados[2].Status);
        Assert.Equal(StatusPedido.SuspeitoDeFraude, resultados[3].Status);
    }

    [Fact]
    public void Pedido_suspeito_por_valor_nao_consome_o_limite_do_cliente()
    {
        var resultados = Processar(
            NovoPedido(id: 1, tipo: "Exame de Imagem", valorPago: 600m),  // 500
            NovoPedido(id: 2, tipo: "Exame de Imagem", valorPago: 600m),  // 500
            NovoPedido(id: 3, tipo: "Consulta Médica", valorPago: 250m),  // 200 -> total 1200
            NovoPedido(id: 4, tipo: "Exame de Imagem", valorPago: 600m),  // 500 -> 1700, suspeito
            NovoPedido(id: 5, tipo: "Consulta Médica", valorPago: 375m)); // 300 -> 1500, cabe

        Assert.Equal(StatusPedido.SuspeitoDeFraude, resultados[3].Status);
        Assert.Equal(StatusPedido.Aprovado, resultados[4].Status);
    }

    // ---------- Conflitos e cenários gerais ----------

    [Fact]
    public void Quando_as_duas_regras_de_fraude_disparam_o_status_e_unico_com_dois_motivos()
    {
        var pedidos = Enumerable.Range(1, 5)
            .Select(i => NovoPedido(id: i, tipo: "Consulta Médica", valorPago: 350m)) // 280 cada -> 1400
            .Append(NovoPedido(id: 6, tipo: "Exame de Imagem", valorPago: 600m))      // 6º e total 1900
            .ToArray();

        var sexto = Processar(pedidos)[5];

        Assert.Equal(StatusPedido.SuspeitoDeFraude, sexto.Status);
        Assert.Equal(2, sexto.Motivos.Count);
    }

    [Fact]
    public void Prazo_tem_precedencia_sobre_fraude()
    {
        var pedidos = Enumerable.Range(1, 6)
            .Select(i => NovoPedido(id: i, data: Hoje.AddDays(-91), tipo: "Exame de Imagem", valorPago: 600m))
            .ToArray();

        Assert.All(Processar(pedidos), r => Assert.Equal(StatusPedido.Rejeitado, r.Status));
    }

    [Fact]
    public void Limites_de_fraude_sao_independentes_por_cliente()
    {
        var pedidos = Enumerable.Range(1, 10)
            .Select(i => NovoPedido(id: i, clienteId: i % 2, valorPago: 100m)) // 5 pedidos para cada cliente
            .ToArray();

        Assert.All(Processar(pedidos), r => Assert.Equal(StatusPedido.Aprovado, r.Status));
    }

    [Fact]
    public void Fraude_e_avaliada_em_ordem_cronologica_e_saida_mantem_a_ordem_da_entrada()
    {
        // O pedido mais recente vem primeiro no arquivo, mas é o 6º no tempo.
        var maisRecente = NovoPedido(id: 99, data: Hoje, valorPago: 100m);
        var anteriores = Enumerable.Range(1, 5)
            .Select(i => NovoPedido(id: i, data: Hoje.AddDays(-i), valorPago: 100m));

        var resultados = Processar([maisRecente, .. anteriores]);

        Assert.Equal(99, resultados[0].Pedido.Id);
        Assert.Equal(StatusPedido.SuspeitoDeFraude, resultados[0].Status);
        Assert.All(resultados.Skip(1), r => Assert.Equal(StatusPedido.Aprovado, r.Status));
    }

    [Fact]
    public void Pedidos_no_mesmo_dia_desempatam_pelo_id()
    {
        var pedidos = new[] { 6, 5, 4, 3, 2, 1 }.Select(id => NovoPedido(id: id, valorPago: 10m)).ToArray();

        var resultados = Processar(pedidos);

        Assert.Equal(StatusPedido.SuspeitoDeFraude, resultados.Single(r => r.Pedido.Id == 6).Status);
    }

    [Fact]
    public void Lote_vazio_devolve_lista_vazia()
    {
        Assert.Empty(Processar());
    }

    // ---------- Auditoria ----------

    [Fact]
    public void Somente_rejeitados_e_suspeitos_vao_para_o_log_de_auditoria()
    {
        var log = new LogFalso();
        var pedidos = new[]
        {
            NovoPedido(id: 1),
            NovoPedido(id: 2, data: Hoje.AddDays(-200)),
        };

        NovoProcessador(log).Processar(pedidos, Hoje);

        var auditado = Assert.Single(log.Pedidos);
        Assert.Equal(2, auditado.Pedido.Id);
        Assert.NotEmpty(auditado.Motivos);
    }

    [Fact]
    public void Tipo_desconhecido_gera_aviso_no_log()
    {
        var log = new LogFalso();

        NovoProcessador(log).Processar([NovoPedido(tipo: "Acupuntura")], Hoje);

        Assert.Single(log.Avisos);
    }
}
