using Microsoft.EntityFrameworkCore;
using WebAvaliacaoDancando.Data;
using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Repositories;

public sealed class JuradoRepository(FestivalDbContext context) : IJuradoRepository
{
    public async Task<Jurado?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var loginNormalizado = login.Trim().ToLower();

        return await context.Jurados
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Login != null
                    && item.Login.Trim() != string.Empty
                    && item.Login.ToLower() == loginNormalizado,
                cancellationToken);
    }
}
