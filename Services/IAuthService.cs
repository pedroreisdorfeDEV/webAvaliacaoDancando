using System.Security.Claims;

namespace WebAvaliacaoDancando.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string login, string senha, CancellationToken cancellationToken = default);
}
