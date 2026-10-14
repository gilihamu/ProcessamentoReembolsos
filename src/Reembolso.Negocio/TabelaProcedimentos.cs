namespace Reembolso.Negocio;

// Regra 1 - Tipos de procedimento e percentual de reembolso.
// Para criar um novo tipo de procedimento basta adicionar uma linha no
// dicionário abaixo; nenhuma outra classe precisa mudar.
public class TabelaProcedimentos
{
    public const string Outros = "Outros";

    private readonly Dictionary<string, decimal> _percentuais = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consulta Médica"] = 0.80m,
        ["Exame de Imagem"] = 0.90m,
        ["Exame Laboratorial"] = 0.70m,
        [Outros] = 0.50m,
    };

    // Devolve o percentual de reembolso do tipo informado.
    // Tipo não cadastrado cai em "Outros" (50%): o enunciado define "Outros"
    // como a categoria para tudo que não é consulta nem exame, então um
    // procedimento desconhecido não deve travar o processamento do lote.
    // A comparação ignora maiúsculas/minúsculas e espaços nas pontas.
    public decimal ObterPercentual(string tipoProcedimento)
    {
        var tipo = tipoProcedimento.Trim();
        return _percentuais.TryGetValue(tipo, out var percentual)
            ? percentual
            : _percentuais[Outros];
    }

    // Indica se o tipo está cadastrado; usado para avisar no log de auditoria
    // quando um pedido foi calculado como "Outros" por não ser reconhecido.
    public bool Conhece(string tipoProcedimento) => _percentuais.ContainsKey(tipoProcedimento.Trim());
}
