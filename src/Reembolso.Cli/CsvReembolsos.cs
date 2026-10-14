using System.Globalization;
using Reembolso.Dados;

namespace Reembolso.Cli;

// Converte o CSV de entrada em pedidos e os pedidos processados no CSV de saída.
// Não contém regra de negócio: só formato de arquivo.
public static class CsvReembolsos
{
    public const string CabecalhoSaida = "Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status";

    private const string FormatoData = "yyyy-MM-dd";
    private const int QuantidadeDeCampos = 5;

    // Cultura invariante: o CSV usa ponto como separador decimal e a data no
    // formato ISO, independente do idioma configurado na máquina.
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    // Lê todas as linhas da entrada no formato
    //   Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
    // - O cabeçalho (linha que começa com "Id,") é ignorado.
    // - Linhas em branco são ignoradas.
    // - Linhas com erro (campos faltando, número ou data inválidos) são
    //   registradas no log e puladas: uma linha ruim não deve impedir o
    //   processamento das demais.
    // O CSV do enunciado não usa aspas, por isso um Split simples por vírgula basta.
    public static List<Pedido> Ler(TextReader entrada, LogAuditoria log)
    {
        var pedidos = new List<Pedido>();
        var numeroLinha = 0;

        while (entrada.ReadLine() is { } linha)
        {
            numeroLinha++;
            linha = linha.TrimStart('﻿').Trim();

            if (linha.Length == 0 || EhCabecalho(linha))
                continue;

            if (TentarConverter(linha, out var pedido, out var erro))
                pedidos.Add(pedido);
            else
                log.RegistrarLinhaIgnorada(numeroLinha, linha, erro);
        }

        return pedidos;
    }

    // Escreve o cabeçalho e uma linha por pedido processado, na ordem recebida.
    public static void Escrever(TextWriter saida, IEnumerable<PedidoProcessado> pedidos)
    {
        saida.WriteLine(CabecalhoSaida);

        foreach (var processado in pedidos)
        {
            var p = processado.Pedido;
            saida.WriteLine(string.Join(',',
                p.Id.ToString(Cultura),
                p.TipoProcedimento,
                p.DataProcedimento.ToString(FormatoData, Cultura),
                FormatarValor(p.ValorPago),
                FormatarValor(processado.ValorReembolsado),
                p.ClienteId.ToString(Cultura),
                processado.Status.ParaTexto()));
        }
    }

    // Valores sem zeros desnecessários, como no exemplo do enunciado
    // (400 -> "400", 87,50 -> "87.5", 33,33 -> "33.33").
    public static string FormatarValor(decimal valor) => valor.ToString("0.##", Cultura);

    private static bool EhCabecalho(string linha) =>
        linha.StartsWith("Id,", StringComparison.OrdinalIgnoreCase);

    // Converte uma linha em Pedido. Devolve false e a descrição do erro se algum
    // campo for inválido, indicando qual campo para facilitar a correção.
    private static bool TentarConverter(string linha, out Pedido pedido, out string erro)
    {
        pedido = null!;
        var campos = linha.Split(',').Select(c => c.Trim()).ToArray();

        if (campos.Length != QuantidadeDeCampos)
            return Falha($"esperados {QuantidadeDeCampos} campos, encontrados {campos.Length}", out erro);

        if (!int.TryParse(campos[0], NumberStyles.Integer, Cultura, out var id))
            return Falha($"Id inválido '{campos[0]}'", out erro);

        if (string.IsNullOrWhiteSpace(campos[1]))
            return Falha("TipoProcedimento vazio", out erro);

        if (!DateOnly.TryParseExact(campos[2], FormatoData, Cultura, DateTimeStyles.None, out var data))
            return Falha($"DataProcedimento inválida '{campos[2]}' (use {FormatoData})", out erro);

        if (!decimal.TryParse(campos[3], NumberStyles.Number, Cultura, out var valorPago))
            return Falha($"ValorPago inválido '{campos[3]}'", out erro);

        if (!int.TryParse(campos[4], NumberStyles.Integer, Cultura, out var clienteId))
            return Falha($"ClienteId inválido '{campos[4]}'", out erro);

        pedido = new Pedido(id, campos[1], data, valorPago, clienteId);
        erro = string.Empty;
        return true;
    }

    private static bool Falha(string mensagem, out string erro)
    {
        erro = mensagem;
        return false;
    }
}
