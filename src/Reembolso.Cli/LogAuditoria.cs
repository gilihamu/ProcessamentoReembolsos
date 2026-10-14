using Reembolso.Dados;
using Reembolso.Negocio;

namespace Reembolso.Cli;

// Log de auditoria escrito no stderr.
// O stdout é reservado para o CSV de resultado; mandar o log para o stderr
// permite redirecionar cada um para um lugar diferente, por exemplo:
//   Reembolso.Cli < entrada.csv > saida.csv 2> auditoria.log
public class LogAuditoria : ILogAuditoria
{
    private readonly TextWriter _destino;

    public LogAuditoria(TextWriter destino)
    {
        _destino = destino;
    }

    // Registra por que um pedido foi rejeitado ou marcado como suspeito,
    // para que um analista consiga auditar a decisão.
    public void RegistrarPedido(PedidoProcessado processado)
    {
        var p = processado.Pedido;
        var motivos = string.Join("; ", processado.Motivos);
        _destino.WriteLine($"[AUDITORIA] Pedido {p.Id} | Cliente {p.ClienteId} | {processado.Status.ParaTexto()} | {motivos}");
    }

    // Registra avisos que não mudam o status do pedido.
    public void RegistrarAviso(string mensagem)
    {
        _destino.WriteLine($"[AVISO] {mensagem}");
    }

    // Registra linhas da entrada que não puderam ser lidas e foram descartadas.
    public void RegistrarLinhaIgnorada(int numeroLinha, string conteudo, string motivo)
    {
        _destino.WriteLine($"[ENTRADA] Linha {numeroLinha} ignorada: {motivo} -> \"{conteudo}\"");
    }
}
