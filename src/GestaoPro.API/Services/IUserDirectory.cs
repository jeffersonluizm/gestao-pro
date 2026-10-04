using GestaoPro.API.DTOs;

namespace GestaoPro.API.Services;

// Leituras de usuários usadas pelo UsersController (implementadas pelo UserAppService).
// Separadas das escritas para que o controle de acesso possa ser testado sem MongoDB.
public interface IUserDirectory
{
    Task<List<UserResponse>> GetAllActiveAsync();
    Task<List<UserResponse>> GetAllInactiveAsync();
    Task<List<UserResponse>> GetByCondominiumAsync(string condominiumId);
    Task<UserResponse?> GetByIdAsync(string id);
}
