namespace Reembolso.Dados;

// ESQUELETO - FORA DO ESCOPO DO TESTE.
// Mostra onde ficaria a gravação/leitura no banco Oracle. Não é usado pela
// aplicação (o enunciado proíbe dependências externas); para ativar seria
// preciso adicionar o pacote Oracle.ManagedDataAccess.Core, implementar os
// métodos abaixo e trocar a instância criada no Program/Aplicacao.
//
// Tabela sugerida:
//   CREATE TABLE PEDIDO_REEMBOLSO (
//     ID                 NUMBER        PRIMARY KEY,
//     TIPO_PROCEDIMENTO  VARCHAR2(100) NOT NULL,
//     DATA_PROCEDIMENTO  DATE          NOT NULL,
//     VALOR_PAGO         NUMBER(12,2)  NOT NULL,
//     VALOR_REEMBOLSADO  NUMBER(12,2)  NOT NULL,
//     CLIENTE_ID         NUMBER        NOT NULL,
//     STATUS             VARCHAR2(30)  NOT NULL
//   );
//   CREATE INDEX IX_PEDIDO_CLIENTE_DATA ON PEDIDO_REEMBOLSO (CLIENTE_ID, DATA_PROCEDIMENTO);
public class RepositorioReembolsosOracle : IRepositorioReembolsos
{
    private readonly string _connectionString;

    public RepositorioReembolsosOracle(string connectionString)
    {
        _connectionString = connectionString;
    }

    // AQUI INSERE OS DADOS NO BANCO ORACLE.
    // Exemplo do que seria executado:
    //   using var conexao = new OracleConnection(_connectionString);
    //   INSERT INTO PEDIDO_REEMBOLSO
    //     (ID, TIPO_PROCEDIMENTO, DATA_PROCEDIMENTO, VALOR_PAGO, VALOR_REEMBOLSADO, CLIENTE_ID, STATUS)
    //   VALUES (:id, :tipo, :data, :valorPago, :valorReembolsado, :clienteId, :status)
    public void Salvar(PedidoProcessado pedido)
    {
        throw new NotImplementedException("Persistência em Oracle está fora do escopo deste teste.");
    }

    // AQUI LÊ OS DADOS DO BANCO ORACLE.
    // Exemplo do que seria executado (usa o índice CLIENTE_ID + DATA_PROCEDIMENTO):
    //   SELECT ID, TIPO_PROCEDIMENTO, DATA_PROCEDIMENTO, VALOR_PAGO, VALOR_REEMBOLSADO, CLIENTE_ID, STATUS
    //     FROM PEDIDO_REEMBOLSO
    //    WHERE CLIENTE_ID = :clienteId
    //      AND STATUS = 'Aprovado'
    //      AND DATA_PROCEDIMENTO BETWEEN :desde AND :ate
    public IReadOnlyList<PedidoProcessado> ObterAprovadosDoCliente(int clienteId, DateOnly desde, DateOnly ate)
    {
        throw new NotImplementedException("Leitura do Oracle está fora do escopo deste teste.");
    }
}
