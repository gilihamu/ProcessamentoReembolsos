# Processamento de Reembolsos

Aplicação de linha de comando em .NET que lê pedidos de reembolso em CSV (stdin), aplica as regras de
negócio e de prevenção de fraude e escreve o resultado em CSV (stdout).

## Como compilar, executar e testar

Pré-requisito: **.NET SDK 10** (`dotnet --list-sdks`).

```bash
dotnet build      # compilar
dotnet test       # testes
```

### Executar

A forma recomendada, que funciona igual em **PowerShell, cmd e bash**, é informar o arquivo com `--arquivo`:

```powershell
dotnet run --project src/Reembolso.Cli -- --arquivo exemplos/entrada.csv --data-referencia 2024-04-30
```

Também é possível enviar o CSV pelo **stdin**, como pede o enunciado. Nesse caso o comando muda conforme o shell:

```bash
# bash / cmd: redirecionamento "<"
dotnet run --project src/Reembolso.Cli -- --data-referencia 2024-04-30 < exemplos/entrada.csv
```

```powershell
# PowerShell não tem o operador "<" ("Operador '<' reservado para uso futuro").
# Use o pipe, ajustando a codificação para não perder os acentos ("Médica" viraria "M?dica"):
$OutputEncoding = [System.Text.Encoding]::UTF8
Get-Content exemplos\entrada.csv -Encoding UTF8 | dotnet run --project src\Reembolso.Cli -- --data-referencia 2024-04-30
```

Para **separar o resultado do log de auditoria** (stdout x stderr):

```bash
# bash / cmd
dotnet run --project src/Reembolso.Cli -- --arquivo exemplos/entrada.csv > saida.csv 2> auditoria.log
```

```powershell
# No PowerShell 5.1 o "2>" embrulha cada linha do log como erro do PowerShell; use o cmd para isso:
cmd /c "dotnet run --project src\Reembolso.Cli -- --arquivo exemplos\entrada.csv > saida.csv 2> auditoria.log"
```

### Parâmetros (todos opcionais, em qualquer ordem)

| Parâmetro | Descrição |
|---|---|
| `--arquivo caminho` | Lê os pedidos desse arquivo (UTF-8) em vez do stdin. |
| `--data-referencia AAAA-MM-DD` | O "hoje" usado na regra dos 90 dias. Sem ele, usa a data atual. |

A regra dos 90 dias depende de "hoje". Com `--data-referencia` é possível processar o lote como se fosse
outro dia — necessário para reproduzir o exemplo do enunciado (datas de 2024) e para manter os testes
determinísticos. Com `2024-04-30` a saída é idêntica à do enunciado.

Parâmetro desconhecido, repetido, sem valor, data em formato inválido ou arquivo inexistente: mensagem no
stderr e código de saída `1`.

### Arquivos de exemplo

| Arquivo | Conteúdo |
|---|---|
| `exemplos/entrada.csv` | Os 4 pedidos do enunciado. |
| `exemplos/pedidos-10000.csv` | 10.500 pedidos, 800 clientes, datas de 2024-01-01 a 2024-04-30 e alguns tipos desconhecidos ("Fisioterapia"). |
| `exemplos/gerar-pedidos.ps1` | Script que gera o arquivo acima (semente fixa, sempre o mesmo resultado). Aceita `-Quantidade` e `-Saida`. |

```powershell
dotnet run --project src/Reembolso.Cli -- --arquivo exemplos/pedidos-10000.csv --data-referencia 2024-04-30
# Resultado: 6.492 Aprovado, 2.551 Rejeitado, 1.457 Suspeito de Fraude, em cerca de 1,7 s
# contando a inicialização do dotnet.

# gerar outro arquivo
powershell -ExecutionPolicy Bypass -File exemplos\gerar-pedidos.ps1 -Quantidade 50000 -Saida exemplos\grande.csv
```

## Arquitetura

Três camadas, cada uma com uma responsabilidade, sem camadas extras:

```
Reembolso.Cli  ──►  Reembolso.Negocio  ──►  Reembolso.Dados
(entrada/saída)     (regras de negócio)     (modelos e persistência)
```

| Projeto | Responsabilidade | Arquivos |
|---|---|---|
| `Reembolso.Cli` | Só entrada e saída: ler stdin, converter CSV, escrever stdout, log em stderr. Nenhuma regra de negócio. | `Program.cs`, `Aplicacao.cs`, `CsvReembolsos.cs`, `LogAuditoria.cs` |
| `Reembolso.Negocio` | Todas as regras do enunciado. | `TabelaProcedimentos.cs`, `CalculadoraReembolso.cs`, `ProcessadorReembolsos.cs`, `ILogAuditoria.cs` |
| `Reembolso.Dados` | Modelos (`Pedido`, `PedidoProcessado`, `StatusPedido`) e persistência. | `IRepositorioReembolsos.cs`, `RepositorioReembolsosEmMemoria.cs`, `RepositorioReembolsosOracle.cs` |
| `Reembolso.Tests` | Testes unitários e de ponta a ponta. | |

Cada método tem um comentário logo acima explicando qual regra implementa e por quê.

### Camada de dados e Oracle

O enunciado proíbe banco de dados, então a aplicação usa `RepositorioReembolsosEmMemoria`, que guarda o
histórico de pedidos de cada cliente durante a execução (é dele que saem os dados da janela de 30 dias da
antifraude).

`RepositorioReembolsosOracle` é **apenas um esqueleto documentado** (fora do escopo): mostra o
`CREATE TABLE`, onde seria feito o `INSERT` ("aqui insere dados no banco") e o `SELECT` ("aqui lê dados do
banco"). Os métodos lançam `NotImplementedException` e a classe não é usada. Para ativá-la bastaria
implementá-la e trocar a instância criada em `Aplicacao.Executar` — as regras de negócio dependem só da
interface `IRepositorioReembolsos` e não mudam.

### Princípios aplicados

- **Responsabilidade única**: tabela de percentuais, cálculo, decisão de status, CSV e log estão em
  classes separadas e pequenas.
- **Aberto para extensão**: novo tipo de procedimento = uma linha em `TabelaProcedimentos`. Novo banco =
  nova implementação de `IRepositorioReembolsos`. Novo destino de log = nova implementação de `ILogAuditoria`.
- **Inversão de dependência**: o negócio recebe repositório e log por construtor; os testes usam
  implementações em memória.
- **Testabilidade**: a data de referência e os fluxos de entrada/saída são parâmetros, nada é lido de
  `DateTime.Now` ou `Console` dentro das regras.

## Regras de negócio e decisões

| Regra | Implementação |
|---|---|
| Percentual por tipo | Consulta Médica 80%, Exame de Imagem 90%, Exame Laboratorial 70%, Outros 50%. Tipo não cadastrado é tratado como **Outros** e gera um aviso no log. Comparação ignora maiúsculas e espaços. |
| Teto de R$ 500 | `min(valorPago × percentual, 500)`. Arredondamento comercial em 2 casas. Valores em `decimal`. |
| Prazo de 90 dias | Aceita de `referência − 90 dias` até a `referência` (inclusive). Data futura também é **Rejeitado**. |
| Validade | Valor pago zero ou negativo é **Rejeitado**. |
| Máx. 5 pedidos em 30 dias | Se o cliente já tem 5 pedidos **aprovados** na janela, o pedido é **Suspeito de Fraude**. |
| Máx. R$ 1.500 em 30 dias | Se o total já **aprovado** na janela + este reembolso passar de R$ 1.500,00, o pedido é **Suspeito de Fraude**. Exatamente R$ 1.500,00 é permitido. |

Detalhes:

- **Janela de 30 dias**: termina na data do procedimento e inclui o próprio dia (`[data − 29, data]`).
  Como a entrada só traz a data do procedimento, ela é usada como data do pedido.
- **Ordem de avaliação**: os pedidos são avaliados em ordem cronológica (data e, no empate, Id), porque o
  "6º pedido em 30 dias" é o 6º no tempo, não na posição do arquivo. A **saída mantém a ordem da entrada**.
- **ValorReembolsado em pedidos não aprovados**: o valor calculado é informado mesmo para Rejeitado e
  Suspeito, seguindo o exemplo do enunciado (pedido 4: `75, Rejeitado`). O status indica que ele não será pago.

### Resolução de conflitos

Precedência: **Rejeitado → Suspeito de Fraude → Aprovado**.

1. Pedido inválido (prazo, data futura, valor ≤ 0) é **Rejeitado** e nem passa pela antifraude: não faz
   sentido acusar de fraude um pedido que já não pode ser processado.
2. As duas regras de fraude são sempre avaliadas. Se ambas disparam (ex.: 6º pedido que também estoura
   R$ 1.500), o status é um só — **Suspeito de Fraude** — e o log registra os dois motivos.
3. **Só pedidos aprovados contam para os limites.** Um pedido rejeitado ou suspeito não gerou reembolso,
   então não "gasta" a cota do cliente. Ex.: com R$ 1.200 aprovados, um pedido de R$ 500 é suspeito,
   mas um seguinte de R$ 300 ainda é aprovado.
4. Não há aprovação parcial: se o reembolso não cabe inteiro no limite de R$ 1.500, o pedido é suspeito.

## Entrada inválida e logs

- O log vai para o **stderr**, mantendo o stdout como CSV limpo.
- `[AUDITORIA]` — todo pedido Rejeitado ou Suspeito de Fraude, com o motivo:
  `[AUDITORIA] Pedido 4 | Cliente 126 | Rejeitado | fora do prazo de 90 dias (aceita a partir de 2024-01-31)`
- `[AVISO]` — tipo de procedimento desconhecido calculado como Outros.
- `[ENTRADA]` — linha que não pôde ser lida (campos faltando, Id/valor/cliente não numérico, data fora do
  formato `yyyy-MM-dd`). A linha é ignorada e o restante do lote continua sendo processado.
- Cabeçalho, linhas em branco e BOM UTF-8 são tratados. Números usam ponto como separador decimal.
- Parâmetro inválido ou arquivo inexistente: mensagem no stderr e código de saída `1`.

## Testes

53 testes com xUnit, cobrindo:

- Percentual de cada tipo, teto exatamente em 500, um centavo abaixo, tipo desconhecido, arredondamento.
- Prazo: dia 0, dia 90 (aprovado), dia 91 (rejeitado), data futura, valor zero/negativo.
- Fraude: 6º pedido em 30 dias, janela que "anda" no 31º dia, R$ 1.500,00 exato x R$ 1.500,01,
  suspeitos e rejeitados não consumindo limite, conflito entre as duas regras, prazo com precedência
  sobre fraude, clientes independentes, entrada fora de ordem, desempate por Id.
- CSV: cabeçalho, BOM, linhas em branco, 7 tipos de linha inválida, formatação de saída.
- Ponta a ponta: o exemplo do enunciado gera exatamente a saída esperada, tanto pelo stdin quanto por
  `--arquivo` (UTF-8 com BOM); arquivo inexistente; parâmetros inválidos, sem valor ou repetidos.
- Desempenho: 10.000 pedidos aleatórios (semente fixa) processados em menos de 2 segundos
  (na prática, poucos milissegundos).

## Bibliotecas

- **Aplicação**: nenhuma dependência externa, só a biblioteca padrão do .NET. O CSV do enunciado não
  usa aspas nem vírgulas dentro de campos, então um `Split` simples é suficiente e evita uma dependência.
- **Testes**: xUnit (padrão de mercado e template oficial do `dotnet new xunit`).

## Notas e possíveis evoluções

- **Escalabilidade**: o lote é ordenado uma vez (O(n log n)) e as consultas de fraude são feitas por
  cliente em um dicionário. Para arquivos muito grandes (milhões de linhas), a leitura poderia ser em
  streaming, mas a ordenação cronológica exige conhecer o lote inteiro.
- **Configuração**: limites (90 dias, 5 pedidos, R$ 1.500, R$ 500) estão como constantes nomeadas nas
  classes de negócio; poderiam vir de um arquivo de configuração se passassem a variar.
- **Percentuais por tipo** poderiam ser carregados de arquivo/banco sem alterar a calculadora.
