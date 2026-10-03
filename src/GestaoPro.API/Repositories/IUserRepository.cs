using GestaoPro.API.Models;

namespace GestaoPro.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
