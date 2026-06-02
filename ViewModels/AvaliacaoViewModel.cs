using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.ViewModels;

public sealed class AvaliacaoViewModel
{
    public string JuradoNome { get; init; } = string.Empty;
    public short JuradoNumero { get; init; }
    public IReadOnlyList<FestivalSessao> Sessoes { get; init; } = [];
    public FestivalSessao SessaoAtual { get; init; } = default!;
    public IReadOnlyList<ApresentacaoCardViewModel> Apresentacoes { get; init; } = [];
    public string? FeedbackMensagem { get; set; }
    public string? FeedbackTipo { get; set; }
}
