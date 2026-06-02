using WebAvaliacaoDancando.ViewModels;

namespace WebAvaliacaoDancando.Services;

public interface IAvaliacaoService
{
    Task<AvaliacaoViewModel> GetViewModelAsync(
        string? sessaoKey,
        short juradoNumero,
        string juradoNome,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SalvarAvaliacaoViewModel model,
        short juradoNumero,
        CancellationToken cancellationToken = default);

    Task<bool> ApresentacaoJaTemParecerAsync(
        long apresentacaoId,
        short juradoNumero,
        CancellationToken cancellationToken = default);
}
