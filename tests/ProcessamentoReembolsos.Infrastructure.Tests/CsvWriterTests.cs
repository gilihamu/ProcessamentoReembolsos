namespace ProcessamentoReembolsos.Tests.Infrastructure;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Entities;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Infrastructure;

public class CsvWriterTests
{
    [Fact]
    public void Write_EscreveHeaderCorreto()
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedidos = new List<PedidoReembolso>();

        writer.Write(stringWriter, pedidos);

        var output = stringWriter.ToString();
        output.Should().Contain("Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status");
    }

    [Theory]
    [InlineData(TipoProcedimento.ConsultaMedica, "Consulta Médica")]
    [InlineData(TipoProcedimento.ExameImagem, "Exame de Imagem")]
    [InlineData(TipoProcedimento.ExameLaboratorial, "Exame Laboratorial")]
    [InlineData(TipoProcedimento.Outros, "Outros")]
    public void Write_FormataTipoProcedimentoCorretamente(TipoProcedimento tipo, string textoEsperado)
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedido = new PedidoReembolso(1, tipo, DateTime.Today, 100m, 123);
        pedido.Aprovar(80m);

        writer.Write(stringWriter, new List<PedidoReembolso> { pedido });

        var output = stringWriter.ToString();
        output.Should().Contain(textoEsperado);
    }

    [Theory]
    [InlineData(StatusPedido.Aprovado, "Aprovado")]
    [InlineData(StatusPedido.Rejeitado, "Rejeitado")]
    [InlineData(StatusPedido.SuspeitoDeFraude, "Suspeito de Fraude")]
    public void Write_FormataStatusCorretamente(StatusPedido status, string textoEsperado)
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);

        if (status == StatusPedido.Aprovado)
            pedido.Aprovar(80m);
        else if (status == StatusPedido.Rejeitado)
            pedido.Rejeitar();
        else if (status == StatusPedido.SuspeitoDeFraude)
            pedido.MarcarComoSuspeito();

        writer.Write(stringWriter, new List<PedidoReembolso> { pedido });

        var output = stringWriter.ToString();
        output.Should().Contain(textoEsperado);
    }

    [Fact]
    public void Write_FormataDataEmYyyyMmDd()
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, new DateTime(2026, 02, 10), 100m, 123);
        pedido.Aprovar(80m);

        writer.Write(stringWriter, new List<PedidoReembolso> { pedido });

        var output = stringWriter.ToString();
        output.Should().Contain("2026-02-10");
    }

    [Fact]
    public void Write_FormataValoresSemZerosInecessarios()
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ConsultaMedica, DateTime.Today, 100m, 123);
        pedido.Aprovar(80m);

        writer.Write(stringWriter, new List<PedidoReembolso> { pedido });

        var output = stringWriter.ToString();
        output.Should().Contain(",100,");
        output.Should().Contain(",80,");
    }

    [Fact]
    public void Write_FormataValoresComDecimais()
    {
        var writer = new CsvWriter();
        var stringWriter = new StringWriter();
        var pedido = new PedidoReembolso(1, TipoProcedimento.ExameImagem, DateTime.Today, 555m, 123);
        pedido.Aprovar(499.5m);

        writer.Write(stringWriter, new List<PedidoReembolso> { pedido });

        var output = stringWriter.ToString();
        output.Should().Contain(",555,");
        output.Should().Contain(",499.5,");
    }
}
