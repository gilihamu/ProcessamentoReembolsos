using System.Diagnostics;
using Reembolso.Dados;
using static Reembolso.Tests.Apoio;

namespace Reembolso.Tests;

// Extra do enunciado: 10.000 pedidos devem ser processados em tempo razoável.
public class DesempenhoTests
{
    private static readonly string[] Tipos = ["Consulta Médica", "Exame de Imagem", "Exame Laboratorial", "Outros"];

    [Fact]
    public void Processa_10000_pedidos_em_menos_de_2_segundos()
    {
        var aleatorio = new Random(42); // semente fixa: o teste é sempre igual
        var pedidos = Enumerable.Range(1, 10_000)
            .Select(id => NovoPedido(
                id: id,
                tipo: Tipos[aleatorio.Next(Tipos.Length)],
                data: Hoje.AddDays(-aleatorio.Next(0, 120)),
                valorPago: aleatorio.Next(10, 1000),
                clienteId: aleatorio.Next(1, 500)))
            .ToArray();

        var cronometro = Stopwatch.StartNew();
        var resultados = NovoProcessador().Processar(pedidos, Hoje);
        cronometro.Stop();

        Assert.Equal(10_000, resultados.Count);
        Assert.Contains(resultados, r => r.Status == StatusPedido.Aprovado);
        Assert.Contains(resultados, r => r.Status == StatusPedido.Rejeitado);
        Assert.Contains(resultados, r => r.Status == StatusPedido.SuspeitoDeFraude);
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(2), $"Levou {cronometro.ElapsedMilliseconds} ms");
    }
}
