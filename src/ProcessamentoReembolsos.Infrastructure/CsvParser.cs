namespace ProcessamentoReembolsos.Infrastructure;

using System.Globalization;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;

public class CsvParser
{
    public List<PedidoReembolso> Parse(TextReader reader)
    {
        var pedidos = new List<PedidoReembolso>();
        var header = reader.ReadLine();

        string? linha;
        while ((linha = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;

            var campos = linha.Split(',');
            var id = int.Parse(campos[0]);
            var tipo = ParseTipoProcedimento(campos[1].Trim());
            var data = DateTime.Parse(campos[2].Trim());
            var valor = decimal.Parse(campos[3].Trim(), CultureInfo.InvariantCulture);
            var clienteId = int.Parse(campos[4].Trim());

            pedidos.Add(new PedidoReembolso(id, tipo, data, valor, clienteId));
        }

        return pedidos;
    }

    private static TipoProcedimento ParseTipoProcedimento(string valor) => valor switch
    {
        "Consulta Médica" => TipoProcedimento.ConsultaMedica,
        "Exame de Imagem" => TipoProcedimento.ExameImagem,
        "Exame Laboratorial" => TipoProcedimento.ExameLaboratorial,
        _ => TipoProcedimento.Outros
    };
}
