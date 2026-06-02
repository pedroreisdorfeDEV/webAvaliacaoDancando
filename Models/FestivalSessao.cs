namespace WebAvaliacaoDancando.Models;

public sealed class FestivalSessao
{
    public required string Key { get; init; }
    public required string Titulo { get; init; }
    public required DateTime Data { get; init; }
    public required string Turno { get; init; }

    public string TurnoNormalizado => Turno.ToUpperInvariant();
}
