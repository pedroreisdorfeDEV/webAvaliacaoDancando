using System.Security.Claims;
using WebAvaliacaoDancando.Security;

namespace WebAvaliacaoDancando.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static short GetJuradoNumero(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JuradoClaimTypes.Numero);
        return short.TryParse(value, out var numero) ? numero : (short)0;
    }
}
