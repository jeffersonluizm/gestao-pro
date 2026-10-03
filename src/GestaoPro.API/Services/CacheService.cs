using System.Collections.Concurrent;

namespace GestaoPro.API.Services;

public class CacheService
{
    private readonly ConcurrentDictionary<string, (object Dados, DateTime Expira)> _cache = new();

    public T? Obter<T>(string chave)
    {
        if (_cache.TryGetValue(chave, out var item) && item.Expira > DateTime.UtcNow)
            return (T)item.Dados;

        return default;
    }

    public void Definir<T>(string chave, T dados, int segundosVida = 60)
    {
        _cache[chave] = (dados!, DateTime.UtcNow.AddSeconds(segundosVida));
    }

    public void Remover(string chave) => _cache.TryRemove(chave, out _);
}
