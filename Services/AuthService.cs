using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAvaliacaoDancando.Repositories;
using WebAvaliacaoDancando.Security;

namespace WebAvaliacaoDancando.Services;

public sealed class AuthService(IJuradoRepository juradoRepository) : IAuthService
{
    public async Task<ClaimsPrincipal?> AuthenticateAsync(
        string login,
        string senha,
        CancellationToken cancellationToken = default)
    {

        //string hash = BCrypt.Net.BCrypt.HashPassword(senha);

        var jurado = await juradoRepository.GetByLoginAsync(login, cancellationToken);
        if (jurado is null || !SenhaValida(senha, jurado.SenhaHash))
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, jurado.Id.ToString()),
            new(ClaimTypes.Name, jurado.Nome),
            new(JuradoClaimTypes.Numero, jurado.Numero.ToString()),
            new(JuradoClaimTypes.Login, jurado.Login ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static bool SenhaValida(string senhaInformada, string? senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            return false;
        }

        if (senhaHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return BCrypt.Net.BCrypt.Verify(senhaInformada, senhaHash);
        }

        return string.Equals(senhaInformada, senhaHash, StringComparison.Ordinal);
    }
}
