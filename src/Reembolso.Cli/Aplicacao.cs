using System.Globalization;
using System.Text;
using Reembolso.Dados;
using Reembolso.Negocio;

namespace Reembolso.Cli;

// Fluxo da linha de comando: ler -> processar -> escrever.
// Recebe entrada, saída e erros como parâmetros (em vez de usar Console
// direto) para que o fluxo completo possa ser testado com textos em memória.
public static class Aplicacao
{
    private const string ParametroDataReferencia = "--data-referencia";
    private const string ParametroArquivo = "--arquivo";

    private const string MensagemDeUso =
        $"Uso: Reembolso.Cli [{ParametroArquivo} entrada.csv] [{ParametroDataReferencia} AAAA-MM-DD]\n" +
        $"     Sem {ParametroArquivo}, os pedidos são lidos do stdin.";

    // Executa o programa e devolve o código de saída (0 = sucesso, 1 = erro de uso).
    // Aqui também é onde as dependências são montadas: para usar Oracle no
    // futuro bastaria trocar RepositorioReembolsosEmMemoria por RepositorioReembolsosOracle.
    public static int Executar(string[] args, TextReader entrada, TextWriter saida, TextWriter erros, DateOnly hoje)
    {
        if (!TentarLerParametros(args, hoje, out var dataReferencia, out var caminhoArquivo))
        {
            erros.WriteLine(MensagemDeUso);
            return 1;
        }

        if (caminhoArquivo is not null && !File.Exists(caminhoArquivo))
        {
            erros.WriteLine($"Arquivo não encontrado: {caminhoArquivo}");
            return 1;
        }

        var log = new LogAuditoria(erros);
        var processador = new ProcessadorReembolsos(
            new TabelaProcedimentos(),
            new RepositorioReembolsosEmMemoria(),
            log);

        var pedidos = LerPedidos(caminhoArquivo, entrada, log);
        var resultados = processador.Processar(pedidos, dataReferencia);
        CsvReembolsos.Escrever(saida, resultados);

        return 0;
    }

    // Os pedidos vêm do arquivo informado em --arquivo ou, se ele não foi
    // informado, do stdin. A opção de arquivo existe porque o PowerShell não
    // suporta o redirecionamento "<" e, no pipe, perde os acentos da entrada.
    // O arquivo é sempre lido como UTF-8.
    private static List<Pedido> LerPedidos(string? caminhoArquivo, TextReader entrada, LogAuditoria log)
    {
        if (caminhoArquivo is null)
            return CsvReembolsos.Ler(entrada, log);

        using var arquivo = new StreamReader(caminhoArquivo, Encoding.UTF8);
        return CsvReembolsos.Ler(arquivo, log);
    }

    // Lê os parâmetros opcionais, em qualquer ordem:
    //  --data-referencia AAAA-MM-DD : o "hoje" da regra dos 90 dias. Sem ele,
    //    usa a data atual. Permite reprocessar um lote como se fosse outro dia
    //    (por exemplo, o exemplo do enunciado, que tem datas de 2024).
    //  --arquivo caminho : lê os pedidos de um arquivo em vez do stdin.
    // Parâmetro desconhecido, repetido ou sem valor é erro de uso.
    private static bool TentarLerParametros(string[] args, DateOnly hoje, out DateOnly dataReferencia, out string? caminhoArquivo)
    {
        dataReferencia = hoje;
        caminhoArquivo = null;
        var dataInformada = false;

        if (args.Length % 2 != 0)
            return false;

        for (var i = 0; i < args.Length; i += 2)
        {
            var nome = args[i];
            var valor = args[i + 1];

            if (nome == ParametroDataReferencia && !dataInformada)
            {
                if (!DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dataReferencia))
                    return false;
                dataInformada = true;
            }
            else if (nome == ParametroArquivo && caminhoArquivo is null)
            {
                caminhoArquivo = valor;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
