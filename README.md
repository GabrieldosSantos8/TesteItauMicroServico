# CotacoesWorker - Documentação Técnica

## Visão Geral
CotacoesWorker é um serviço Worker .NET responsável por consumir cotações de ativos enviadas por um microserviço externo via Kafka, aplicar estratégias de resiliência (retry, idempotência, circuit breaker, fallback) e persistir as informações. O serviço é resiliente: o sistema de operações continua funcionando mesmo se o serviço de cotações estiver indisponível.

## Arquitetura
- **Worker Service .NET**: Serviço de background que executa a lógica de consumo e persistência.
- **Kafka**: Fila de mensagens para ingestão de cotações.
- **Polly**: Biblioteca para retry, circuit breaker e fallback.
- **Observabilidade**: Logs estruturados via ILogger.
- **Idempotência**: Controle de mensagens já processadas.

## Principais Componentes

### 1. CotacaoKafkaMessage
Representa a mensagem recebida do Kafka:
- `Ticker`: Código do ativo.
- `Preco`: Valor da cotação.
- `DataCotacao`: Data/hora da cotação.
- `MessageId`: Identificador único para idempotência.

### 2. CotacaoRepository
Simula a persistência das cotações e garante idempotência (não processa duas vezes a mesma mensagem).

### 3. Worker
- Consome mensagens do Kafka.
- Aplica retry (3 tentativas) e circuit breaker (abre após 2 falhas, espera 30s).
- Fallback: aguarda e tenta novamente se o serviço de cotações estiver fora.
- Loga todas as operações e falhas.

### 4. Observabilidade
- Logs de sucesso, warnings e erros via ILogger.
- Mensagens de circuit breaker e tentativas de retry são logadas.

## Fluxo de Funcionamento
1. O Worker conecta ao Kafka e consome mensagens do tópico `cotacoes-topic`.
2. Cada mensagem é desserializada para `CotacaoKafkaMessage`.
3. O repositório verifica se a mensagem já foi processada (idempotência).
4. Se for nova, salva (simulado) e loga.
5. Em caso de falha, aplica retry e circuit breaker.
6. Se o serviço de cotações estiver fora, o worker aguarda e tenta novamente (fallback).

## Resiliência
- **Retry**: 3 tentativas com espera exponencial.
- **Circuit Breaker**: Abre após 2 falhas, fecha após 30 segundos.
- **Fallback**: Espera e tenta novamente se o serviço estiver indisponível.
- **Idempotência**: Mensagens duplicadas não são processadas.

## Como Executar
1. Configure o Kafka em `localhost:9092` e crie o tópico `cotacoes-topic`.
2. Compile e execute:
   ```
   dotnet run
   ```
3. O serviço irá consumir e processar as mensagens automaticamente.

## Como Consultar a Última Cotação de um Ativo
Implemente um método público no repositório para buscar a última cotação de um ticker:
```csharp
public CotacaoKafkaMessage? ObterUltimaCotacao(string ticker)
{
    // Exemplo: buscar na lista de cotações processadas
}
```

## Extensões Futuras
- Persistência real em banco de dados.
- Exposição de API HTTP para consulta de cotações.
- Métricas e traces com OpenTelemetry.

---

> **Atenção:** Este projeto é um exemplo didático. Adapte para produção conforme as necessidades de segurança, performance e escalabilidade do seu ambiente.
