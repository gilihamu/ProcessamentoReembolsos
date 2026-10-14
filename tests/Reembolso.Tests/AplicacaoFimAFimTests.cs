using Reembolso.Cli;

namespace Reembolso.Tests;

// Testa o fluxo completo (CSV entra -> CSV sai) sem precisar de processo externo.
public class AplicacaoFimAFimTests
{
    private const string EntradaDoEnunciado =
        """
        Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
        1,Consulta Médica,2024-02-10,400,123
        2,Exame de Imagem,2024-03-01,600,124
        3,Exame Laboratorial,2024-04-15,200,125
        4,Outros,2024-01-20,150,126
        """;

    private static readonly string[] SaidaDoEnunciado =
    [
        "Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status",
        "1,Consulta Médica,2024-02-10,400,320,123,Aprovado",
        "2,Exame de Imagem,2024-03-01,600,500,124,Aprovado",
        "3,Exame Laboratorial,2024-04-15,200,140,125,Aprovado",
        "4,Outros,2024-01-20,150,75,126,Rejeitado",
    ];

    private static (int codigo, string[] saida, string erros) Executar(string entrada, params string[] args)
    {
        var saida = new StringWriter();
        var erros = new StringWriter();
        var codigo = Aplicacao.Executar(args, new StringReader(entrada), saida, erros, hoje: new DateOnly(2026, 1, 1));
        var linhas = saida.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        return (codigo, linhas, erros.ToString());
    }

    [Fact]
    public void Exemplo_do_enunciado_gera_exatamente_a_saida_esperada()
    {
        var (codigo, saida, erros) = Executar(EntradaDoEnunciado, "--data-referencia", "2024-04-30");

        Assert.Equal(0, codigo);
        Assert.Equal(SaidaDoEnunciado, saida);
        Assert.Contains("[AUDITORIA] Pedido 4", erros); // log vai para o stderr, não para o CSV
    }

    [Fact]
    public void Sem_data_de_referencia_usa_a_data_de_hoje()
    {
        // "Hoje" no teste é 2026-01-01, então todos os pedidos de 2024 estão fora do prazo.
        var (_, saida, _) = Executar(EntradaDoEnunciado);

        Assert.All(saida.Skip(1), linha => Assert.EndsWith(",Rejeitado", linha));
    }

    [Fact]
    public void Le_pedidos_do_arquivo_informado_em_utf8_com_parametros_em_qualquer_ordem()
    {
        var caminho = Path.GetTempFileName();
        try
        {
            File.WriteAllText(caminho, EntradaDoEnunciado, new System.Text.UTF8Encoding(true));

            var (codigo, saida, _) = Executar("", "--data-referencia", "2024-04-30", "--arquivo", caminho);

            Assert.Equal(0, codigo);
            Assert.Equal(SaidaDoEnunciado, saida);
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    [Fact]
    public void Arquivo_inexistente_retorna_erro()
    {
        var (codigo, saida, erros) = Executar("", "--arquivo", "nao-existe.csv");

        Assert.Equal(1, codigo);
        Assert.Empty(saida);
        Assert.Contains("Arquivo não encontrado", erros);
    }

    [Theory]
    [InlineData("--data-referencia")]
    [InlineData("--data-referencia", "30/04/2024")]
    [InlineData("--outro", "2024-04-30")]
    [InlineData("--arquivo")]
    [InlineData("--data-referencia", "2024-04-30", "--data-referencia", "2024-04-29")]
    [InlineData("--arquivo", "a.csv", "--arquivo", "b.csv")]
    public void Parametro_invalido_retorna_erro_de_uso(params string[] args)
    {
        var (codigo, saida, erros) = Executar(EntradaDoEnunciado, args);

        Assert.Equal(1, codigo);
        Assert.Empty(saida);
        Assert.StartsWith("Uso:", erros);
    }

    [Fact]
    public void Entrada_vazia_gera_apenas_o_cabecalho()
    {
        var (codigo, saida, _) = Executar("");

        Assert.Equal(0, codigo);
        Assert.Equal([CsvReembolsos.CabecalhoSaida], saida);
    }
}
