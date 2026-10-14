namespace ProcessamentoReembolsos.Tests.Infrastructure;

using FluentAssertions;
using ProcessamentoReembolsos.Domain.Enums;
using ProcessamentoReembolsos.Infrastructure;

public class CsvParserTests
{
    [Fact]
    public void Parse_CsvValido_RetornaPedidosCorretamente()
    {
        var csv = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-02-10,400,123
2,Exame de Imagem,2026-03-01,600,124";

        var parser = new CsvParser();
        var pedidos = parser.Parse(new StringReader(csv));

        pedidos.Should().HaveCount(2);
        pedidos[0].Id.Should().Be(1);
        pedidos[0].TipoProcedimento.Should().Be(TipoProcedimento.ConsultaMedica);
        pedidos[0].DataProcedimento.Should().Be(new DateTime(2026, 02, 10));
        pedidos[0].ValorPago.Should().Be(400m);
        pedidos[0].ClienteId.Should().Be(123);

        pedidos[1].Id.Should().Be(2);
        pedidos[1].TipoProcedimento.Should().Be(TipoProcedimento.ExameImagem);
    }

    [Theory]
    [InlineData("Consulta Médica", TipoProcedimento.ConsultaMedica)]
    [InlineData("Exame de Imagem", TipoProcedimento.ExameImagem)]
    [InlineData("Exame Laboratorial", TipoProcedimento.ExameLaboratorial)]
    [InlineData("Outros", TipoProcedimento.Outros)]
    public void Parse_TiposProcedimento_ParseiaCorretamente(string tipoTexto, TipoProcedimento tipoEsperado)
    {
        var csv = $@"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,{tipoTexto},2026-02-10,100,123";

        var parser = new CsvParser();
        var pedidos = parser.Parse(new StringReader(csv));

        pedidos[0].TipoProcedimento.Should().Be(tipoEsperado);
    }

    [Fact]
    public void Parse_TipoNaoReconhecido_RetornaOutros()
    {
        var csv = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Tipo Desconhecido,2026-02-10,100,123";

        var parser = new CsvParser();
        var pedidos = parser.Parse(new StringReader(csv));

        pedidos[0].TipoProcedimento.Should().Be(TipoProcedimento.Outros);
    }

    [Fact]
    public void Parse_IgnoraLinhasEmBranco()
    {
        var csv = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-02-10,400,123

2,Exame de Imagem,2026-03-01,600,124";

        var parser = new CsvParser();
        var pedidos = parser.Parse(new StringReader(csv));

        pedidos.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_ValoresDecimais_ParseiaCorretamente()
    {
        var csv = @"Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-02-10,123.45,123";

        var parser = new CsvParser();
        var pedidos = parser.Parse(new StringReader(csv));

        pedidos[0].ValorPago.Should().Be(123.45m);
    }
}
