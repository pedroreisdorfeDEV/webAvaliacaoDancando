namespace WebAvaliacaoDancando.Models;

public sealed class ApresentacaoCardViewModel
{
    public long ApresentacaoId { get; init; }
    public long CoreografiaId { get; init; }
    public DateTime Data { get; init; }
    public string Turno { get; init; } = string.Empty;
    public string CoreografiaNome { get; init; } = string.Empty;
    public string CoreografoNome { get; init; } = string.Empty;
    public string TipoMostra { get; init; } = string.Empty;
    public decimal? NotaAtual { get; init; }
    public string? ParecerAtual { get; init; }
    public decimal? MediaFinal { get; init; }
    public int Ordem { get; init; }

    public bool JaAvaliado => NotaAtual.HasValue || !string.IsNullOrWhiteSpace(ParecerAtual);
}
