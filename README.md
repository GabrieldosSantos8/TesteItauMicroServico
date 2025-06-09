# CotacoesWorker

Este projeto é um Worker Service .NET que consome cotações de uma fila Kafka, salva no banco de dados e implementa estratégias de retry, idempotência, circuit breaker, fallback e observabilidade. O serviço de operações deve funcionar mesmo se o serviço de cotações estiver indisponível.

## Funcionalidades
- Consumo de mensagens Kafka
- Persistência de cotações no banco de dados
- Retry e idempotência
- Circuit breaker e fallback
- Observabilidade (logs, métricas)

## Como rodar
1. Configure o Kafka e o banco de dados nas variáveis de ambiente/appsettings.json
2. Execute:
   ```
   dotnet run
   ```

## Estrutura
- Program.cs: inicialização do worker
- Worker.cs: lógica principal de consumo e persistência

## Observações
- O serviço de operações é resiliente a falhas do serviço de cotações.
- Adapte as configurações conforme seu ambiente.
