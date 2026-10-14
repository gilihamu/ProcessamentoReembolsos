# Processamento de Reembolsos

Sistema de processamento de pedidos de reembolso de planos de saúde. Recebe um arquivo CSV de entrada com pedidos, aplica regras de validação, cálculo e detecção de fraude, e exibe o resultado processado diretamente no console (stdout).

## Decisões Técnicas e Arquiteturais

O projeto segue **Clean Architecture** com separação em quatro camadas, cada uma em seu próprio projeto:

- **Domain** — Camada central que contém as entidades (`PedidoReembolso`, `ContextoCliente`), enums, interfaces e todas as regras de negócio. Não possui dependência de nenhuma camada externa nem de frameworks de infraestrutura, garantindo que a lógica de negócio seja testável de forma isolada.
- **Application** — Camada de orquestração. O `ProcessadorReembolsos` coordena a execução do pipeline de regras, gerenciando o contexto de cada cliente (histórico de aprovações) ao longo do processamento. Depende apenas do Domain.
- **Infrastructure** — Camada responsável pela entrada e saída de dados. O `CsvParser` converte o texto CSV em entidades de domínio e o `CsvWriter` serializa as entidades processadas de volta para formato CSV. Depende apenas do Domain, sem conhecimento da camada de aplicação.
- **Console** — Ponto de entrada da aplicação. Monta o pipeline de regras, conecta as camadas e direciona a entrada (stdin) e saída (stdout). Referencia todas as camadas.

Essa separação garante que cada camada possa ser testada de forma independente e que mudanças em infraestrutura (ex: trocar CSV por banco de dados) não impactem as regras de negócio.

### Padrões de Projeto

**Chain of Responsibility** para o pipeline de regras. Cada regra recebe o pedido, decide se deve rejeitá-lo ou marcá-lo como suspeito, ou encaminha para a próxima regra na cadeia. Quando uma regra atua sobre o pedido (rejeição ou suspeita), o pipeline é interrompido — as regras seguintes não são executadas. A classe abstrata `RegraBase` implementa esse mecanismo, verificando `pedido.JaProcessado` antes de encaminhar. A ordem do pipeline é:

1. `RegraValidacaoData` — Rejeita pedidos com mais de 90 dias entre a data do procedimento e a data de referência.
2. `RegraLimitePedidos` — Marca como suspeito se o cliente já possui 5 ou mais pedidos aprovados dentro da janela de 30 dias.
3. `RegraLimiteValor` — Marca como suspeito se o valor acumulado de reembolsos do cliente na janela de 30 dias, somado ao reembolso atual, ultrapassa R$1.500.
4. `RegraCalculoReembolso` — Calcula o valor de reembolso aplicando o percentual do tipo de procedimento e o cap individual, e aprova o pedido.

**Strategy** para o cálculo de percentuais de reembolso. A `CalculadoraReembolsoFactory` retorna a calculadora adequada para cada tipo de procedimento, permitindo adicionar novos tipos sem alterar as regras existentes:

| Tipo | Percentual |
|------|-----------|
| Consulta Médica | 80% |
| Exame de Imagem | 90% |
| Exame Laboratorial | 70% |
| Outros | 60% |

Todos os reembolsos individuais são limitados a R$500 (cap).

### Logs de Auditoria

Logs são gerados via Serilog exclusivamente para pedidos rejeitados e suspeitos de fraude. Os logs são direcionados para stderr, mantendo o stdout limpo para a saída de dados. Ao final do processamento, é exibido um resumo com a quantidade total de pedidos, o tempo de processamento, e as contagens de rejeitados e suspeitos.

## Bibliotecas Utilizadas

| Biblioteca | Justificativa |
|-----------|--------------|
| **Serilog** | Logging estruturado com suporte nativo a saída em stderr, permitindo separar logs da saída de dados no console |
| **xUnit** | Framework de testes padrão para .NET, com suporte a testes parametrizados via `[Theory]` |
| **FluentAssertions** | Assertions mais expressivas e legíveis nos testes, facilitando a leitura dos cenários |

## Compilar e Executar

Pré-requisitos: .NET 10 SDK.

```bash
dotnet build
```

### Linux / macOS

```bash
# Executar — a saída processada é exibida no console
cat arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console

# Redirecionar a saída do console para um arquivo, se desejado
cat arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console > saida.csv

# Salvar saída e logs separadamente
cat arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console > saida.csv 2> logs.txt
```

### Windows (PowerShell)

```powershell
# Executar
Get-Content arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console

# Redirecionar a saída do console para um arquivo, se desejado
Get-Content arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console > saida.csv
```

### Windows (CMD)

```cmd
# Executar
type arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console

# Redirecionar a saída do console para um arquivo, se desejado
type arquivo.csv | dotnet run --project src/ProcessamentoReembolsos.Console > saida.csv
```

### Formato do CSV de entrada

```
Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2026-02-10,400,123
```

### Formato da saída no console

```
Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status
1,Consulta Médica,2026-02-10,400,320,123,Aprovado
```

## Rodar os Testes

```bash
dotnet test
```

O projeto possui 77 testes cobrindo:

- **Domain.Tests** (47) — Testes unitários das entidades, de cada regra de negócio individualmente e das strategies de cálculo.
- **Infrastructure.Tests** (19) — Testes unitários do parser (conversão de CSV para entidades) e do writer (serialização de entidades para CSV).
- **Application.Tests** (11) — Testes do processador e fluxos completos de integração validando cenários de ponta a ponta (aprovação, rejeição por prazo, suspeita por quantidade e por valor, janela temporal).

## Arquivos de Exemplo

O projeto inclui arquivos CSV de exemplo na raiz para testar os diferentes cenários:

| Arquivo | Cenário |
|---------|---------|
| `entrada_percentuais.csv` | Validação dos percentuais de reembolso por tipo de procedimento |
| `entrada_datas_limite.csv` | Rejeição por prazo excedido |
| `entrada_fraude_pedidos.csv` | Detecção de suspeita por quantidade de pedidos |
| `entrade_fraude_valor.csv` | Detecção de suspeita por limite de valor acumulado |
| `entrada_cenario_misto.csv` | Cenário misto com aprovações, rejeições e suspeitas |
| `entrada_escalabilidade_10k.csv` | Teste de carga com 10.000 registros |

## Notas Adicionais

- **Escalabilidade**: O arquivo `entrada_escalabilidade_10k.csv` contém 10.000 registros para teste de carga. O sistema processa os 10k registros em aproximadamente 1 segundo.
- **Limites configuráveis**: Prazo máximo (dias), limite de pedidos por período, limite de valor por período e cap individual são parâmetros nos construtores das regras, facilitando ajustes sem alterar código.
- **Janela temporal**: As regras de limite de pedidos e valor consideram uma janela de 30 dias a partir da data de cada procedimento, não uma janela fixa.
