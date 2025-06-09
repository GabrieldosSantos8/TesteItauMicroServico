using Confluent.Kafka;
using Polly;
using Polly.CircuitBreaker;
using System.Text.Json;

namespace CotacoesWorker;

public class CotacaoKafkaMessage
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public DateTime DataCotacao { get; set; }
    public string MessageId { get; set; } = string.Empty;
}

public class CotacaoRepository
{
    private readonly HashSet<string> _idsProcessados = new(); // Simula idempotência
    public Task<bool> SaveAsync(CotacaoKafkaMessage cotacao, ILogger logger)
    {
        // Simulação de idempotência
        if (_idsProcessados.Contains(cotacao.MessageId))
        {
            logger.LogWarning("Cotação já processada: {id}", cotacao.MessageId);
            return Task.FromResult(false);
        }
        _idsProcessados.Add(cotacao.MessageId);
        logger.LogInformation("Cotação salva: {ticker} - {preco}", cotacao.Ticker, cotacao.Preco);
        return Task.FromResult(true);
    }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CotacaoRepository _repo = new();
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly IAsyncPolicy _retryPolicy;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30),
                onBreak: (ex, ts) => _logger.LogWarning("Circuit breaker aberto: {msg}", ex.Message),
                onReset: () => _logger.LogInformation("Circuit breaker fechado"));
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2),
                (ex, ts, retry, ctx) => _logger.LogWarning("Tentativa {retry} falhou: {msg}", retry, ex.Message));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "cotacoes-worker-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("cotacoes-topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _circuitBreaker.ExecuteAsync(async () =>
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);
                            var cotacao = JsonSerializer.Deserialize<CotacaoKafkaMessage>(consumeResult.Message.Value);
                            if (cotacao != null)
                                await _repo.SaveAsync(cotacao, _logger);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError("Erro ao consumir mensagem: {msg}", ex.Message);
                        }
                        return true;
                    })
                );
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit breaker aberto. Fallback: aguardando antes de tentar novamente.");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro inesperado: {msg}", ex.Message);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
