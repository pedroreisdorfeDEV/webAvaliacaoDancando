using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Repositories;

public interface IJuradoRepository
{
    Task<Jurado?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
}
