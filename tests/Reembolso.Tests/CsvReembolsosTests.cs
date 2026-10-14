using Reembolso.Cli;
using Reembolso.Dados;
using static Reembolso.Tests.Apoio;

namespace Reembolso.Tests;

public class CsvReembolsosTests
{
    private readonly StringWriter _erros = new();

    private List<Pedido> Ler(string texto) => CsvReembolsos.Ler(new StringReader(texto), new LogAuditoria(_erros));

    [Fact]
    public void Le_pedidos_ignorando_cabecalho_e_linhas_em_branco()
    {
        var pedidos = Ler("Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId\n\n1,Consulta Médica,2024-02-10,400.50,123\n");

        var pedido = Assert.Single(pedidos);
        Assert.Equal(new Pedido(1, "Consulta Médica", new DateOnly(2024, 2, 10), 400.50m, 123), pedido);
    }

    [Fact]
    public void Ignora_marca_BOM_no_inicio_do_arquivo()
    {
        var pedidos = Ler("﻿Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId\n1,Outros,2024-02-10,10,1");

        Assert.Single(pedidos);
    }

    [Theory]
    [InlineData("1,Outros,2024-02-10,10")]             // campo faltando
    [InlineData("x,Outros,2024-02-10,10,1")]           // Id inválido
    [InlineData("1, ,2024-02-10,10,1")]                // tipo vazio
    [InlineData("1,Outros,10/02/2024,10,1")]           // data em formato errado
    [InlineData("1,Outros,2024-02-30,10,1")]           // data inexistente
    [InlineData("1,Outros,2024-02-10,dez,1")]          // valor inválido
    [InlineData("1,Outros,2024-02-10,10,abc")]         // cliente inválido
    public void Linha_invalida_e_ignorada_e_registrada_no_log(string linhaInvalida)
    {
        var pedidos = Ler($"{linhaInvalida}\n2,Outros,2024-02-10,10,1");

        Assert.Equal(2, Assert.Single(pedidos).Id);
        Assert.Contains("Linha 1 ignorada", _erros.ToString());
    }

    [Fact]
    public void Escreve_cabecalho_e_valores_sem_zeros_desnecessarios()
    {
        var saida = new StringWriter();
        var processado = new PedidoProcessado(
            NovoPedido(id: 7, tipo: "Outros", data: new DateOnly(2024, 3, 1), valorPago: 175m, clienteId: 9),
            87.5m,
            StatusPedido.SuspeitoDeFraude,
            []);

        CsvReembolsos.Escrever(saida, [processado]);

        var linhas = saida.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(CsvReembolsos.CabecalhoSaida, linhas[0]);
        Assert.Equal("7,Outros,2024-03-01,175,87.5,9,Suspeito de Fraude", linhas[1]);
    }
}
