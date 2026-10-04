namespace GestaoPro.API.Repositories;

public interface IAdministratorLookup
{
    Task<bool> HasAnyAdministratorAsync();
}
