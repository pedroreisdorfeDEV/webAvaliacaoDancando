namespace WebAvaliacaoDancando.Models;

public sealed class DataTurnoPreferencialDisponivel
{
    public long Id { get; init; }
    public DateTime Data { get; init; }
    public string Turno { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public int Ordem { get; init; }
    public string? Observacao { get; init; }
    public DateTime? DataCriacao { get; init; }
    public DateTime? DataAtualizacao { get; init; }
}
