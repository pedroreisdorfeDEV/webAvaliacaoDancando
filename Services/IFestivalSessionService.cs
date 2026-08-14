using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Services;

public interface IFestivalSessionService
{
    Task<IReadOnlyList<FestivalSessao>> GetAllAsync(CancellationToken cancellationToken = default);
    FestivalSessao GetByKeyOrDefault(IReadOnlyList<FestivalSessao> sessoes, string? key);
}
