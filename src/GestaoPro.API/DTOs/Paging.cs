namespace GestaoPro.API.DTOs;

// Página de resultados: os itens e o total que a consulta encontraria sem paginação (vai no cabeçalho X-Total-Count).
public sealed record PagedResult<T>(List<T> Items, long Total);

// Paginação opcional nas listas: sem page/pageSize, a lista vem inteira (compatível com os clientes atuais).
public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    // Normaliza page/pageSize; devolve null quando são inválidos.
    public static (int? Page, int? PageSize)? Normalize(int? page, int? pageSize)
    {
        if (page is null && pageSize is null) return (null, null);
        var p = page ?? 1;
        var size = pageSize ?? DefaultPageSize;
        if (p < 1 || size < 1 || size > MaxPageSize) return null;
        return (p, size);
    }
}

// Consulta de avisos: sempre de um condomínio (o do token), com filtros e página opcionais.
public sealed record AnnouncementQuery(
    string CondominiumId,
    bool ActiveOnly = false,
    string? Category = null,
    int? Page = null,
    int? PageSize = null);
