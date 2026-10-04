using System.Collections.Concurrent;
using MongoDB.Driver;

namespace GestaoPro.API.Services;

// Trava por área comum e data: a conferência de conflito e a gravação da reserva acontecem juntas,
// para dois pedidos simultâneos no mesmo horário não passarem os dois pela conferência.
public interface IReservationSlotLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(string commonAreaId, string date, CancellationToken cancellationToken = default);
}

// Uma instância da API (testes e desenvolvimento).
public sealed class InProcessReservationSlotLock : IReservationSlotLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _slots = new();

    public async ValueTask<IAsyncDisposable> AcquireAsync(string commonAreaId, string date, CancellationToken cancellationToken = default)
    {
        var semaphore = _slots.GetOrAdd($"{commonAreaId}|{date}", _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Release(semaphore);
    }

    private sealed class Release(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}

// Várias instâncias (produção): um documento por área e data com _id único faz o papel de trava.
// Quem não consegue inserir espera e tenta de novo; trava esquecida (processo que caiu) expira sozinha.
public sealed class MongoReservationSlotLock : IReservationSlotLock
{
    public const string BusyMessage = "Muitos pedidos para este horário agora. Tente de novo em instantes.";

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<SlotLease> _leases;

    public MongoReservationSlotLock(IMongoDatabase database)
    {
        _leases = database.GetCollection<SlotLease>("reservation_locks");
        _leases.Indexes.CreateOne(new CreateIndexModel<SlotLease>(
            Builders<SlotLease>.IndexKeys.Ascending(l => l.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
    }

    public async ValueTask<IAsyncDisposable> AcquireAsync(string commonAreaId, string date, CancellationToken cancellationToken = default)
    {
        var key = $"{commonAreaId}|{date}";
        var deadline = DateTime.UtcNow + WaitLimit;
        var delay = 5;

        while (true)
        {
            try
            {
                await _leases.InsertOneAsync(new SlotLease { Id = key, ExpiresAt = DateTime.UtcNow + LeaseDuration },
                    cancellationToken: cancellationToken);
                return new Release(_leases, key);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Trava vencida (o TTL do MongoDB roda a cada minuto): remove e tenta de novo.
                await _leases.DeleteOneAsync(l => l.Id == key && l.ExpiresAt < DateTime.UtcNow, cancellationToken);
                if (DateTime.UtcNow > deadline) throw new InvalidOperationException(BusyMessage);
                await Task.Delay(delay, cancellationToken);
                delay = Math.Min(delay * 2, 100);
            }
        }
    }

    public sealed class SlotLease
    {
        public string Id { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private sealed class Release(IMongoCollection<SlotLease> leases, string key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await leases.DeleteOneAsync(l => l.Id == key);
    }
}
