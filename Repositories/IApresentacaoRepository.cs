using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Repositories;

public interface IApresentacaoRepository
{
    Task<IReadOnlyList<ApresentacaoCardViewModel>> GetBySessaoAsync(
        FestivalSessao sessao,
        short juradoNumero,
        CancellationToken cancellationToken = default);

    Task SaveAvaliacaoAsync(
        long apresentacaoId,
        short juradoNumero,
        decimal nota,
        string? parecer,
        string? audioPath,
        CancellationToken cancellationToken = default);

    Task<bool> TemParecerAsync(
        long apresentacaoId,
        short juradoNumero,
        CancellationToken cancellationToken = default);

    Task<ApresentacaoAvaliacaoInfo?> GetAvaliacaoInfoAsync(
        long apresentacaoId,
        CancellationToken cancellationToken = default);
}
