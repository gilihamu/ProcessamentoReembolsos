namespace ProcessamentoReembolsos.Infrastructure;

using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;

public class CsvWriter
{
    public void Write(TextWriter writer, List<PedidoReembolso> pedidos)
    {
        writer.WriteLine("Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status");

        foreach (var pedido in pedidos)
        {
            var tipo = FormatTipoProcedimento(pedido.TipoProcedimento);
            var data = pedido.DataProcedimento.ToString("yyyy-MM-dd");
            var valorPago = pedido.ValorPago.ToString("0.##");
            var valorReembolsado = pedido.ValorReembolsado.ToString("0.##");
            var status = FormatStatus(pedido.Status);

            writer.WriteLine($"{pedido.Id},{tipo},{data},{valorPago},{valorReembolsado},{pedido.ClienteId},{status}");
        }
    }

    private static string FormatTipoProcedimento(TipoProcedimento tipo) => tipo switch
    {
        TipoProcedimento.ConsultaMedica => "Consulta Médica",
        TipoProcedimento.ExameImagem => "Exame de Imagem",
        TipoProcedimento.ExameLaboratorial => "Exame Laboratorial",
        _ => "Outros"
    };

    private static string FormatStatus(StatusPedido status) => status switch
    {
        StatusPedido.Aprovado => "Aprovado",
        StatusPedido.Rejeitado => "Rejeitado",
        StatusPedido.SuspeitoDeFraude => "Suspeito de Fraude",
        _ => "Pendente"
    };
}
