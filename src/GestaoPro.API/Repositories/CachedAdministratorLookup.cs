namespace GestaoPro.API.Repositories;

// O ApiAccessMiddleware pergunta em toda escrita se já existe administrador (instalação inicial).
// Depois do primeiro "sim", a resposta fica guardada: escrita comum deixa de pagar uma consulta ao banco.
// Guardar o "sim" é o lado seguro: a instalação anônima continua fechada.
public sealed class CachedAdministratorLookup : IAdministratorLookup
{
    private readonly IAdministratorLookup _inner;
    private volatile bool _hasAdministrator;

    public CachedAdministratorLookup(IAdministratorLookup inner)
    {
        _inner = inner;
    }

    public async Task<bool> HasAnyAdministratorAsync()
    {
        if (_hasAdministrator) return true;
        _hasAdministrator = await _inner.HasAnyAdministratorAsync();
        return _hasAdministrator;
    }
}
