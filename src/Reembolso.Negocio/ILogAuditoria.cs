using Reembolso.Dados;

namespace Reembolso.Negocio;

// Contrato do log de auditoria. O negócio só diz "registre este pedido";
// onde o log é escrito (console, arquivo, ferramenta de observabilidade)
// é decisão de quem usa a camada de negócio.
public interface ILogAuditoria
{
    // Chamado para todo pedido Rejeitado ou Suspeito de Fraude, com os motivos.
    void RegistrarPedido(PedidoProcessado pedido);

    // Chamado para avisos que não mudam o status (ex.: tipo de procedimento
    // desconhecido calculado como "Outros").
    void RegistrarAviso(string mensagem);
}
