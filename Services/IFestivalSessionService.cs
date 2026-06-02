using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Services;

public interface IFestivalSessionService
{
    IReadOnlyList<FestivalSessao> GetAll();
    FestivalSessao GetByKeyOrDefault(string? key);
}
