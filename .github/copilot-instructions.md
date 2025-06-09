<!-- Use this file to fornecer instruções customizadas para o Copilot neste workspace. -->

Este projeto é um Worker Service .NET que consome cotações de uma fila Kafka, salva no banco de dados e implementa estratégias de retry, idempotência, circuit breaker, fallback e observabilidade. O serviço de operações deve funcionar mesmo se o serviço de cotações estiver indisponível.
